using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GliderView.Data.Models
{
    public class Flight
    {
        public Guid FlightId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string? IgcFilename { get; set; }

        // Aircraft
        public Guid? AircraftId { get; set; }
        public string? TrackerId { get; set; }
        public string? AircraftDescription { get; set; }
        public string? AircraftRegistration { get; set; }
        public int? NumSeats { get; set; }
        public bool? IsGlider { get; set; }
    }
}
