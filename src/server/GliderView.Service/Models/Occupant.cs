using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GliderView.Service.Models
{
    public class Occupant
    {
        public Guid FlightId { get; set; }
        public Guid UserId { get; set; }
        public string Name { get; set; }
        public string Notes { get; set; }
        public int? FlightNumber { get; set; }
    }
}
