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
                    // Give time to collect the last few seconds of data
                    TimeSpan.FromSeconds(15)
                );
            }
            else if (payload.Type == IgcdPayload.TYPE_TESTHOOK)
            {
                _logger.LogInformation("Received test webhook");
            }

            return Ok();
        }

        [HttpPost("upload")]
        public async Task<IActionResult> Upload(List<IFormFile> files, [FromQuery] string airfield)
        {
            _logger.LogInformation($"Uploading {files.Count} IGC file(s) for {airfield}");

            if (string.IsNullOrEmpty(airfield))
                return BadRequest("Airfield is required.");

            foreach (IFormFile file in files)
            {
                _logger.LogDebug("Uploading file {0} with length {1}", file.FileName, file.Length);

                using (var stream = file.OpenReadStream())
                {
                    await _service.UploadAndProcess(file.FileName, stream, airfield);
                }
            }

            return Ok();
        }

        [NonAction]
        public Task ProcessWebhook(string airfield, string trackerId, DateTime eventDate)
        {
            var maxIgcRetention = TimeSpan.FromDays(1);

            _logger.LogInformation($"Processing webhook job for {trackerId} @ {airfield}");

            if ((DateTime.UtcNow - eventDate) > maxIgcRetention)
            {
                _logger.LogError($"Download did not complete in time. Igc file lost. {airfield};{trackerId};{eventDate}");
                return Task.CompletedTask;
            }

            return _service.DownloadAndProcess(airfield, trackerId);
        }
    }
}
