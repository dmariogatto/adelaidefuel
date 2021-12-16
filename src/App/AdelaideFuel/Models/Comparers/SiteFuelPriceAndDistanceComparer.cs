using System.Collections.Generic;

namespace AdelaideFuel.Models
{
    public class SiteFuelPriceAndDistanceComparer : IComparer<SiteFuelPriceAndDistance>
    {
        public int Compare(SiteFuelPriceAndDistance x, SiteFuelPriceAndDistance y)
        {
            var priceX = x.Price ?? new SiteFuelPrice();
            var priceY = y.Price ?? new SiteFuelPrice();

            var result = priceX.FuelSortOrder.CompareTo(priceY.FuelSortOrder);

            if (result == 0)
                result = x.RadiusKm.CompareTo(y.RadiusKm);

            if (result == 0)
                result = priceX.PriceInCents.CompareTo(priceY.PriceInCents);

            if (result == 0)
                result = x.DistanceKm.CompareTo(y.DistanceKm);

            return result;
        }
    }
}
