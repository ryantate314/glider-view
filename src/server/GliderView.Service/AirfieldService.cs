using GeoLibrary.Model;
using GliderView.Service.Adapters;
using GliderView.Service.Models;
using GliderView.Service.Repositories;
using GliderView.Service.Utilities;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace GliderView.Service
{
    public interface IAirfieldService
    {
        Task<Airfield?> GetByFaaId(string faaId);
        Task<IEnumerable<Models.FlightBook.AircraftLocationUpdate>> GetFleet(string faaId);
    }

    public class AirfieldService : IAirfieldService
    {
        private readonly IFlightBookClient _flightBookClient;
        private readonly IAirfieldRepo _airfieldRepo;

        public AirfieldService(
            IFlightBookClient flightbookClient,
            IAirfieldRepo airfieldRepo
        )
        {
            _flightBookClient = flightbookClient;
            _airfieldRepo = airfieldRepo;
        }

        public async Task<IEnumerable<Models.FlightBook.AircraftLocationUpdate>> GetFleet(string faaId)
        {
            // Field is almost certainly cached, so multithreading isn't helpful
            Airfield? field = await _airfieldRepo.GetAirfield(faaId);
            if (field == null)
                throw new InvalidOperationException("Could not find field with ID.");

            IEnumerable<Models.FlightBook.AircraftLocationUpdate> fleet = await _flightBookClient.GetFleet(faaId);

            foreach (var aircraft in fleet)
            {
                aircraft.DistanceFromFieldKm = GeoUtils.GetDistanceFromLatLonInKm(field.Latitude, field.Longitude, aircraft.Latitude, aircraft.Longitude);
                aircraft.BearingFromField = (int)Math.Round(GeoUtils.GetBearing(field.Latitude, field.Longitude, aircraft.Latitude, aircraft.Longitude));
            }

            return fleet;
        }

        public Task<Models.Airfield?> GetByFaaId(string faaId)
        {
            return _airfieldRepo.GetAirfield(faaId);
        }

    }
}
