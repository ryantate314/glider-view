using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace GliderView.Adapters.FlightBook
{
    public class Aircraft
    {
        /*
            "acft": "Duo Discus xl",
            "addr": "3EF8EC",
            "alive": true,
            "alt": 199,
            "clr": -0.1,
            "cn": "2M",
            "gsp": 0,
            "lat": 51.462,
            "lat_dms": "51\u00b0 27' 43'' N",
            "lng": 7.840683333333334,
            "lng_dms": "7\u00b0 50' 26'' E",
            "receiv": "#TODO",
            "reg": "D-6229",
            "track": 90,
            "tsp": 1690397992,
            "type": 1,
            "utc": "2023-07-26 @ 18:59:52"
         */

        [JsonPropertyName("acft")]
        public string Model { get; set; }
        
        [JsonPropertyName("reg")]
        public string Registration { get; set; }

        [JsonPropertyName("cn")]
        public string ContestId { get; set; }

        [JsonPropertyName("alive")]
        public bool IsAlive { get; set; }

        [JsonPropertyName("alt")]
        public int AltitudeMeters { get; set; }

        [JsonPropertyName("gsp")]
        public int GroundSpeed { get; set; }

        [JsonPropertyName("lat")]
        public double Latitude { get; set; }

        [JsonPropertyName("lng")]
        public double Longitude { get; set; }

        [JsonPropertyName("tsp")]
        public DateTime LastCheckin { get; set; }
    }
}
