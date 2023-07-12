using GliderView.Service.Models;
using GliderView.Service.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace GliderView.API.Controllers
{
    [Route("airfields")]
    public class AirfieldController : Controller
    {
        private readonly IAirfieldRepo _airfieldRepo;

        public AirfieldController(IAirfieldRepo airfieldRepo)
        {
            _airfieldRepo = airfieldRepo;
        }

        [HttpGet]
        public async Task<IActionResult> GetByFaaId([FromQuery] string faaId)
        {
            Airfield? field = await _airfieldRepo.GetAirfield(faaId);

            if (field == null)
            {
                return NotFound("No field found with id " + faaId + ".");
            }

            return Ok(field);
        }
    }
}
