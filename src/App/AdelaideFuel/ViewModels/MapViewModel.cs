﻿using AdelaideFuel.Essentials;
using AdelaideFuel.Localisation;
using AdelaideFuel.Models;
using AdelaideFuel.Services;
using AdelaideFuel.Shared;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Devices;
using Microsoft.Maui.Devices.Sensors;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AdelaideFuel.ViewModels
{
    public class MapViewModel : BaseViewModel
    {
        public readonly double InitialRadiusMetres;

        private readonly IAppClock _clock;

        private readonly IPermissions _permissions;
        private readonly IGeolocation _geolocation;
        private readonly IMap _map;

        private readonly SemaphoreSlim _sitesSemaphore = new SemaphoreSlim(1, 1);

        private List<int> _userBrandIds;
        private CancellationTokenSource _sitesCancellation;

        private readonly Dictionary<PriceCategory, FuelCategory> _fuelCategories = new Dictionary<PriceCategory, FuelCategory>()
        {
            { PriceCategory.Lowest, new FuelCategory(PriceCategory.Lowest) },
            { PriceCategory.Low, new FuelCategory(PriceCategory.Low) },
            { PriceCategory.Average, new FuelCategory(PriceCategory.Average) },
            { PriceCategory.High, new FuelCategory(PriceCategory.High) },
            { PriceCategory.Highest, new FuelCategory(PriceCategory.Highest) }
        };

        public MapViewModel(
            IAppClock clock,
            IDeviceInfo deviceInfo,
            IPermissions permissions,
            IGeolocation geolocation,
            IMap map,
            IBvmConstructor bvmConstructor) : base(bvmConstructor)
        {
            Title = Resources.Map;

            InitialRadiusMetres = deviceInfo.Idiom == DeviceIdiom.Tablet ? 4800d : 2600d;

            _clock = clock;

            _permissions = permissions;
            _geolocation = geolocation;
            _map = map;

            Sites = new ObservableRangeCollection<Site>();
            FilteredSites = new ObservableRangeCollection<Site>();
            Fuels = new ObservableRangeCollection<UserFuel>();
            FuelCategories = new ObservableRangeCollection<FuelCategory>();

            LoadSitesCommand = new AsyncRelayCommand<UserFuel>(LoadSitesAsync);
            LoadFuelsCommand = new AsyncRelayCommand<int>(LoadFuelsAsync);
            LaunchMapCommand = new AsyncRelayCommand<Site>(LaunchMapAsync);
            GoToSiteSearchCommand = new AsyncRelayCommand(() => NavigationService.NavigateToAsync<SiteSearchViewModel>(new Dictionary<string, string>()
            {
                { NavigationKeys.FuelIdQueryProperty, (Fuel?.Id ?? -1).ToString()  }
            }));

            CheckAndRequestLocationCommand = new AsyncRelayCommand(CheckAndRequestLocationAsync);
        }

        #region Overrides
        public override void OnAppearing()
        {
            base.OnAppearing();

            CheckAndRequestLocationCommand.Execute(null);

            TrackEvent(Events.PageView.MapView);
        }
        #endregion

        private bool _initialLoadComplete;
        public bool InitialLoadComplete
        {
            get => _initialLoadComplete;
            set => SetProperty(ref _initialLoadComplete, value);
        }

        private bool _enableMyLocation;
        public bool EnableMyLocation
        {
            get => _enableMyLocation;
            set => SetProperty(ref _enableMyLocation, value);
        }

        private Coords? _initialCameraUpdate;
        public Coords InitialCameraUpdate
        {
            get => _initialCameraUpdate ?? Constants.AdelaideCenter.WithRadius(InitialRadiusMetres);
            set => SetProperty(ref _initialCameraUpdate, value);
        }

        private Coords _mapPosition;
        public Coords MapPosition
        {
            get => _mapPosition;
            set => SetProperty(ref _mapPosition, value);
        }

        private UserFuel _fuel;
        public UserFuel Fuel
        {
            get => _fuel;
            set
            {
                if (SetProperty(ref _fuel, value))
                    LoadSitesCommand.ExecuteAsync(value);
            }
        }

        private UserFuel _loadedFuel;
        public UserFuel LoadedFuel
        {
            get => _loadedFuel;
            private set => SetProperty(ref _loadedFuel, value);
        }

        private Site _selectedSite;
        public Site SelectedSite
        {
            get => _selectedSite;
            set
            {
                if (SetProperty(ref _selectedSite, value) && value is Site site)
                {
                    _permissions.CheckStatusAsync<Permissions.LocationWhenInUse>().ContinueWith(r =>
                    {
                        if (r.Result == PermissionStatus.Granted)
                        {
                            _geolocation.GetLastKnownLocationAsync().ContinueWith(r =>
                            {
                                site.LastKnownDistanceKm = r.Result is not null
                                    ? r.Result.CalculateDistance(site.Latitude, site.Longitude, DistanceUnits.Kilometers)
                                    : -1;
                            }, TaskScheduler.FromCurrentSynchronizationContext());
                        }
                    }, TaskScheduler.FromCurrentSynchronizationContext());
                }
            }
        }

        private DateTime _modifiedUtc = DateTime.MinValue;
        public DateTime ModifiedUtc
        {
            get => _modifiedUtc;
            set => SetProperty(ref _modifiedUtc, value);
        }

        private DateTime _lastLoadedUtc = DateTime.MinValue;
        public DateTime LastLoadedUtc
        {
            get => _lastLoadedUtc;
            set => SetProperty(ref _lastLoadedUtc, value);
        }

        public ObservableRangeCollection<Site> Sites { get; private set; }
        public ObservableRangeCollection<Site> FilteredSites { get; private set; }

        public ObservableRangeCollection<UserFuel> Fuels { get; private set; }
        public ObservableRangeCollection<FuelCategory> FuelCategories { get; private set; }

        public AsyncRelayCommand<int> LoadFuelsCommand { get; private set; }
        public AsyncRelayCommand<UserFuel> LoadSitesCommand { get; private set; }
        public AsyncRelayCommand<Site> LaunchMapCommand { get; private set; }
        public AsyncRelayCommand GoToSiteSearchCommand { get; private set; }

        public AsyncRelayCommand CheckAndRequestLocationCommand { get; private set; }

        private async Task LoadFuelsAsync(int defaultFuelId)
        {
            try
            {
                var fuels = (await FuelService.GetUserFuelsAsync(default))
                        ?.Where(i => i.IsActive)?.ToList();

                if (!Fuels.SequenceEqual(fuels))
                    Fuels.ReplaceRange(fuels);

                if (!Fuels.Contains(Fuel))
                    Fuel = Fuels.FirstOrDefault(i => i.Id == defaultFuelId) ?? Fuels.FirstOrDefault();
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
        }

        private async Task LoadSitesAsync(UserFuel fuel)
        {
            if (fuel is null)
                return;

            var localCancelCopy = _sitesCancellation;
            _sitesCancellation = new CancellationTokenSource();

            var ct = _sitesCancellation.Token;
            localCancelCopy?.Cancel();

            await _sitesSemaphore.WaitAsync();

            if (!ct.IsCancellationRequested && Fuel == fuel)
            {
                IsBusy = true;

                try
                {
                    var pricesTask = FuelService.GetSitePricesAsync(ct);
                    var userBrandsTask = FuelService.GetUserBrandsAsync(ct);

                    if (!Sites.Any())
                    {
                        var sites = await FuelService.GetSitesAsync(ct);
                        Sites.ReplaceRange(sites.Select(i => new Site(i)));
                    }

                    await Task.WhenAll(pricesTask, userBrandsTask);

                    // Don't care if sort order changed, just active state
                    var brandIds = userBrandsTask.Result
                        .Where(i => i.IsActive)
                        .Select(i => i.Id)
                        .OrderBy(i => i)
                        .ToList();

                    var userBrandsChanged = !_userBrandIds?.SequenceEqual(brandIds) ?? true;
                    if (userBrandsChanged)
                        _userBrandIds = brandIds;

                    var (prices, modifiedUtc) = pricesTask.Result;

                    if (!ct.IsCancellationRequested && (userBrandsChanged || modifiedUtc > ModifiedUtc || fuel.Id != LoadedFuel?.Id))
                    {
                        var priceLookup = prices
                            .GroupBy(i => i.SiteId)
                            .ToDictionary(g => g.Key, g => g);
                        var fuelPrices = prices
                            .Where(i => i.FuelId == fuel.Id && i.PriceInCents != Constants.OutOfStockPriceInCents)
                            .Select(i => i.PriceInCents)
                            .ToList();

                        var validCategories = Enumerable.Empty<FuelCategory>();

                        var fns = Statistics.FiveNumberSummary(fuelPrices);

                        var q1 = fns[1];
                        var q2 = fns[2];
                        var q3 = fns[3];

                        var q375 = (q1 + q2) / 2d;
                        var q625 = (q2 + q3) / 2d;

                        if (fuelPrices.Count > 0)
                        {
                            if (fuelPrices.Count < Statistics.MinLengthForFns)
                            {
                                // set this for the categorisation below
                                q1 = fuelPrices.Last();

                                _fuelCategories[PriceCategory.Lowest].LowerBound = (int)fuelPrices.First();
                                _fuelCategories[PriceCategory.Lowest].UpperBound = (int)q1;

                                validCategories = new[] { _fuelCategories[PriceCategory.Lowest] };
                            }
                            else if (q1.FuzzyEquals(q3, 0.1))
                            {
                                _fuelCategories[PriceCategory.Lowest].LowerBound =
                                _fuelCategories[PriceCategory.Lowest].UpperBound = (int)q1;

                                validCategories = new[] { _fuelCategories[PriceCategory.Lowest] };
                            }
                            else
                            {
                                _fuelCategories[PriceCategory.Lowest].LowerBound = 0;
                                _fuelCategories[PriceCategory.Lowest].UpperBound = (int)q1;

                                _fuelCategories[PriceCategory.Low].LowerBound = (int)q1;
                                _fuelCategories[PriceCategory.Low].UpperBound = (int)q375;

                                _fuelCategories[PriceCategory.Average].LowerBound = (int)q375;
                                _fuelCategories[PriceCategory.Average].UpperBound = (int)q625;

                                _fuelCategories[PriceCategory.High].LowerBound = (int)q625;
                                _fuelCategories[PriceCategory.High].UpperBound = (int)q3;

                                _fuelCategories[PriceCategory.Highest].LowerBound = (int)q3;
                                _fuelCategories[PriceCategory.Highest].UpperBound = 0;

                                validCategories = _fuelCategories
                                    .Values
                                    .Where(i => i.LowerBound != i.UpperBound);
                            }
                        }

                        FuelCategories.ReplaceRange(validCategories);

                        foreach (var s in Sites)
                        {
                            if (priceLookup.TryGetValue(s.Id, out var sitePrices))
                            {
                                s.Prices.ReplaceRange(sitePrices);
                                s.SelectedFuelPrice = s.Prices.FirstOrDefault(p => p.FuelId == Fuel.Id);
                                s.LastUpdatedUtc = sitePrices.Max(p => p.ModifiedUtc);
                            }
                            else
                            {
                                s.SelectedFuelPrice = null;
                                s.Prices.Clear();
                            }

                            if (s.SelectedFuelPrice is SiteFuelPrice fp && fp.PriceInCents != Constants.OutOfStockPriceInCents)
                            {
                                if (fp.PriceInCents <= q1)
                                    s.PriceCategory = PriceCategory.Lowest;
                                else if (fp.PriceInCents > q1 && fp.PriceInCents < q375)
                                    s.PriceCategory = PriceCategory.Low;
                                else if (fp.PriceInCents >= q375 && fp.PriceInCents <= q625)
                                    s.PriceCategory = PriceCategory.Average;
                                else if (fp.PriceInCents > q625 && fp.PriceInCents < q3)
                                    s.PriceCategory = PriceCategory.High;
                                else if (fp.PriceInCents >= q3)
                                    s.PriceCategory = PriceCategory.Highest;
                                else
                                    s.PriceCategory = PriceCategory.Unknown;
                            }
                            else
                                s.PriceCategory = PriceCategory.Unknown;
                        }

                        var newFiltered = Sites.Where(s => s.SelectedFuelPrice?.FuelId == Fuel.Id);
                        var toRemove = FilteredSites.Except(newFiltered).ToList();
                        var toAdd = newFiltered.Except(FilteredSites).ToList();

                        if (SelectedSite is not null && toRemove.Contains(SelectedSite))
                            SelectedSite = null;

                        FilteredSites.RemoveRange(toRemove, NotifyCollectionChangedAction.Remove);
                        FilteredSites.AddRange(toAdd);

                        LastLoadedUtc = _clock.UtcNow;
                        ModifiedUtc = modifiedUtc;
                    }

                    if (!ct.IsCancellationRequested)
                    {
                        InitialLoadComplete = true;
                        LoadedFuel = fuel;
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error(ex);
                }
                finally
                {
                    IsBusy = false;
                }
            }

            _sitesSemaphore.Release();

            if (!ct.IsCancellationRequested && !_sitesCancellation.IsCancellationRequested)
            {
                IsBusy = false;
            }
        }

        private async Task LaunchMapAsync(Site site)
        {
            if (string.IsNullOrEmpty(site?.Name))
                return;

            try
            {
                var location = new Location(site.Latitude, site.Longitude);
                var options = new MapLaunchOptions()
                {
                    Name = site.Name,
                    NavigationMode = NavigationMode.Driving
                };

                await _map.OpenAsync(location, options);
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
        }

        private async Task CheckAndRequestLocationAsync()
        {
            try
            {
                var status = await _permissions.CheckAndRequestAsync<Permissions.LocationWhenInUse>();
                if (status == PermissionStatus.Granted)
                {
                    var loc = await _geolocation.GetLastKnownLocationAsync() ??
                              await _geolocation.GetLocationAsync(new GeolocationRequest(GeolocationAccuracy.Medium, TimeSpan.FromSeconds(3)));

                    if (!_initialCameraUpdate.HasValue && loc is not null)
                    {
                        var distanceFromCenter = loc.CalculateDistance(Constants.SaCenter.Latitude, Constants.SaCenter.Longitude, DistanceUnits.Kilometers);
                        if (distanceFromCenter * 1000d <= Constants.SaCenter.RadiusMetres)
                            InitialCameraUpdate = new Coords(loc.Latitude, loc.Longitude, InitialRadiusMetres);
                    }

                    EnableMyLocation = true;
                }
            }
            catch (Exception ex)
            {
                switch (ex)
                {
                    case FeatureNotSupportedException _:
                    case FeatureNotEnabledException _:
                    case PermissionException _:
                        EnableMyLocation = false;
                        break;
                    default:
                        Logger.Error(ex);
                        break;
                }
            }
        }
    }
}