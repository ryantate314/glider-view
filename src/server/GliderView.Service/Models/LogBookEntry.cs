using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GliderView.Service.Models
{
    public class LogBookEntry
    {
        public Guid FlightId { get; set; }
        public Flight Flight { get; set; }
        public int? FlightNumber { get; set; }
        public string? Remarks { get; set; }

    }
}
