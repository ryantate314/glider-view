using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using static System.Net.WebRequestMethods;

namespace GliderView.Service
{
    public interface IOgnDeviceDatabaseProvider
    {
        Task<OgnDeviceDatabaseProvider.AircraftInfo?> GetAircraftInfo(string trackerId);
    }

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

        public class Response
        {
            [JsonPropertyName("devices")]
            public List<AircraftInfo> Devices { get; set; }
        }

        public class AircraftInfo
        {
            [JsonPropertyName("device_type")]
            public char DeviceType { get; set; }

            [JsonPropertyName("device_id")]
            public string DeviceId { get; set; }

            [JsonPropertyName("aircraft_model")]
            public string AircraftModel { get; set; }

            [JsonPropertyName("registration")]
            public string Registration { get; set; }

            [JsonPropertyName("cn")]
            public string RacingNumber { get; set; }

            /// <summary>
            /// <see cref="OgnDeviceDatabaseProvider.AircraftType"/>
            /// </summary>
            [JsonPropertyName("aircraft_type")]
            public char AircraftType { get; set; }
        }

        public static class AircraftType
        {
            public const char Glider = '1';
            public const char TowPlane = '2';
        }
    }

    
}
