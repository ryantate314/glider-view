using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GliderView.Service.Models
{
    public class Rates
    {
        public decimal HookupCost { get; set; }
        public decimal CostPerHundredFeet { get; set; }
        /// <summary>
        /// Feet
        /// </summary>
        public int MinTowHeight { get; set; }
        public DateTime EffectiveDate { get; set; }
        public DateTime? ExpirationDate { get; set; }
    }

    public class AircraftRates
    {
        public Guid AircraftId { get; set; }
        public decimal RentalCostPerHour { get; set; }
        public float MinRentalHours { get; set; }
        public DateTime EffectiveDate { get; set; }
        public DateTime? ExpirationDate { get; set; }
    }
}
