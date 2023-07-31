using GliderView.Service.Exeptions;
using GliderView.Service.Models;
using GliderView.Service.Repositories;
using GliderView.Service.Utilities;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GliderView.Service
{
    public class FlightService
    {
        private readonly IFlightRepository _repo;
        private readonly FlightAnalyzer _analyzer;
        private readonly IRateRepo _rateRepo;
        private readonly IAirfieldRepo _fieldRepo;
        private readonly ILogger<FlightService> _logger;

        public FlightService(IFlightRepository repo, FlightAnalyzer analyzer, IRateRepo rateRepo, IAirfieldRepo fieldRepo, ILogger<FlightService> logger)
        {
            _repo = repo;
            _analyzer = analyzer;
            _rateRepo = rateRepo;
            _fieldRepo = fieldRepo;
            _logger = logger;
        }

        public async Task AddPilot(Guid flightId, Guid pilotId)
        {
            Flight? flight = await _repo.GetFlight(flightId);
            if (flight == null)
                throw new NotFoundException<Flight>(flightId);

            await _repo.AddPilot(flightId, pilotId);
        }

        public async Task RecalculateStatistics(Guid flightId)
        {
            Task<Flight?> flightTask = _repo.GetFlight(flightId);
            Task<List<Waypoint>> waypointTask = _repo.GetWaypoints(flightId);
            // Get the towplane stats for more accurate data
            Task<FlightStatistics> towStats = (await flightTask)?.TowFlight != null
                ? _repo.GetStatistics((await flightTask)!.TowFlight!.FlightId)
                : Task.FromResult(new FlightStatistics()
            );

            await Task.WhenAll(flightTask, waypointTask, towStats);

            var flight = flightTask.Result;
            if (flight == null)
                throw new InvalidOperationException("Flight not found.");

            // Attach the towplane stats to the flight object
            if (flight.TowFlight != null)
                flight.TowFlight.Statistics = towStats.Result;

            flight.Waypoints = waypointTask.Result;

            flight.Statistics = _analyzer.Analyze(flight);

            await _repo.UpsertFlightStatistics(flight);

            await _repo.UpdateFlightEvents(flight);
        }

        public Task RemovePilot(Guid flightId, Guid pilotId)
        {
            return _repo.RemovePilot(flightId, pilotId);
        }

        public Task<IEnumerable<Occupant>> GetPilotsOnFlight(Guid flightId)
        {
            return _repo.GetPilotsOnFlight(flightId);
        }

        public Task<Dictionary<Guid, IEnumerable<Occupant>>> GetPilotsOnFlights(IEnumerable<Guid> flightIds)
        {
            return _repo.GetPilotsOnFlights(flightIds);
        }

        public async Task<Flight?> GetFlight(Guid flightId)
        {
            Flight? flight = await _repo.GetFlight(flightId);

            if (flight == null)
                throw new NotFoundException("Could not find flight with ID " + flightId);

            return flight;
        }

        public Task DeleteFlight(Guid flightId)
        {
            return _repo.DeleteFlight(flightId);
        }

        public async Task<PricingInfo?> CalculateCost(Flight flight)
        {
            _logger.LogDebug("Calculating costs for flight {0}", flight.FlightId);

            if (flight.Aircraft == null)
                throw new ArgumentException("Cannot calculate rate for flight without aircraft.");
            if (flight.AirfieldId == null)
                throw new ArgumentException("Cannot calculate rate for flight without an airfield.");
            if (flight.Statistics?.ReleaseHeight == null)
                throw new ArgumentException("Cannot calculate rate for flight without release height.");

            Task<Rates?> baseRateInfo = _rateRepo.GetRates();
            Task<AircraftRates?> aircraftRateInfo = _rateRepo.GetAircraftRates(flight.Aircraft.AircraftId);
            Task<Airfield?> field = _fieldRepo.GetAirfield(flight.AirfieldId);

            await Task.WhenAll(baseRateInfo, aircraftRateInfo, field);

            if (baseRateInfo.Result == null)
                throw new InvalidOperationException("No tow rate information found.");
            if (field.Result == null)
                throw new InvalidOperationException("No field information found.");

            var pricing = new PricingInfo();

            // Tow
            pricing.LineItems.Add(new LineItem()
            {
                Description = "Hook Up",
                TotalCost = baseRateInfo.Result.HookupCost,
            });

            int releaseHeight = GetReleaseHeightInHundredsOfFeetAgl(flight.Statistics.ReleaseHeight.Value, field.Result.ElevationMeters);
            int minReleaseHeight = baseRateInfo.Result.MinTowHeight / 100;

            releaseHeight = Math.Max(releaseHeight, minReleaseHeight);

            pricing.LineItems.Add(new LineItem()
            {
                Description = "Tow",
                UnitCost = baseRateInfo.Result.CostPerHundredFeet,
                Units = "100ft",
                Quantity = releaseHeight,
                TotalCost = baseRateInfo.Result.CostPerHundredFeet * releaseHeight
            });

            // Rental
            if (aircraftRateInfo.Result != null)
            {
                decimal rentalCost = CalculateRentalCost(flight.Duration, aircraftRateInfo.Result, out double rentalDurationHours);
                pricing.LineItems.Add(new LineItem()
                {
                    Description = "Rental",
                    UnitCost = aircraftRateInfo.Result.RentalCostPerHour,
                    Units = "hr",
                    Quantity = (float)Math.Round(rentalDurationHours, 2),
                    TotalCost = rentalCost
                });
            }

            pricing.Total = pricing.LineItems.Sum(x => x.TotalCost);

            return pricing;
        }

        private decimal CalculateRentalCost(int flightDurationSeconds, AircraftRates rates, out double rentalDurationHours)
        {
            int rentalDurationSeconds = (int)Math.Max(
                flightDurationSeconds,
                rates.MinRentalHours * 60 * 60
            );

            // Round to the nearest minute
            rentalDurationHours = Math.Round(rentalDurationSeconds / 60.0) / 60.0;

            // Round to the nearest dollar
            return Math.Round((decimal)rentalDurationHours * rates.RentalCostPerHour);
        }

        private int GetReleaseHeightInHundredsOfFeetAgl(int releaseHeightMeters, int fieldElevationMeters)
        {
            int aglMeters = releaseHeightMeters - fieldElevationMeters;

            int hundresOfFeet = (int)Math.Round(UnitUtils.MetersToFeet(aglMeters) / 100.0);

            return hundresOfFeet;
        }
    }
}
