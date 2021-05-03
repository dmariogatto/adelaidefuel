using AdelaideFuel.Localisation;
using AdelaideFuel.Shared;
using MvvmHelpers;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AdelaideFuel.Models
{
    public class Site : ObservableObject, IMapPin
    {
        public Site(SiteDto site)
        {
            Id = site.SiteId;
            BrandId = site.BrandId;
            Name = site.Name;
            Address = site.Address;
            Latitude = site.Latitude;
            Longitude = site.Longitude;

            OpeningHours = site.GetOpeningHours();
            Prices = new ObservableRangeCollection<SiteFuelPrice>();
        }

        private SiteFuelPrice _selectedFuelPrice;
        public SiteFuelPrice SelectedFuelPrice
        {
            get => _selectedFuelPrice;
            set
            {
                SetProperty(ref _selectedFuelPrice, value);
                OnPropertyChanged(nameof(Description));
            }
        }

        public IDictionary<DayOfWeek, OpeningHour> OpeningHours { get; }
        public ObservableRangeCollection<SiteFuelPrice> Prices { get; }

        private int _id;
        public int Id
        {
            get => _id;
            set => SetProperty(ref _id, value);
        }

        private int _branId;
        public int BrandId
        {
            get => _branId;
            set => SetProperty(ref _branId, value);
        }

        private string _name;
        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        private string _address;
        public string Address
        {
            get => _address;
            set => SetProperty(ref _address, value);
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

        private PriceCategory _priceCategory;
        public PriceCategory PriceCategory
        {
            get => _priceCategory;
            set => SetProperty(ref _priceCategory, value);
        }

        private double _lastKnownDistanceKm;
        public double LastKnownDistanceKm
        {
            get => _lastKnownDistanceKm;
            set => SetProperty(ref _lastKnownDistanceKm, value);
        }

        private DateTime _lastUpdatedUtc = DateTime.MinValue;
        public DateTime LastUpdatedUtc
        {
            get => _lastUpdatedUtc;
            set => SetProperty(ref _lastUpdatedUtc, value);
        }

        #region IMapPin
        public string Label => Name;
        public string Description => SelectedFuelPrice != null
            ? string.Format(Resources.ItemDashItem, SelectedFuelPrice.FuelName, SelectedFuelPrice.PriceInCents)
            : Address;
        public Coords Position => new Coords(Latitude, Longitude);
        public int ZIndex => 1000;
        #endregion
    }
}