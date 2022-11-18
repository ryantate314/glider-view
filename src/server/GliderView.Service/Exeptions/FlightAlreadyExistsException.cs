using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GliderView.Service.Exeptions
{
    public class FlightAlreadyExistsException : Exception
    {
        public string TrackerId { get; set; }
        public DateTime? StartDate { get; set; }

        public FlightAlreadyExistsException(string trackerId, DateTime? startDate)
            : base(String.Format("Flight already exists for tracker {0} at {1}.", trackerId, startDate))
        {
            TrackerId = trackerId;
            StartDate = startDate;
        }
    }
}
