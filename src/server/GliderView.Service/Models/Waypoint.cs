using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GliderView.Service.Models
{
    public class Waypoint
    {
        public DateTime Time { get; set; }
        public int GpsAltitude { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }
}
