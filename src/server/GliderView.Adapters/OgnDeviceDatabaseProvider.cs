using GliderView.Service.Adapters;
using GliderView.Service.Models.OgnDatabaseProvider;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace GliderView.Service
{
    /// <inheritdoc/>
    public class OgnDeviceDatabaseProvider : IOgnDeviceDatabaseProvider
    {
        private readonly IHttpClientFactory _httpClient;
        private readonly string _baseUrl;

        public OgnDeviceDatabaseProvider(IHttpClientFactory httpClient)
        {
            _httpClient = httpClient;
            _baseUrl = "https://ddb.glidernet.org/download";
        }

        public async Task<AircraftInfo?> GetAircraftInfo(string trackerId)
        {
            // t = include aircraft type
            // j = return json
            string url = $"{_baseUrl}?t=1&j=1&device_id={Uri.EscapeDataString(trackerId)}";

            using (var client = _httpClient.CreateClient())
            using (var request = new HttpRequestMessage(HttpMethod.Get, url))
            using (var response = await client.SendAsync(request))
            {
                response.EnsureSuccessStatusCode();

                string content = await response.Content.ReadAsStringAsync();

                return JsonSerializer.Deserialize<Response>(content)!.Devices.FirstOrDefault();
            }
        }
    }
}
