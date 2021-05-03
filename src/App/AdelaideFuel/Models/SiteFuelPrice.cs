using AdelaideFuel.Shared;
using MvvmHelpers;
using System;
using System.Diagnostics;

namespace AdelaideFuel.Models
{
    [DebuggerDisplay("{FuelName} - {PriceInCents}")]
    public class SiteFuelPrice : ObservableObject
    {
        public SiteFuelPrice() { }

        public SiteFuelPrice(UserBrand brand, UserFuel fuel, SiteDto site, SitePriceDto sitePrice)
        {
            BrandId = brand.Id;
            BrandName = brand.Name;
            BrandSortOrder = brand.SortOrder;

            FuelId = fuel.Id;
            FuelName = fuel.Name;
            FuelSortOrder = fuel.SortOrder;

            SiteId = site.SiteId;
            SiteName = site.Name;
            SiteAddress = site.Address;
            Latitude = site.Latitude;
            Longitude = site.Longitude;

            PriceInCents = sitePrice.PriceInCents();
            ModifiedUtc = sitePrice.TransactionDateUtc;
        }

        #region Brand
        private int _brandId;
        public int BrandId
        {
            get => _brandId;
            set => SetProperty(ref _brandId, value);
        }

        private string _brandName;
        public string BrandName
        {
            get => _brandName;
            set => SetProperty(ref _brandName, value);
        }

        private int _brandSortOrder;
        public int BrandSortOrder
        {
            get => _brandSortOrder;
            set => SetProperty(ref _brandSortOrder, value);
        }
        #endregion

        #region Site
        private int _siteId;
        public int SiteId
        {
            get => _siteId;
            set => SetProperty(ref _siteId, value);
        }

        private string _siteName;
        public string SiteName
        {
            get => _siteName;
            set => SetProperty(ref _siteName, value);
        }

        private string _siteAddress;
        public string SiteAddress
        {
            get => _siteAddress;
            set => SetProperty(ref _siteAddress, value);
        }

        private double _latitude;
        public double Latitude
        {
            get => _latitude;
            set => SetProperty(ref _latitude, value);
        }

        private double _longitude;
        public double Longitude
        {
            get => _longitude;
            set => SetProperty(ref _longitude, value);
        }
        #endregion

        #region Fuel
        private int _fuelId;
        public int FuelId
        {
            get => _fuelId;
            set => SetProperty(ref _fuelId, value);
        }

        private string _fuelName;
        public string FuelName
        {
            get => _fuelName;
            set => SetProperty(ref _fuelName, value);
        }

        private int _fuelSortOrder;
        public int FuelSortOrder
        {
            get => _fuelSortOrder;
            set => SetProperty(ref _fuelSortOrder, value);
        }
        #endregion

        #region Price
        private double _priceInCents;
        public double PriceInCents
        {
            get => _priceInCents;
            set => SetProperty(ref _priceInCents, value);
        }

        private DateTime _modifiedUtc;
        public DateTime ModifiedUtc
        {
            get => _modifiedUtc;
            set => SetProperty(ref _modifiedUtc, value);
        }
        #endregion
    }
}