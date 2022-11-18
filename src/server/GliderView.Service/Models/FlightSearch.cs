using System.ComponentModel.DataAnnotations;

namespace GliderView.Service.Models
{
    public class FlightSearch
    {
        [Required]
        public DateTime? StartDate { get; set; }
        [Required]
        public DateTime? EndDate { get; set; }
        public Guid? PilotId { get; set; }
        public Guid? AircraftId { get; set; }


    }
}
