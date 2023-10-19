using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace GliderView.Service.Models.OgnDatabaseProvider
{
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
        public string ContestNumber { get; set; }

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
