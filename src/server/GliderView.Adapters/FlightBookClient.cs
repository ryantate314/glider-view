using GliderView.Service.Adapters;
using GliderView.Service.Models.FlightBook;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.WebSockets;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace GliderView.Adapters
{
    public class FlightBookClient : IFlightBookClient
    {
        private readonly string _url;
        private readonly IHttpClientFactory _httpClient;
        private readonly IMemoryCache _cache;

        public int ReceiveBufferSize { get; set; } = 8192;

        public FlightBookClient(string url, IHttpClientFactory httpClient, IMemoryCache cache)
        {
            _url = url;
            _httpClient = httpClient;
            _cache = cache;
        }

        // api/live/igc/{trackerId}/{start}/{end} - https://gitlab.com/lemoidului/ogn-flightbook/-/blob/master/web/service.py#L83
        // https://gitlab.com/lemoidului/ogn-flightbook/-/blob/master/core/live.py#L252
        // start and end are unix timestamps (seconds) indicating the waypoints to include in the IGC file
        // start of 0 means return all waypoints since last takeoff

        public async Task Connect()
        {
            throw new NotImplementedException();
        }

        public async Task<Stream> DownloadIgcFile(string trackerId, DateTime? start = null, DateTime? end = null)
        {
            long startTimestamp = 0;
            if (start != null)
                startTimestamp = GetUnixTimeSeconds(start.Value);

            var url = $"{_url}/live/igc/{trackerId}/{startTimestamp}";
            if (end != null)
                url = url + "/" + GetUnixTimeSeconds(end.Value);

            using (var client = _httpClient.CreateClient())
            using (var response = await client.GetAsync(url))
            {
                response.EnsureSuccessStatusCode();

                // Copy to memory stream because we can't reset the stream from the Http content
                var memoryStream = new MemoryStream();
                using (var stream = await response.Content.ReadAsStreamAsync())
                    stream.CopyTo(memoryStream);
                memoryStream.Seek(0, SeekOrigin.Begin);

                return memoryStream;
            }
        }

        private long GetUnixTimeSeconds(DateTime date)
        {
            return ((DateTimeOffset)date).ToUnixTimeSeconds();
        }

        public Task<IEnumerable<AircraftLocationUpdate>> GetFleet(string faaId)
        {
            return _cache.GetOrCreateAsync($"fleet-{faaId}", async item =>
            {
                var fleet = await _GetFleet(faaId);

                // Prevent calling the Flightbook API more than once every 5 seconds.
                item.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(5);

                return fleet;
            });
        }


        private async Task<IEnumerable<AircraftLocationUpdate>> _GetFleet(string faaId)
        {
            string uri = $"{_url}/live/fleet/{faaId}";

            using (var client = _httpClient.CreateClient())
            using (var request = new HttpRequestMessage(HttpMethod.Get, uri))
            using (HttpResponseMessage response = await client.SendAsync(request))
            {
                response.EnsureSuccessStatusCode();

                string content = await response.Content.ReadAsStringAsync();

                var options = new JsonSerializerOptions()
                {
                    PropertyNameCaseInsensitive = true,
                };
                options.Converters.Add(new DateTimeConverter());

                var aircraft = JsonSerializer.Deserialize<IEnumerable<Adapters.FlightBook.Aircraft>>(content, options);

                return aircraft!.Select(x => new AircraftLocationUpdate()
                {
                    Altitude= x.AltitudeMeters,
                    Registration = x.Registration,
                    ContestId = x.ContestId,
                    IsAlive= x.IsAlive,
                    LastCheckin= x.LastCheckin,
                    Latitude= x.Latitude,
                    Longitude= x.Longitude,
                    Model = x.Model
                })
                    // Call ToList() otherwise each time the enumerable is iterated against, the system re-performs this mapping.
                    .ToList();
            }
        }

        private class DateTimeConverter : JsonConverter<DateTime>
        {
            public override DateTime Read(
                ref Utf8JsonReader reader,
                Type typeToConvert,
                JsonSerializerOptions options)
            {
                DateTime date = DateTimeOffset.FromUnixTimeSeconds(reader.GetInt64())
                    .UtcDateTime;

                return date;
            }
                    
            public override void Write(
                Utf8JsonWriter writer,
                DateTime dateTimeValue,
                JsonSerializerOptions options) =>
                    throw new NotSupportedException();
        }
    }
}
