using GliderView.Service;
using GliderView.Service.Models;
using GliderView.Service.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GliderView.API.Controllers
{
    [Route("airfields")]
    public class AirfieldController : Controller
    {
        private readonly IAirfieldService _airfieldRepo;

        public AirfieldController(IAirfieldService airfieldRepo)
        {
            _airfieldRepo = airfieldRepo;
        }

        const int fiveMinutes = 60 * 5;

        [HttpGet]
        [ResponseCache(Duration = fiveMinutes, VaryByQueryKeys = new string[] { "faaId" })]
        public async Task<IActionResult> GetByFaaId([FromQuery] string faaId)
        {
            Airfield? field = await _airfieldRepo.GetByFaaId(faaId);

            if (field == null)
            {
                return NotFound("No field found with id " + faaId + ".");
            }

            return Ok(field);
        }

        [Authorize]
        [HttpGet("{faaId}/fleet")]
        public async Task<IActionResult> GetNearbyAircrft([FromRoute]string faaId)
        {
            if (String.IsNullOrEmpty(faaId))
                return BadRequest("Invalid FAA Id.");

            IEnumerable<Service.Models.FlightBook.AircraftLocationUpdate> fleet = await _airfieldRepo.GetFleet(faaId);

            return Ok(fleet);
        }
    }
}
