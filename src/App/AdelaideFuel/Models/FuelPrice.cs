using AdelaideFuel.Localisation;
using System;

namespace AdelaideFuel.Models
{
    public class SiteFuelPriceItem : SiteFuelPrice
    {
        public SiteFuelPriceItem() { }

        public SiteFuelPriceItem(SiteFuelPrice siteFuelPrice)
        {
            SetFuelPrice(siteFuelPrice);
        }

        public SiteFuelPriceItem(SiteFuelPriceItem siteFuelPriceItem)
        {
            SetFuelPrice(siteFuelPriceItem);
        }

        public void SetFuelPrice(SiteFuelPrice siteFuelPrice)
        {
            BrandId = siteFuelPrice?.BrandId ?? 0;
            BrandName = siteFuelPrice?.BrandName ?? string.Empty;
            BrandSortOrder = siteFuelPrice?.BrandSortOrder ?? 0;

            FuelId = siteFuelPrice?.FuelId ?? 0;
            FuelName = siteFuelPrice?.FuelName ?? string.Empty;
            FuelSortOrder = siteFuelPrice?.FuelSortOrder ?? 0;

            SiteId = siteFuelPrice?.SiteId ?? 0;
            SiteName = siteFuelPrice?.SiteName ?? string.Empty;
            SiteAddress = siteFuelPrice?.SiteAddress ?? string.Empty;
            Latitude = siteFuelPrice?.Latitude ?? 0;
            Longitude = siteFuelPrice?.Longitude ?? 0;

            PriceInCents = siteFuelPrice?.PriceInCents ?? 0;
            ModifiedUtc = siteFuelPrice?.ModifiedUtc ?? DateTime.MinValue;

            OnPropertyChanged(nameof(IsClear));
        }

        public void SetFuelPrice(SiteFuelPriceItem siteFuelPriceItem)
        {
            LastKnowDistanceKm = siteFuelPriceItem?.LastKnowDistanceKm ?? 0;
            RadiusKm = siteFuelPriceItem?.RadiusKm ?? 0;

            SetFuelPrice((SiteFuelPrice)siteFuelPriceItem);
        }

        public void Clear() => SetFuelPrice(null);
        public bool IsClear => FuelId <= 0;

        private double _lastKnowDistanceKm;
        public double LastKnowDistanceKm
        {
            get => _lastKnowDistanceKm;
            set => SetProperty(ref _lastKnowDistanceKm, value);
        }

        private int _radiusKm;
        public int RadiusKm
        {
            get => _radiusKm;
            set
            {
                SetProperty(ref _radiusKm, value);
                OnPropertyChanged(nameof(Description));
            }
        }

        private bool _closest;
        public bool Closest
        {
            get => _closest;
            set
            {
                SetProperty(ref _closest, value);
                OnPropertyChanged(nameof(Description));
            }
        }

        private bool _cheapestInSa;
        public bool CheapestInSa
        {
            get => _cheapestInSa;
            set
            {
                SetProperty(ref _cheapestInSa, value);
                OnPropertyChanged(nameof(Description));
            }
        }

        public string Description
            => Closest
               ? string.Format(Resources.CheapestAndClosestInItem, CheapestInSa ? Resources.SA : string.Format(Resources.ItemKm, RadiusKm))
               : string.Format(Resources.CheapestInItem, CheapestInSa ? Resources.SA : string.Format(Resources.ItemKm, RadiusKm));
    }
}