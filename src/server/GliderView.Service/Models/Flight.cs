namespace GliderView.Service.Models
{
    public class Flight
    {
        public Guid FlightId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int Duration
        {
            get
            {
                return (int)(EndDate - StartDate).TotalSeconds;
            }
        }
        public string? IgcFileName { get; set; }

        /// <summary>
        /// The ID of the corresponding tow plane flight.
        /// </summary>
        public Guid? TowFlightId { get; set; }

        public IEnumerable<Waypoint>? Waypoints { get; set; }
        public Aircraft? Aircraft { get; set; }
    }
}
