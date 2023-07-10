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
        public string AirfieldId { get; set; }

        // Aircraft
        public Guid? AircraftId { get; set; }
        public string? TrackerId { get; set; }
        public string? AircraftDescription { get; set; }
        public string? AircraftRegistration { get; set; }
        public int? NumSeats { get; set; }
        public bool? IsGlider { get; set; }

        // Tow plane
        public Guid? TowFlightId { get; set; }
        public string? TowAircraftDescription { get; set; }
        public string? TowAircraftRegistration { get; set; }
        public string? TowAircraftTrackerId { get; set; }
        public Guid? TowAircraftId { get; set; }

        // Statistics
        public int? MaxAltitude { get; set; }
        /// <summary>
        /// Kilometers
        /// </summary>
        public float? DistanceTraveled { get; set; }
        public int? AltitudeGained { get; set; }
        public int? ReleaseHeight { get; set; }
        public int? PatternEntryAltitude { get; set; }
        public string? ContestId { get; set; }
    }
}
