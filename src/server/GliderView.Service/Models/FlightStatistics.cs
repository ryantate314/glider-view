using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GliderView.Service.Models
{
    public class FlightStatistics
    {
        public int? ReleaseHeight { get; set; }
        public int? MaxAltitude { get; set; }
        public int? AltitudeGained { get; set; }
        /// <summary>
        /// Kilometers
        /// </summary>
        public float? DistanceTraveled { get; set; }
        public int? PatternEntryAltitude { get; set; }
        public float? MaxDistanceFromField { get; set; }
    }
}
