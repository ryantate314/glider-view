using GliderView.API.Models;
using GliderView.Service;
using GliderView.Service.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GliderView.API.Controllers
{
    [Route("leaderboard")]
    [Authorize]
    public class LeaderboardController : Controller
    {
        private readonly IFlightRepository _flightRepo;

        public LeaderboardController(IFlightRepository flightRepository)
        {
            _flightRepo = flightRepository;
        }

        [HttpGet("{_date}")]
        public async Task<IActionResult> Get([FromRoute] DateTime? _date, [FromQuery] int topN = 5)
        {
            if (_date == null)
                return BadRequest();

            DateTime date = _date.Value;

            List<Flight> allFlights = await _flightRepo.GetFlights(new FlightSearch()
            {
                StartDate = new DateTime(date.Year, 1, 1),
                EndDate = new DateTime(date.Year, 12, 31)
            });

            Dictionary<Guid, FlightStatistics> stats = await _flightRepo.GetStatistics(allFlights.Select(x => x.FlightId));
            foreach (Flight flight in allFlights)
                if (stats.ContainsKey(flight.FlightId))
                    flight.Statistics = stats[flight.FlightId];

            IEnumerable<Flight> flightsThisMonth = allFlights.Where(x => x.StartDate.Month == date.Month);
            IEnumerable<Flight> flightsToday = allFlights.Where(x => x.StartDate.Date == date.Date);

            var byAircraft = allFlights.Where(x => x.Aircraft != null)
                .GroupBy(x => x.Aircraft!.AircraftId);

            foreach (var aircraft in byAircraft)
            {
                // TODO pull aircraft stats
            }

            var leaderBoard = new LeaderboardDto()
            {
                Date = date,

                NumFlightsThisYear = allFlights.Count(),
                LongestDurationFlightsThisYear = allFlights
                    .OrderByDescending(x => x.Duration)
                    .Take(topN)
                    .ToList(),
                LongestLengthFlightsThisYear = allFlights
                    .Where(x => x.Statistics != null)
                    .OrderByDescending(x => x.Statistics!.DistanceTraveled)
                    .Take(topN)
                    .ToList(),

                NumFlightsThisMonth = flightsThisMonth.Count(),
                LongestDurationFlightsThisMonth = flightsThisMonth
                    .OrderByDescending(x => x.Duration)
                    .Take(topN)
                    .ToList(),
                LongestLengthFlightsThisMonth = flightsThisMonth
                    .Where(x => x.Statistics != null)
                    .OrderByDescending(x => x.Statistics!.DistanceTraveled)
                    .Take(topN)
                    .ToList(),

                NumFlightsToday = flightsToday.Count(),
                LongestDurationFlightsToday = flightsToday
                    .OrderByDescending(x => x.Duration)
                    .Take(topN)
                    .ToList(),
                LongestLengthFlightsToday = flightsToday
                    .Where(x => x.Statistics != null)
                    .OrderByDescending(x => x.Statistics!.DistanceTraveled)
                    .Take(topN)
                    .ToList(),
            };

            return Ok(leaderBoard);
        }
    }
}
