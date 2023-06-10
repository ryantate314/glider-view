using GliderView.Service.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GliderView.Service
{
    public interface IFlightRepository
    {
        Task AddAircraft(Aircraft aircraft);
        Task AddFlight(Flight flight);
        Task UpdateFlight(Flight flight);
        Task AddPilot(Guid flightId, Guid pilotId);
        Task AssignTow(Guid gliderFlightId, Guid towPlaneFlightId);
        Task<Aircraft?> GetAircraftByTrackerId(string trackerId);
        Task<Flight?> GetFlight(Guid flightId);
        Task<List<Flight>> GetFlights(FlightSearch search);
        Task<List<LogBookEntry>> GetLogBook(Guid pilotId);
        Task<IEnumerable<Occupant>> GetPilotsOnFlight(Guid flightId);
        Task<Dictionary<Guid, IEnumerable<Occupant>>> GetPilotsOnFlights(IEnumerable<Guid> flightIds);
        Task<FlightStatistics> GetStatistics(Guid flightId);
        Task<Dictionary<Guid, FlightStatistics>> GetStatistics(IEnumerable<Guid> flightIds);
        Task<List<Waypoint>> GetWaypoints(Guid flightId);
        Task RemovePilot(Guid flightId, Guid pilotId);
        Task UpdateFlightEvents(Flight flight);
        Task UpsertFlightStatistics(Flight flight);
        Task DeleteFlight(Guid flightId);
    }
}
