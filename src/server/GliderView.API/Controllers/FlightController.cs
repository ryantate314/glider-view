using GliderView.Data;
using GliderView.Service;
using GliderView.Service.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GliderView.API.Controllers
{
    [Route("flights")]
    public class FlightController : Controller
    {
        private const string INCLUDE_WAYPOINTS = "waypoints";
        private const string INCLUDE_STATISTICS = "statistics";

        [Obsolete]
        private readonly IFlightRepository _flightRepo;
        private readonly IIgcFileRepository _igcRepo;
        private readonly FlightService _flightService;
        private readonly IncludeHandler<Flight> _includeHandler;

        public FlightController(IFlightRepository flightRepo, IIgcFileRepository igcRepo, FlightService flightService, ILogger<FlightController> logger)
        {
            _flightRepo = flightRepo;
            _igcRepo = igcRepo;
            _flightService = flightService;

            _includeHandler = new IncludeHandler<Flight>(logger)
                .AddHandler(x => x.Waypoints, config =>
                {
                    config.SingleUpdateFunction = async flight =>
                        flight.Waypoints = await _flightRepo.GetWaypoints(flight.FlightId);
                    config.MultipleUpdateFunction = flights => throw new InvalidOperationException("Retrieving waypoints for multiple flights is not supported.");
                })
                .AddHandler(x => x.Statistics, config =>
                {
                    config.SingleUpdateFunction = async flight =>
                        flight.Statistics = await _flightRepo.GetStatistics(flight.FlightId);
                    config.MultipleUpdateFunction = async flights =>
                    {
                        var stats = await _flightRepo.GetStatistics(flights.Select(x => x.FlightId));

                        foreach (var flight in flights)
                            if (stats.ContainsKey(flight.FlightId))
                                flight.Statistics = stats[flight.FlightId];
                    };
                });
        }

        [HttpGet]
        public async Task<IActionResult> Search([FromQuery] FlightSearch search)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            List<Flight> flights = await _flightRepo.GetFlights(search);

            await _includeHandler.AddIncludedProperties(flights, search.Includes);
           
            return Ok(flights);
        }

        [HttpGet("{flightId}")]
        public async Task<IActionResult> GetById([FromRoute] Guid flightId, [FromQuery] string includes)
        {
            if (flightId == Guid.Empty)
                return BadRequest("Flight ID cannot be empty.");

            Flight? flight = await _flightRepo.GetFlight(flightId);

            if (flight == null)
                return NotFound();

            await _includeHandler.AddIncludedProperties(flight, includes);

            return Ok(flight);
        }

        [ResponseCache(Duration = 24*60*60)]
        [HttpGet("{flightId}/igc")]
        public async Task<IActionResult> DownloadIgcFile([FromRoute] Guid flightId)
        {
            var flight = await _flightRepo.GetFlight(flightId);

            if (flight == null)
                return NotFound();
            if (String.IsNullOrEmpty(flight.IgcFileName))
                return BadRequest("The provided flight does not have an associated Igc file.");

            try
            {
                Stream file = _igcRepo.GetFile(flight.IgcFileName);
                Response.Headers.Add("Access-Control-Expose-Headers", "Content-Disposition");
                return File(file, "text/plain", fileDownloadName: Path.GetFileName(flight.IgcFileName));
            }
            catch (Exception ex)
            {
                if (ex is DirectoryNotFoundException || ex is FileNotFoundException)
                {
                    return BadRequest("The corresponding Igc file is missing from disk.");
                }
                throw;
            }
        }

        [HttpPost("{flightId}/recalculate-statistics")]
        public async Task<IActionResult> RecalculateStatistics([FromRoute] Guid flightId)
        {
            await _flightService.RecalculateStatistics(flightId);

            return Ok();
        }

        [Authorize]
        [HttpPost("{flightId}/pilots")]
        public async Task<IActionResult> AddPilot([FromRoute] Guid flightId, [FromQuery] Guid? pilotId)
        {
            if (pilotId == null)
                pilotId = User.GetUserId();

            await _flightService.AddPilot(flightId, pilotId!.Value);

            return Ok();
        }

        [Authorize]
        [HttpDelete("{flightId}/pilots/{pilotId}")]
        public async Task<IActionResult> AddPilot([FromRoute] Guid flightId, [FromRoute] Guid pilotId)
        {
            // TODO authorize this user to the user ID
            await _flightService.RemovePilot(flightId, pilotId);

            return Ok();
        }
    }
}
