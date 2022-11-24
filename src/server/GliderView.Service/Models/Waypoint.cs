using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace GliderView.Service.Models
{
    public class Waypoint
    {
        [JsonIgnore]
        public int? WaypointId { get; set; }

        public DateTime Time { get; set; }
        public int GpsAltitude { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }

        public FlightEventType? FlightEvent { get; set; }
    }
}
