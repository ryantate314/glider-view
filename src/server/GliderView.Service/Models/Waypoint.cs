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
        public decimal Latitude { get; set; }
        public decimal Longitude { get; set; }
    }
}
