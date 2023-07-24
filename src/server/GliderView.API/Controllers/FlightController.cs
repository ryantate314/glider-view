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
        private const string INCLUDE_PILOTS = "occupants";
        private const string INCLUDE_COST = "cost";

        [Obsolete]
        private readonly IFlightRepository _flightRepo;
        private readonly IIgcFileRepository _igcRepo;
        private readonly FlightService _flightService;
        private readonly IgcService _igcService;
        private readonly ILogger<FlightController> _logger;
        private readonly IncludeHandler<Flight> _includeHandler;

        public FlightController(
            IFlightRepository flightRepo,
            IIgcFileRepository igcRepo,
            FlightService flightService,
            IgcService igcService,
            ILogger<FlightController> logger
        )
        {
            _flightRepo = flightRepo;
            _igcRepo = igcRepo;
            _flightService = flightService;
            _igcService = igcService;
            _logger = logger;

            _includeHandler = new IncludeHandler<Flight>(logger)
                .AddHandler(x => x.Waypoints, config =>
                {
                    config.SingleUpdateFunction = async flight =>
                        flight.Waypoints = await _flightRepo.GetWaypoints(flight.FlightId);
                    config.MultipleUpdateFunction = flights =>
                        throw new InvalidOperationException("Retrieving waypoints for multiple flights is not supported.");
                })
                .AddHandler(x => x.Statistics, config =>
                {
                    config.SingleUpdateFunction = async flight =>
                        flight.Statistics = await _flightRepo.GetStatistics(flight.FlightId);
                    config.MultipleUpdateFunction = async flights =>
                    {
                        _logger.LogDebug("Loading flight statistics for {0} flight(s).", flights.Count);

                        var stats = await _flightRepo.GetStatistics(flights.Select(x => x.FlightId));

                        foreach (var flight in flights)
                            if (stats.ContainsKey(flight.FlightId))
                                flight.Statistics = stats[flight.FlightId];
                    };
                })
                .AddHandler(x => x.Occupants, config =>
                {
                    config.SingleUpdateFunction = async flight =>
                        flight.Occupants = (await _flightService.GetPilotsOnFlight(flight.FlightId))
                            .ToList();
                    config.MultipleUpdateFunction = async flights =>
                    {
                        var pilots = await _flightService.GetPilotsOnFlights(flights.Select(x => x.FlightId));

                        foreach (var flight in flights)
                            if (pilots.ContainsKey(flight.FlightId))
                                flight.Occupants = pilots[flight.FlightId]
                                    .ToList();
                    };
                })
                .AddHandler(x => x.Cost, config =>
                {
                    config.FailOnError = false;
                    config.RequireSyncronous = true;

                    config.SingleUpdateFunction = async flight =>
                        flight.Cost = await _flightService.CalculateCost(flight);

                    config.MultipleUpdateFunction = async flights =>
                    {
                        foreach (Flight flight in flights)
                        {
                            try
                            {
                                flight.Cost = await _flightService.CalculateCost(flight);
                            }
                            catch (Exception ex)
                            {
                                if (ex is ArgumentException || ex is InvalidOperationException)
                                    _logger.LogDebug("Error calculating cost for flight {0}: {1}", flight.FlightId, ex.Message);
                                else
                                    throw;
                            }
                        }
                    };
                });
        }

        [HttpGet]
        public async Task<IActionResult> Search([FromQuery] FlightSearch search)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (!User.Identity.IsAuthenticated && _includeHandler.ContainsProperty(search.Includes, INCLUDE_PILOTS))
                return Unauthorized("You must be logged in to see a list of pilots.");

            if (!User.Identity.IsAuthenticated && _includeHandler.ContainsProperty(search.Includes, INCLUDE_COST))
                return Unauthorized("You must be logged in to see flight costs.");

            List<Flight> flights = await _flightRepo.GetFlights(search);

            await _includeHandler.AddIncludedProperties(flights, search.Includes);
           
            return Ok(flights);
        }

        [HttpGet("{flightId}")]
        public async Task<IActionResult> GetById([FromRoute] Guid flightId, [FromQuery] string includes)
        {
            if (flightId == Guid.Empty)
                return BadRequest("Flight ID cannot be empty.");

            if (!User.Identity.IsAuthenticated && _includeHandler.ContainsProperty(includes, INCLUDE_PILOTS))
                return Unauthorized("You must be logged in to see a list of pilots.");

            if (!User.Identity.IsAuthenticated && _includeHandler.ContainsProperty(includes, INCLUDE_COST))
                return Unauthorized("You must be logged in to see flight costs.");

            Flight? flight = await _flightRepo.GetFlight(flightId);

            if (flight == null)
                return NotFound();

            await _includeHandler.AddIncludedProperties(flight, includes);

            return Ok(flight);
        }

        [Authorize]
        [ResponseCache(Duration = 24*60*60)]
        [HttpGet("{flightId}/igc")]
        public async Task<IActionResult> DownloadIgcFile([FromRoute] Guid flightId)
        {
            var flight = await _flightService.GetFlight(flightId);

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

        [Authorize]
        [HttpPost("{flightId}/reprocess")]
        public async Task<IActionResult> ReProcessIgcFile([FromRoute] Guid flightId)
        {
            await _igcService.ReprocessIgcFile(flightId);

            return Ok();
        }

        [Authorize]
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
            else if (pilotId != User.GetUserId() && !User.HasScope(Scopes.AssignPilots))
                return Unauthorized();

            await _flightService.AddPilot(flightId, pilotId!.Value);

            return Ok();
        }

        [Authorize]
        [HttpDelete("{flightId}/pilots/{pilotId}")]
        public async Task<IActionResult> RemovePilot([FromRoute] Guid flightId, [FromRoute] Guid pilotId)
        {
            if (pilotId != User.GetUserId() && !User.HasScope(Scopes.AssignPilots))
                return Unauthorized();

            // TODO authorize this user to the user ID
            await _flightService.RemovePilot(flightId, pilotId);

            return Ok();
        }

        [Authorize(Scopes.AssignPilots)]
        [HttpPut("{flightId}/pilots")]
        public async Task<IActionResult> UpdatePilots([FromRoute] Guid flightId, [FromBody] List<Occupant> newOccupants)
        {
            IEnumerable<Occupant> currentOccupants = await _flightService.GetPilotsOnFlight(flightId);

            foreach (var occupant in currentOccupants)
            {
                if (!newOccupants.Any(x => x.UserId == occupant.UserId))
                {
                    await _flightService.RemovePilot(flightId, occupant.UserId);
                }
            }
            foreach (var newOccupant in newOccupants)
            {
                if (!currentOccupants.Any(x => x.UserId == newOccupant.UserId))
                {
                    await _flightService.AddPilot(flightId, newOccupant.UserId);
                }
            }

            return NoContent();
        }

        [Authorize(Scopes.ManageFlights)]
        [HttpDelete("{flightId}")]
        public async Task<IActionResult> DeleteFlight([FromRoute] Guid flightId)
        {
            _logger.LogInformation($"User {User.Identity?.Name} is deleting flight {flightId}");

            await _flightService.DeleteFlight(flightId);

            return Ok();
        }

    }
}
