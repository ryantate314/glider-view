using GliderView.Service.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GliderView.Data.Models
{
    public class FlightStatistic
    {
        public Guid FlightId { get; set; }
        public Statistic Statistic { get; set;}
        public float? Value { get; set; }
    }
}
