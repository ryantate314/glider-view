using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GliderView.Service.Models
{
    public class FlightStatistics
    {
        public int? ReleaseHeightMeters { get; set; }
        public int? MaxAltitudeMeters { get; set; }
        public int? AltitudeGainedMeters { get; set; }
        public float? DistanceTraveledKm { get; set; }
    }
}
