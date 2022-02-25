using System.Collections.Generic;

namespace AdelaideFuel.Models
{
    public class SiteFuelPriceAndDistanceComparer : IComparer<SiteFuelPriceAndDistance>
    {
        public int Compare(SiteFuelPriceAndDistance x, SiteFuelPriceAndDistance y)
        {
            var priceX = x.Price ?? new SiteFuelPrice();
            var priceY = y.Price ?? new SiteFuelPrice();

            var cmp = priceX.FuelSortOrder.CompareTo(priceY.FuelSortOrder);

            if (cmp == 0)
                cmp = x.RadiusKm.CompareTo(y.RadiusKm);
            if (cmp == 0)
                cmp = priceX.PriceInCents.CompareTo(priceY.PriceInCents);
            if (cmp == 0)
                cmp = x.DistanceKm.CompareTo(y.DistanceKm);

            if (cmp == 0)
                cmp = priceX.BrandSortOrder.CompareTo(priceY.BrandSortOrder);
            if (cmp == 0)
                cmp = priceX.SiteId.CompareTo(priceY.SiteId);

            return cmp;
        }
    }
}
