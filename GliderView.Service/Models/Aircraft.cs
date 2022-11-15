using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GliderView.Service.Models
{
    public class Aircraft
    {
        public Guid AircraftId { get; set; }
        public string? Description { get; set; }
        public string? TrackerId { get; set; }
        public string? RegistrationId { get; set; }
        public int? NumSeats { get; set; }
    }
}
