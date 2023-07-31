using GliderView.Service.Exeptions;
using GliderView.Service.Models;
using GliderView.Service.Repositories;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace GliderView.Service
{
    public class IgcService
    {
        private readonly IIgcFileRepository _fileRepo;
        private readonly IFlightRepository _flightRepo;
        private readonly ILogger _logger;
        private readonly IOgnDeviceDatabaseProvider _ognProvider;
        private readonly IFlightBookClient _flightBookClient;
        private readonly IAirfieldRepo _airfieldRepo;
        private readonly IFlightAnalyzer _flightAnalyzer;

        /// <summary>
        /// seconds
        /// </summary>
        private const int MAX_TOW_FLIGHT_DELAY = 30;

        private class FileNameData
        {
            public string Registration { get; set; }
            public string TrackerId { get; set; }
            public DateTime EventDate { get; set; }
        }


        public IgcService(
            IIgcFileRepository fileRepo,
            IFlightRepository flightRepo,
            ILogger<IgcService> logger,
            IOgnDeviceDatabaseProvider ognProvider,
            IFlightBookClient flightBookClient,
            IFlightAnalyzer flightAnalyzer,
            IAirfieldRepo airfieldRepo
        )
        {
            _fileRepo = fileRepo;
            _flightRepo = flightRepo;
            _logger = logger;
            _ognProvider = ognProvider;
            _flightBookClient = flightBookClient;
            _airfieldRepo = airfieldRepo;

            _flightAnalyzer = flightAnalyzer;
        }

        /// <summary>
        /// Downloads an IGC file from the OGN Flightbook and creates the flight.
        /// </summary>
        /// <param name="airfield"></param>
        /// <param name="trackerId"></param>
        /// <returns></returns>
        public async Task DownloadAndProcess(string airfield, string trackerId)
        {
            IgcFile parsedFile;
            string fileName;
            using (Stream igcFile = await _flightBookClient.DownloadIgcFile(trackerId))
            {
                parsedFile = IgcFile.Parse(igcFile);

                fileName = await _fileRepo.SaveFile(igcFile, airfield, parsedFile.GliderId, trackerId, parsedFile.DateOfFlight);
            }

            await ProcessIgcFile(fileName, parsedFile, airfield, trackerId);
        }

        /// <summary>
        /// Reads a file already on disk disk and creates the flight.
        /// </summary>
        /// <param name="airfield"></param>
        /// <param name="fileName"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public async Task ReadAndProcess(string airfield, string fileName)
        {
            string? trackerId = GetTrackerFromFilename(Path.GetFileName(fileName));
            if (trackerId == null)
                throw new ArgumentException("File name is not in the proper format.");

            IgcFile parsedFile;
            using (Stream file = _fileRepo.GetFile(fileName))
            {
                parsedFile = IgcFile.Parse(file);
            }

            await ProcessIgcFile(fileName, parsedFile, airfield, trackerId);
        }

        private string? GetTrackerFromFilename(string filename)
        {
            return ParseFileName(filename)
                ?.TrackerId;
        }

        private FileNameData? ParseFileName(string filename)
        {
            //2022-11-13_N2750H.C7720C.igc
            var regex = new Regex(@"^([0-9\-]+)_([A-Za-z0-9]+)\.([A-Za-z0-9]+)(_[0-9]+)?\.[iI][gG][cC]$");
            var match = regex.Match(filename);
            if (!match.Success)
                return null;
            return new FileNameData()
            {
                EventDate = DateTime.Parse(match.Groups[1].Value),
                Registration = match.Groups[2].Value,
                TrackerId = match.Groups[3].Value
            };
        }

        /// <summary>
        /// Creates a new flight from a uploaded IGC file.
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="stream"></param>
        /// <param name="airfield"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public async Task<Flight> UploadAndProcess(string fileName, Stream stream, string airfield)
        {
            IgcFile parsedFile = IgcFile.Parse(stream);


            string? trackerId = GetTrackerFromFilename(fileName);

            var glider = new Lazy<Task<Aircraft?>>(() =>
                _flightRepo.GetAircraftByRegistration(parsedFile.GliderId)
            );

            if (trackerId == null)
            {
                // Attempt to get the tracker ID from the existing aircraft DB entry.
                trackerId = (await glider.Value)?.TrackerId;
            }

            if (trackerId == null)
                throw new ArgumentException("IGC file name is not in the correct format.");

            if (parsedFile.ContestId == null && (await glider.Value) != null)
            {
                parsedFile.ContestId = await GetPreviousContestId((await glider.Value)!.AircraftId);
            }

            string internalFileName = await _fileRepo.SaveFile(stream, airfield, parsedFile.GliderId, trackerId, parsedFile.DateOfFlight);
           
            return await ProcessIgcFile(internalFileName, parsedFile, airfield, trackerId);
        }

        private async Task<string?> GetPreviousContestId(Guid aircraftId)
        {
            _logger.LogDebug("Attempting to find prevous contest ID for aircraft {0}", aircraftId);

            List<Flight> flights = await _flightRepo.GetFlights(new FlightSearch()
            {
                AircraftId = aircraftId
            });

            return flights.Where(x => !String.IsNullOrEmpty(x.ContestId))
                .OrderByDescending(x => x.StartDate)
                .FirstOrDefault()
                ?.ContestId;
        }

        //public async Task ProcessWebhook(string airfield, string trackerId, DateTime eventDate)
        //{
        //    IEnumerable<string> files = _fileRepo.GetFiles(airfield, trackerId, eventDate);

        //    var search = new FlightSearch()
        //    {
        //        StartDate = eventDate.Date,
        //        EndDate = eventDate.Date.AddDays(1)
        //    };
        //    var flights = await _flightRepo.GetFlights(search);

        //    var processedFiles = new HashSet<string>(
        //        flights.Where(x => !String.IsNullOrEmpty(x.IgcFileName))
        //            .Select(x => x.IgcFileName!),
        //        StringComparer.OrdinalIgnoreCase
        //    );

        //    var newFiles = files.Where(x => !processedFiles.Contains(x));

        //    Aircraft? aircraft = await _flightRepo.GetAircraftByTrackerId(trackerId);

        //    // TODO figure out how to handle multiple unprocessed flights of the same aircraft. Find way to mutex that file/flight record?
        //    foreach (var file in newFiles)
        //    {
        //        using (var stream = _fileRepo.GetFile(file))
        //        {
        //            await ProcessIgcFile(file, stream, aircraft, airfield, trackerId, eventDate);
        //        }
        //    }
        //}

        private async Task<Flight> ProcessIgcFile(string fileName, IgcFile file, string airfield, string trackerId)
        {
            DateTime eventDate = file.DateOfFlight;

            var search = new FlightSearch()
            {
                StartDate = eventDate.Date,
                EndDate = eventDate.Date.AddDays(1)
            };
            var flights = await _flightRepo.GetFlights(search);

            Aircraft? aircraft = await _flightRepo.GetAircraftByTrackerId(trackerId);

            if (aircraft == null)
            {
                aircraft = await AddAircraft(trackerId, file);
            }

            var flight = new Flight()
            {
                Aircraft = aircraft,
                EndDate = file.Waypoints.Select(x => (DateTime?)x.Time)
                    .Max() ?? eventDate,
                StartDate = file.Waypoints.Select(x => (DateTime?)x.Time)
                    .Min() ?? eventDate,
                IgcFileName = fileName,
                ContestId = file.ContestId,
                Waypoints = MapWaypoints(file.Waypoints),
                AirfieldId = airfield
            };

            // See if this flight already exists
            if (flights.Any(x => x.Aircraft?.AircraftId == flight.Aircraft.AircraftId
                && x.StartDate == flight.StartDate))
            {
                throw new FlightAlreadyExistsException(trackerId, flight.StartDate);
            }

            try
            {
                flight.Statistics = _flightAnalyzer.Analyze(flight);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unable to analyze flight");
            }

            try
            {
                // Find tow plane/glider
                Flight? relatedFlight = FindRelatedFlight(flights, flight);
                if (relatedFlight != null && aircraft.IsGlider == true && relatedFlight.Aircraft!.IsGlider == false)
                {
                    // We are currently adding the glider flight
                    flight.TowFlight = new Flight()
                    {
                        FlightId = relatedFlight.FlightId
                    };
                }
                await _flightRepo.AddFlight(flight);

                if (relatedFlight != null && aircraft.IsGlider == false && relatedFlight.Aircraft.IsGlider == true)
                {
                    // We are currently adding the towplane flight
                    await _flightRepo.AssignTow(relatedFlight.FlightId, flight.FlightId);
                }

                await _flightRepo.UpsertFlightStatistics(flight);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error adding flight: {aircraft.RegistrationId} {flight.StartDate}");
                throw;
            }

            return flight;
        }

        private async Task<Aircraft> AddAircraft(string trackerId, IgcFile file)
        {
            var aircraft = new Aircraft()
            {
                Description = file.GliderType,
                TrackerId = trackerId,
                RegistrationId = file.GliderId,
                IsGlider = await IsGlider(trackerId, file.GliderId, file.GliderType)
            };
            await _flightRepo.AddAircraft(aircraft);
            return aircraft;
        }

        private Flight? FindRelatedFlight(IEnumerable<Flight> flightsOnDate, Flight flight)
        {
            KeyValuePair<Flight, double> closestFlight = flightsOnDate.Where(x => x.Aircraft != null)
                .Select(compareFlight => 
                new KeyValuePair<Flight, double>(
                    compareFlight,
                    Math.Abs((flight.StartDate - compareFlight.StartDate).TotalSeconds)
                ))
                .OrderBy(x => x.Value)
                .FirstOrDefault();

            if (closestFlight.Key != null && closestFlight.Value < MAX_TOW_FLIGHT_DELAY)
            {
                return closestFlight.Key;
            }

            return null;
        }

        private async Task<bool?> IsGlider(string trackerId, string registration, string description)
        {
            bool? isGlider = null;

            try
            {
                OgnDeviceDatabaseProvider.AircraftInfo? aircraft = await _ognProvider.GetAircraftInfo(trackerId);

                isGlider = aircraft?.AircraftType == OgnDeviceDatabaseProvider.AircraftType.Glider;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error looking up aircraft in OGN database: {trackerId};{registration};{description}");
            }
            
            if (isGlider == null)
            {
                // Fall back
            }

            return isGlider;
        }

        public async Task ReprocessIgcFile(Guid flightId)
        {
            Flight? flight = await _flightRepo.GetFlight(flightId);

            if (flight == null)
                throw new NotFoundException("Could not find flight with ID " + flightId);

            if (String.IsNullOrEmpty(flight.IgcFileName))
                throw new InvalidOperationException("The provided flight does not have an associated Igc file.");

            IgcFile parsedFile;
            using (Stream file = _fileRepo.GetFile(flight.IgcFileName))
            {
                parsedFile = IgcFile.Parse(file);
            }

            flight.Waypoints = MapWaypoints(parsedFile.Waypoints);

            flight.EndDate = flight.Waypoints.Select(x => (DateTime?)x.Time)
                    .Max() ?? parsedFile.DateOfFlight;
            flight.StartDate = flight.Waypoints.Select(x => (DateTime?)x.Time)
                .Min() ?? parsedFile.DateOfFlight;

            // Contest ID
            if (parsedFile.ContestId != null)
                flight.ContestId = parsedFile.ContestId;
            else if (flight.Aircraft != null)
                flight.ContestId = await GetPreviousContestId(flight.Aircraft.AircraftId);

            // Recalculate statistics
            try
            {
                flight.Statistics = _flightAnalyzer.Analyze(flight);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unable to analyze flight");
            }

            await _flightRepo.UpdateFlight(flight);
        }

        public List<Waypoint> MapWaypoints(IEnumerable<IgcFile.Waypoint> waypoints)
        {
            return waypoints.Select(x => new Waypoint()
            {
                GpsAltitude = x.GpsAltitude,
                Latitude= x.Latitude,
                Longitude= x.Longitude,
                Time= x.Time,
            })
                .ToList();
        }
    }
}
