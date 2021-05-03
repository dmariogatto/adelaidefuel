using AdelaideFuel.Localisation;
using AdelaideFuel.Models;
using MvvmHelpers;
using MvvmHelpers.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AdelaideFuel.ViewModels
{
    public class PricesViewModel : BaseViewModel
    {
        private readonly int[] _radii = new[] { 1, 3, 5, 10, 25, 50, int.MaxValue };

        public PricesViewModel(
            IBvmConstructor bvmConstructor) : base(bvmConstructor)
        {
            Title = Resources.Prices;

            FuelPriceGroups = new ObservableRangeCollection<SiteFuelPriceItemGroup>();

            LoadFuelPriceGroupsCommand = new AsyncCommand<CancellationToken>(LoadFuelPriceGroupsAsync);
            FuelPriceTappedCommand = new AsyncCommand<SiteFuelPriceItem>((fp) => fp?.SiteId != null
                ? NavigationService.NavigateToAsync<MapViewModel>(new Dictionary<string, string>()
                {
                    { NavigationKeys.SiteIdQueryProperty, fp.SiteId.ToString() },
                    { NavigationKeys.FuelIdQueryProperty, fp.FuelId.ToString() }
                })
                : Task.CompletedTask);
            GoToMapCommand = new AsyncCommand(() => NavigationService.NavigateToAsync<MapViewModel>());
            GoToSettingsCommand = new AsyncCommand(() => NavigationService.NavigateToAsync<SettingsViewModel>());
        }

        #region Overrides
        public override void OnAppearing()
        {
            base.OnAppearing();

            TrackEvent(AppCenterEvents.PageView.HomeView);
        }
        #endregion

        private DateTime _lastUpdatedUtc = DateTime.MinValue;
        public DateTime LastUpdatedUtc
        {
            get => _lastUpdatedUtc;
            set => SetProperty(ref _lastUpdatedUtc, value);
        }

        private DateTime _lastLoadedUtc = DateTime.MinValue;
        public DateTime LastLoadedUtc
        {
            get => _lastLoadedUtc;
            set => SetProperty(ref _lastLoadedUtc, value);
        }

        public bool RefreshRequired => LastLoadedUtc < DateTime.UtcNow.AddMinutes(-5);
        public bool HasPrices => FuelPriceGroups.Any(g => g.Items.Any(i => !i.IsClear));

        public ObservableRangeCollection<SiteFuelPriceItemGroup> FuelPriceGroups { get; private set; }

        public AsyncCommand<CancellationToken> LoadFuelPriceGroupsCommand { get; private set; }
        public AsyncCommand<SiteFuelPriceItem> FuelPriceTappedCommand { get; private set; }
        public AsyncCommand GoToMapCommand { get; private set; }
        public AsyncCommand GoToSettingsCommand { get; private set; }

        private async Task LoadFuelPriceGroupsAsync(CancellationToken ct)
        {
            var refreshRequired = RefreshRequired;

            if (FuelPriceGroups.Any())
            {
                // has user fuels changed
                try { refreshRequired |= await LoadGroupsAsync(ct); }
                catch (Exception ex) { Logger.Error(ex); }
            }

            if (IsBusy || !refreshRequired)
                return;

            IsBusy = true;

            try
            {
                if (!FuelPriceGroups.Any())
                    await LoadGroupsAsync(ct);
                await UpdatePricesAsync(ct);
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

            OnPropertyChanged(nameof(HasPrices));
        }

        private async Task<bool> LoadGroupsAsync(CancellationToken ct)
        {
            var fuels = (await FuelService.GetUserFuelsAsync(ct))
                    ?.Where(i => i.IsActive)?.ToList();

            if (!ct.IsCancellationRequested && fuels?.Any() == true)
            {
                if (!FuelPriceGroups.Select(g => g.Key).SequenceEqual(fuels))
                {
                    var fuelPriceGroups = new List<SiteFuelPriceItemGroup>();

                    foreach (var f in fuels)
                    {
                        var fuelPrices = new SiteFuelPriceItem[_radii.Length];
                        for (var i = 0; i < _radii.Length; i++)
                            fuelPrices[i] = new SiteFuelPriceItem();
                        fuelPriceGroups.Add(new SiteFuelPriceItemGroup(f, fuelPrices));
                    }

                    FuelPriceGroups.Clear();
                    FuelPriceGroups.AddRange(fuelPriceGroups);
                    OnPropertyChanged(nameof(HasPrices));

                    return true;
                }
            }

            return false;
        }

        private async Task<bool> UpdatePricesAsync(CancellationToken ct)
        {
            if (ct.IsCancellationRequested || !FuelPriceGroups.Any())
                return false;

            var priceLookup = (await FuelService.GetFuelPricesByRadiusAsync(_radii, ct))
                        ?.ToDictionary(p => p.Key.Id, p => p.Items);

            if (!ct.IsCancellationRequested && priceLookup?.Any() == true)
            {
                foreach (var fpg in FuelPriceGroups)
                {
                    priceLookup.TryGetValue(fpg.Key.Id, out var prices);
                    prices ??= Array.Empty<SiteFuelPriceItem>();

                    var groupItems = fpg.Items.ToList();
                    for (var i = 0; i < groupItems.Count; i++)
                    {
                        if (i < prices.Count)
                        {
                            fpg.Items[i].SetFuelPrice(prices[i]);
                            fpg.Items[i].Closest = i == 0 && prices[i].LastKnowDistanceKm >= 0;
                            fpg.Items[i].CheapestInSa = i == prices.Count - 1;

                            if (prices[i].ModifiedUtc > LastUpdatedUtc)
                                LastUpdatedUtc = prices[i].ModifiedUtc;
                        }
                        else
                        {
                            fpg.Items[i].Clear();
                        }
                    }
                }

                LastLoadedUtc = DateTime.UtcNow;

                return true;
            }

            return false;
        }

        private void ClearGroupsAndSetError()
        {
            HasError = true;
            FuelPriceGroups.ForEach(g => g.ForEach(i => i.Clear()));
            OnPropertyChanged(nameof(HasPrices));
        }
    }
}