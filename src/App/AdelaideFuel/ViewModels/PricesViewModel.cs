using AdelaideFuel.Essentials;
using AdelaideFuel.Localisation;
using AdelaideFuel.Models;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Networking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AdelaideFuel.ViewModels
{
    public class PricesViewModel : BaseViewModel
    {
        private readonly IConnectivity _connectivity;
        private readonly IPermissions _permissions;
        private readonly IVersionTracking _versionTracking;
        private readonly IAppActions _appActions;

        private IReadOnlyList<UserFuel> _userFuels = [];
        private IReadOnlyList<UserRadius> _userRadii = [];

        public PricesViewModel(
            IConnectivity connectivity,
            IPermissions permissions,
            IVersionTracking versionTracking,
            IAppActions appActions,
            IBvmConstructor bvmConstructor) : base(bvmConstructor)
        {
            _connectivity = connectivity;
            _permissions = permissions;
            _versionTracking = versionTracking;
            _appActions = appActions;

            Title = Resources.Prices;

            LoadFuelPriceGroupsCommand = new AsyncRelayCommand<CancellationToken>(LoadFuelPriceGroupsAsync);
            FuelPriceTappedCommand = new AsyncRelayCommand<SiteFuelPriceItem>((fp) => fp?.SiteId is not null
                ? NavigationService.NavigateToAsync<MapViewModel>(new Dictionary<string, string>()
                {
                    { NavigationKeys.SiteIdQueryProperty, fp.SiteId.ToString() },
                    { NavigationKeys.FuelIdQueryProperty, fp.FuelId.ToString() }
                })
                : Task.CompletedTask);
            GoToMapCommand = new AsyncRelayCommand(() => NavigationService.NavigateToAsync<MapViewModel>());
            GoToSettingsCommand = new AsyncRelayCommand(() => NavigationService.NavigateToAsync<SettingsViewModel>());
        }

        #region Overrides
        public override void OnAppearing()
        {
            base.OnAppearing();

            HasInternet = _connectivity.NetworkAccess == NetworkAccess.Internet;
            _connectivity.ConnectivityChanged += ConnectivityChanged;

            TrackEvent(Events.PageView.HomeView);
        }

        public override void OnDisappearing()
        {
            base.OnDisappearing();

            _connectivity.ConnectivityChanged -= ConnectivityChanged;
        }
        #endregion

        private bool _hasInternet = true;
        public bool HasInternet
        {
            get => _hasInternet;
            set => SetProperty(ref _hasInternet, value);
        }

        private DateTime _modifiedUtc = DateTime.MinValue;
        public DateTime ModifiedUtc
        {
            get => _modifiedUtc;
            set => SetProperty(ref _modifiedUtc, value);
        }

        private bool _hasPrices = false;
        public bool HasPrices
        {
            get => _hasPrices;
            set => SetProperty(ref _hasPrices, value);
        }

        private bool _noPricesFound = false;
        public bool NoPricesFound
        {
            get => _noPricesFound;
            set => SetProperty(ref _noPricesFound, value);
        }

        private bool _noLocation = false;
        public bool NoLocation
        {
            get => _noLocation;
            set => SetProperty(ref _noLocation, value);
        }

        private IReadOnlyList<PriceItemByFuelGrouping> _fuelPriceGroups = [];
        public IReadOnlyList<PriceItemByFuelGrouping> FuelPriceGroups
        {
            get => _fuelPriceGroups;
            private set => SetProperty(ref _fuelPriceGroups, value);
        }

        public AsyncRelayCommand<CancellationToken> LoadFuelPriceGroupsCommand { get; private set; }
        public AsyncRelayCommand<SiteFuelPriceItem> FuelPriceTappedCommand { get; private set; }
        public AsyncRelayCommand GoToMapCommand { get; private set; }
        public AsyncRelayCommand GoToSettingsCommand { get; private set; }

        private async Task LoadFuelPriceGroupsAsync(CancellationToken ct)
        {
            if (IsBusy)
                return;

            IsBusy = true;

            var firstLoad = !FuelPriceGroups.Any();

            try
            {
                await FuelService.SyncAllAsync(default);

                await LoadGroupsAsync(ct);
                await UpdatePricesAsync(ct);

                HasPrices = FuelPriceGroups.Any(g => g.HasPrices);
                NoPricesFound = !HasPrices;

                await SetupAppShortcutsAsync();
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                ClearGroupsAndSetError();
            }
            finally
            {
                IsBusy = false;
            }

            if (firstLoad && HasInternet)
            {
                if (_versionTracking.IsFirstLaunchEver)
                {
                    var config = await DialogService.ConfirmAsync(
                                    Resources.FuelSetup,
                                    Resources.SaBowser,
                                    Resources.Now,
                                    Resources.Later);

                    if (config)
                        await NavigationService.NavigateToAsync<FuelsViewModel>();

                    Logger.Event(Events.Action.FuelSetup, new Dictionary<string, string>()
                    {
                        { nameof(config), config.ToString() }
                    });
                }
                else if (NoLocation && FuelPriceGroups.Any())
                {
                    try
                    {
                        var status = await _permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
                        if (status == PermissionStatus.Granted)
                        {
                            // First load & no location - will try again
                            _ = Task.Delay(2000, ct)
                                    .ContinueWith(t =>
                                    {
                                        if (!ct.IsCancellationRequested)
                                            _ = LoadFuelPriceGroupsCommand.ExecuteAsync(ct);
                                    }, TaskScheduler.FromCurrentSynchronizationContext());
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine(ex);
                    }
                }
            }
        }

        private async Task<bool> LoadGroupsAsync(CancellationToken ct)
        {
            var fuelsTask = FuelService.GetUserFuelsAsync(ct);
            var radiiTask = FuelService.GetUserRadiiAsync(ct);

            await Task.WhenAll(fuelsTask, radiiTask);

            var fuels = fuelsTask.Result.Where(i => i.IsActive).ToList();
            var radii = radiiTask.Result.Where(i => i.IsActive).ToList();

            if (!ct.IsCancellationRequested && fuels.Any() && radii.Any())
            {
                if (!_userFuels.SequenceEqual(fuels) || !_userRadii.SequenceEqual(radii))
                {
                    var fuelPriceGroups = new List<PriceItemByFuelGrouping>();
                    var range = Enumerable.Range(0, radii.Count);

                    foreach (var f in fuels)
                    {
                        var fuelPrices = range.Select(_ => new SiteFuelPriceItem()).ToList();
                        fuelPriceGroups.Add(new PriceItemByFuelGrouping(f, fuelPrices));
                    }

                    HasPrices = false;
                    FuelPriceGroups = fuelPriceGroups;

                    _userFuels = fuels;
                    _userRadii = radii;

                    return true;
                }
            }

            return false;
        }

        private async Task<bool> UpdatePricesAsync(CancellationToken ct)
        {
            if (ct.IsCancellationRequested || !FuelPriceGroups.Any())
                return false;

            var (prices, location, modifiedUtc) = await FuelService.GetFuelPricesByRadiusAsync(ct);
            var priceLookup = prices?.ToDictionary(p => p.Key.Id, p => p.Items);

            if (!ct.IsCancellationRequested)
            {
                foreach (var fpg in FuelPriceGroups)
                {
                    var priceItems = default(IList<SiteFuelPriceItem>);
                    priceLookup?.TryGetValue(fpg.Key.Id, out priceItems);
                    priceItems ??= Array.Empty<SiteFuelPriceItem>();

                    for (var i = 0; i < fpg.Items.Count; i++)
                    {
                        if (i < priceItems.Count)
                        {
                            fpg.Items[i].SetFuelPrice(priceItems[i]);
                            fpg.Items[i].Closest =
                                i == 0 && // must be the first (ordered by distance)
                                priceItems[i].LastKnowDistanceKm >= 0 && // have a valid distance
                                fpg.Count > 1; // more than one valid user radius
                            fpg.Items[i].CheapestInSa = i == priceItems.Count - 1;
                        }
                        else
                        {
                            fpg.Items[i].Clear();
                        }
                    }

                    fpg.RefreshHasPrices();
                }

                NoLocation = location is null;
                ModifiedUtc = modifiedUtc;

                return true;
            }

            return false;
        }

        private void ClearGroupsAndSetError()
        {
            HasError = true;
            HasPrices = false;
            NoPricesFound = false;
            FuelPriceGroups.ForEach(g => g.ForEach(i => i.Clear()));
        }

        private void ConnectivityChanged(object sender, ConnectivityChangedEventArgs e)
        {
            var hadInternet = HasInternet;
            HasInternet = _connectivity.NetworkAccess == NetworkAccess.Internet;

            if (!hadInternet && HasInternet)
                LoadFuelPriceGroupsCommand.ExecuteAsync(default);
        }

        private async Task SetupAppShortcutsAsync()
        {
            try
            {
                var actions = _userFuels
                    .Take(3)
                    .Select(i => new AppAction($"{nameof(UserFuel)}_{i.Id}", i.Name, icon: "fuel_shortcut"))
                    .ToArray();

                await _appActions.SetAsync(actions);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
            }
        }
    }
}