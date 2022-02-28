using System;
using System.Diagnostics;

namespace AdelaideFuel.Models
{
    [DebuggerDisplay("({RadiusKm}km) {Price}")]
    public struct SiteFuelPriceAndDistance
    {
        public SiteFuelPriceAndDistance(SiteFuelPrice price, double distanceKm, int radiusKm)
        {
            Price = price;
            DistanceKm = distanceKm;
            RadiusKm = radiusKm;
        }

        public SiteFuelPrice Price { get; }
        public double DistanceKm { get; }
        public int RadiusKm { get; }
    }
}