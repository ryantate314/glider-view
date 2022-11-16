using GliderView.Data;
using GliderView.Service;
using GliderView.Service.Models;
using Microsoft.AspNetCore.Mvc;

namespace GliderView.API.Controllers
{
    [Route("flights")]
    public class FlightController : Controller
    {
        private readonly IFlightRepository _flightRepo;
        private readonly IgcFileRepository _igcRepo;

        public FlightController(IFlightRepository flightRepo, IgcFileRepository igcRepo)
        {
            _flightRepo = flightRepo;
            _igcRepo = igcRepo;
        }

        [HttpGet]
        public async Task<IActionResult> Search([FromQuery] FlightSearch search)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            List<Flight> flights = await _flightRepo.GetFlights(search);
            return Ok(flights);
        }

        [HttpGet("{flightId}")]
        public async Task<IActionResult> GetById([FromRoute] Guid flightId)
        {
            if (flightId == Guid.Empty)
                return BadRequest("Flight ID cannot be empty.");

            Flight? flight = await _flightRepo.GetFlight(flightId);

            if (flight == null)
                return NotFound();

            return Ok(flight);
        }

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
                return File(file, "text/plain", fileDownloadName: flight.IgcFileName);
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
    }
}
