using GliderView.Service.Models;

namespace GliderView.API.Models
{
    public class LeaderboardDto
    {
        public DateTime Date { get; set; }

        public int NumFlightsThisYear { get; set; }
       
        public List<Flight> LongestDurationFlightsThisYear { get; set; }
        public List<Flight> LongestLengthFlightsThisYear { get; set; }

        public int NumFlightsThisMonth { get; set; }
        public List<Flight> LongestLengthFlightsThisMonth { get; set; }
        public List<Flight> LongestDurationFlightsThisMonth { get; set; }

        public int NumFlightsToday { get; set; }
        public List<Flight> LongestLengthFlightsToday { get; set; }
        public List<Flight> LongestDurationFlightsToday { get; set; }


        public List<AircraftUsage> AircraftUsage { get; set; }

    }

    public class AircraftUsage
    {
        public Guid AircraftId { get; set; }
        public string Description { get; set; }
        public string Registration { get; set; }

        public TimeSpan UsageThisMonth { get; set; }
        /// <summary>
        /// Meters
        /// </summary>
        public float DistanceTraveledThisMonth { get; set; }
        public TimeSpan UsageThisYear { get; set; }
        /// <summary>
        /// Meters
        /// </summary>
        public float DistanceTraveledThisYear { get; set; }
        public TimeSpan UsageToday { get; set; }
        /// <summary>
        /// Meters
        /// </summary>
        public float DistanceTraveledToday { get; set; }
    }
}
