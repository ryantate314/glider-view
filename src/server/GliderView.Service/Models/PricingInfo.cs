using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GliderView.Service.Models
{
    public class PricingInfo
    {
        public decimal Total { get; set; }
        public List<LineItem> LineItems { get; set; }
            = new List<LineItem>();
    }

    public class LineItem
    {
        public string Description { get; set; }
        public float? Quantity { get; set; }
        public decimal? UnitCost { get; set; }
        public decimal TotalCost { get; set; }
        public string? Units { get; set; }
    }
}
