using GliderView.Service.Exeptions;
using GliderView.Service.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
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
        private readonly IFaaDatabaseProvider _faaProvider;
        private readonly IFlightBookClient _flightBookClient;
        private readonly FlightAnalyzer _flightAnalyzer;

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


        public IgcService(IIgcFileRepository fileRepo, IFlightRepository flightRepo, ILogger<IgcService> logger, IFaaDatabaseProvider faaProvider, IFlightBookClient flightBookClient)
        {
            _fileRepo = fileRepo;
            _flightRepo = flightRepo;
            _logger = logger;
            _faaProvider = faaProvider;
            _flightBookClient = flightBookClient;

            _flightAnalyzer = new FlightAnalyzer();
        }

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

        public async Task ReadAndProcess(string airfield, string fileName)
        {
            string? trackerId = GetTrackerFromFilename(fileName);
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
            var regex = new Regex(@"^([0-9\-]+)_([A-Za-z0-9])+\.([A-Za-z0-9]+)(_[0-9]+)?\.igc$");
            var match = regex.Match(filename);
            if (!match.Success)
                return null;
            return new FileNameData()
            {
                EventDate = DateTime.Parse(match.Groups[0].Value),
                Registration = match.Groups[1].Value,
                TrackerId = match.Groups[2].Value
            };
        }

        public async Task UploadAndProcess(string fileName, Stream stream, string airfield)
        {
            IgcFile parsedFile = IgcFile.Parse(stream);
            
            string? trackerId = GetTrackerFromFilename(fileName);

            if (trackerId == null)
                throw new ArgumentException("IGC file name is not in the correct format.");

            string internalFileName = await _fileRepo.SaveFile(stream, airfield, parsedFile.GliderId, trackerId, parsedFile.DateOfFlight);
            await ProcessIgcFile(internalFileName, parsedFile, airfield, trackerId);
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

        private async Task ProcessIgcFile(string fileName, IgcFile file, string airfield, string trackerId)
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
                Waypoints = file.Waypoints.Select(waypoint => new Waypoint()
                {
                    Time = waypoint.Time,
                    GpsAltitude = waypoint.GpsAltitude,
                    Latitude = waypoint.Latitude,
                    Longitude = waypoint.Longitude
                })
            };

            // See if this flight already exists
            if (flights.Any(x => x.Aircraft?.AircraftId == flight.Aircraft.AircraftId
                && x.StartDate == flight.StartDate))
            {
                throw new FlightAlreadyExistsException(trackerId, flight.StartDate);
            }

            flight.Statistics = _flightAnalyzer.Analyze(flight);

            try
            {
                // Find tow plane/glider
                Flight? relatedFlight = FindRelatedFlight(flights, flight);
                if (relatedFlight != null && aircraft.IsGlider == true)
                {
                    // We are currently adding the glider flight
                    flight.TowFlight = new Flight()
                    {
                        FlightId = relatedFlight.FlightId
                    };
                }
                await _flightRepo.AddFlight(flight);

                if (relatedFlight != null && aircraft.IsGlider == false)
                {
                    // We are currently adding the towplane flight
                    await _flightRepo.AssignTow(relatedFlight.FlightId, flight.FlightId);
                }

                await _flightRepo.UpsertFlightStatistics(flight);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error adding flight: {aircraft.RegistrationId} {flight.StartDate}");
                return;
            }
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
            KeyValuePair<Flight, double> closestFlight = flightsOnDate.Select(compareFlight => 
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
                var aircraft = await _faaProvider.Lookup(registration);
                isGlider = aircraft?.TypeAircraft == FaaDatabaseProvider.Aircraft.TYPE_GLIDER;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error looking up aircraft in FAA database: {trackerId};{registration};{description}");
            }
            
            if (isGlider == null)
            {
                // Fall back
            }

            return isGlider;
        }
    }
}
