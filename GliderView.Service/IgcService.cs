using GliderView.Service.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace GliderView.Service
{
    public class IgcService
    {
        private readonly IgcFileRepository _fileRepo;
        private readonly IFlightRepository _flightRepo;
        private readonly ILogger _logger;
        private readonly FaaDatabaseProvider _faaProvider;

        /// <summary>
        /// seconds
        /// </summary>
        private const int MAX_TOW_FLIGHT_DELAY = 30;


        public IgcService(IgcFileRepository fileRepo, IFlightRepository flightRepo, ILogger<IgcService> logger, FaaDatabaseProvider faaProvider)
        {
            _fileRepo = fileRepo;
            _flightRepo = flightRepo;
            _logger = logger;
            _faaProvider = faaProvider;
        }

        public async Task ProcessWebhook(string airfield, string trackerId, DateTime eventDate)
        {
            IEnumerable<string> files = _fileRepo.GetFiles(airfield, trackerId, eventDate);

            var search = new FlightSearch()
            {
                StartDate = eventDate.Date,
                EndDate = eventDate.Date.AddDays(1)
            };
            var flights = await _flightRepo.GetFlights(search);

            var processedFiles = new HashSet<string>(
                flights.Where(x => !String.IsNullOrEmpty(x.IgcFileName))
                    .Select(x => x.IgcFileName!),
                StringComparer.OrdinalIgnoreCase
            );

            var newFiles = files.Where(x => !processedFiles.Contains(x));

            Aircraft? aircraft = await _flightRepo.GetAircraftByTrackerId(trackerId);

            // TODO figure out how to handle multiple unprocessed flights of the same aircraft. Find way to mutex that file/flight record?
            foreach (var file in newFiles)
            {
                IgcFile parsedFile;
                try
                {
                    using (FileStream stream = _fileRepo.GetFile(file))
                    {
                        parsedFile = IgcFile.Parse(stream);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error parsing IGC file {trackerId}@{airfield}");
                    continue;
                }

                if (aircraft == null)
                {
                    aircraft = await AddAircraft(trackerId, parsedFile);
                }

                var flight = new Flight()
                {
                    Aircraft = aircraft,
                    EndDate = parsedFile.Waypoints.Select(x => (DateTime?)x.Time)
                        .Max() ?? eventDate,
                    StartDate = parsedFile.Waypoints.Select(x => (DateTime?)x.Time)
                        .Min() ?? eventDate,
                    IgcFileName = file,
                    Waypoints = parsedFile.Waypoints.Select(waypoint => new Waypoint()
                    {
                        Time = waypoint.Time,
                        GpsAltitude = waypoint.GpsAltitude,
                        Latitude = waypoint.Latitude,
                        Longitude = waypoint.Longitude
                    })
                };

                try
                {
                    // Find tow plane/glider
                    Flight? relatedFlight = FindRelatedFlight(flights, flight);
                    if (relatedFlight != null && aircraft.IsGlider == true)
                    {
                        // We are currently adding the glider flight
                        flight.TowFlightId = relatedFlight.FlightId;
                    }

                    await _flightRepo.AddFlight(flight);

                    if (relatedFlight != null && aircraft.IsGlider == false)
                    {
                        // We are currently adding the towplane flight
                        await _flightRepo.AssignTow(relatedFlight.FlightId, flight.FlightId);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error adding flight: {aircraft.RegistrationId} {flight.StartDate}");
                    continue;
                }
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
