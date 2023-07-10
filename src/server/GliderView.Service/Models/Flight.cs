namespace GliderView.Service.Models
{
    public class Flight
    {
        public Guid FlightId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        /// <summary>
        /// Seconds
        /// </summary>
        public int Duration
        {
            get
            {
                return (int)(EndDate - StartDate).TotalSeconds;
            }
        }
        public string? IgcFileName { get; set; }

        // Setting this at the flight level because it is tied to the pilot, not the glider.
        public string? ContestId { get; set; }

        /// <summary>
        /// ICAO identifier of the destination of the flight.
        /// </summary>
        public string AirfieldId { get; set; }
        public Aircraft? Aircraft { get; set; }
        public Flight? TowFlight { get; set; }
        public FlightStatistics? Statistics { get; set; }

        // Optional properties (populated by request only)
        public List<Waypoint>? Waypoints { get; set; }
        public List<Occupant>? Occupants { get; set; }
        public PricingInfo? Cost { get; set; }
    }
}
