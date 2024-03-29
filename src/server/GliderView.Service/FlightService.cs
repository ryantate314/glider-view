﻿using GliderView.Service.Exeptions;
using GliderView.Service.Models;
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

        public FlightService(IFlightRepository repo, FlightAnalyzer analyzer)
        {
            _repo = repo;
            _analyzer = analyzer;
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

            await Task.WhenAll(flightTask, waypointTask);

            var flight = flightTask.Result;
            if (flight == null)
                throw new InvalidOperationException("Flight not found.");

            flight.Waypoints = waypointTask.Result;

            flight.Statistics = _analyzer.Analyze(flight);

            await _repo.UpsertFlightStatistics(flight);

            await _repo.UpdateFlightEvents(flight);
        }

        public Task RemovePilot(Guid flightId, Guid pilotId)
        {
            return _repo.RemovePilot(flightId, pilotId);
        }
    }
}
