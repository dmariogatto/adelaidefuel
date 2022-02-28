using System;
using System.Diagnostics;

namespace AdelaideFuel.Functions.Models
{
    [DebuggerDisplay("{SiteName} {FuelName} {CurrentPrice}")]
    public class SitePriceException
    {
        public int BrandId { get; set; }
        public int SiteId { get; set; }
        public int FuelId { get; set; }

        public string SiteName { get; set; }
        public string FuelName { get; set; }

        public double PreviousPrice { get; set; }
        public double CurrentPrice { get; set; }
    }
}
