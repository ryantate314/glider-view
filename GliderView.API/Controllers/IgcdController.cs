using GliderView.API.Models;
using GliderView.Service;
using Hangfire;
using Microsoft.AspNetCore.Mvc;

namespace GliderView.API.Controllers
{
    [Route("igcd")]
    public class IgcdController : Controller
    {
        private readonly ILogger _logger;
        private readonly IgcService _service;

        public IgcdController(IgcService service, ILogger<IgcdController> logger)
        {
            _logger = logger;
            _service = service;
        }

        [HttpPost("webhook")]
        public IActionResult Webhook([FromBody] IgcdPayload payload)
        {
            if (payload.Type == IgcdPayload.TYPE_LANDING)
            {
                _logger.LogInformation($"Received landing webhook for {payload.Id} @ {payload.Airfield}");

                var eventDate = DateTime.UtcNow;

                BackgroundJob.Schedule<IgcdController>(
                    (service) => service.ProcessWebhook(payload.Airfield, payload.Id, eventDate),
                    // Give the IGC daemon time to download the file
                    TimeSpan.FromSeconds(30)
                );
            }
            else if (payload.Type == IgcdPayload.TYPE_TESTHOOK)
            {
                _logger.LogInformation("Received test webhook");
            }
            
            return Ok();
        }

        [NonAction]
        public Task ProcessWebhook(string airfield, string trackerId, DateTime eventDate)
        {
            _logger.LogInformation($"Processing webhook job for {trackerId} @ {airfield}");

            return _service.ProcessWebhook(airfield, trackerId, eventDate);
        }
    }
}
