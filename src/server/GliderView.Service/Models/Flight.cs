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

        public IEnumerable<Waypoint>? Waypoints { get; set; }
        public Aircraft? Aircraft { get; set; }
        public Flight? TowFlight { get; set; }
    }
}
