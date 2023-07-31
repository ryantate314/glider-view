using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace GliderView.Service.Models.FlightBook
{
    public class AircraftLocationUpdate
    {
        public string Model { get; set; }
        public string Registration { get; set; }

        public bool IsAlive { get; set; }

        public int Altitude { get; set; }

        public double Latitude { get; set; }

        public double Longitude { get; set; }

        public DateTime LastCheckin { get; set; }

        // HACK: These are populated by the AirfieldService, not Flightbook.
        public double DistanceFromFieldKm { get; set; }
        public int BearingFromField { get; set; }
        public string ContestId { get; set; }
    }
}
