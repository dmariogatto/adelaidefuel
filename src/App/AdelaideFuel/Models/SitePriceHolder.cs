using AdelaideFuel.Shared;
using System;

namespace AdelaideFuel.Models
{
    public class SitePriceHolder
    {
        public SitePriceHolder(
            UserBrand brand,
            UserFuel fuel,
            SiteDto site,
            SitePriceDto sitePrice)
        {
            Brand = brand ?? throw new ArgumentNullException(nameof(brand));
            Fuel = fuel ?? throw new ArgumentNullException(nameof(fuel));
            Site = site ?? throw new ArgumentNullException(nameof(site));
            SitePrice = sitePrice ?? throw new ArgumentNullException(nameof(sitePrice));
        }

        public UserBrand Brand { get; }
        public UserFuel Fuel { get; }

        public SiteDto Site { get; }
        public SitePriceDto SitePrice { get; }
    }
}