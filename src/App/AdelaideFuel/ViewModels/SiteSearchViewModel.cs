using AdelaideFuel.Localisation;
using AdelaideFuel.Models;
using AdelaideFuel.Shared;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AdelaideFuel.ViewModels
{
    public class SiteSearchViewModel : BaseViewModel
    {
        private readonly Dictionary<UserFuel, IReadOnlyList<SiteFuelPrice>> _sitePrices = new Dictionary<UserFuel, IReadOnlyList<SiteFuelPrice>>();

        private IReadOnlyList<SiteDto> _sites = [];

        private CancellationTokenSource _searchCancellation;

        public SiteSearchViewModel(
            IBvmConstructor bvmConstructor) : base(bvmConstructor)
        {
            Title = Resources.Stations;

            LoadCommand = new AsyncRelayCommand(LoadAsync);
            SearchCommand = new AsyncRelayCommand<(string, CancellationToken)>(t => SearchAsync(t.Item1, t.Item2));
            TappedCommand = new AsyncRelayCommand<SiteFuelPrice>(TappedAsync);
        }

        #region Overrides
        public override void OnAppearing()
        {
            base.OnAppearing();

            LoadCommand.Execute(null);

            TrackEvent(Events.PageView.SiteSearchView);
        }
        #endregion

        private bool _initialLoadComplete;
        public bool InitialLoadComplete
        {
            get => _initialLoadComplete;
            set => SetProperty(ref _initialLoadComplete, value);
        }

        private string _searchText;
        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value?.Trim()))
                {
                    _searchCancellation?.Cancel();
                    _searchCancellation = null;

                    _searchCancellation = new CancellationTokenSource();
                    SearchCommand.ExecuteAsync((_searchText, _searchCancellation.Token));
                }
            }
        }

        private IReadOnlyList<PriceByFuelGrouping> _filteredSites;
        public IReadOnlyList<PriceByFuelGrouping> FilteredSites
        {
            get => _filteredSites;
            private set => SetProperty(ref _filteredSites, value);
        }

        public AsyncRelayCommand LoadCommand { get; private set; }
        public AsyncRelayCommand<(string searchText, CancellationToken ct)> SearchCommand { get; private set; }
        public AsyncRelayCommand<SiteFuelPrice> TappedCommand { get; private set; }

        private async Task LoadAsync()
        {
            if (IsBusy || _sites.Any())
                return;

            IsBusy = true;

            try
            {
                var delayTask = Task.Delay(250);

                var sitesTask = FuelService.GetSitesAsync(default);
                var sitePricesTask = FuelService.GetSitePricesAsync(default);

                await Task.WhenAll(sitesTask, sitePricesTask);

                _sites = sitesTask.Result;

                var sitesByFuelId =
                    (from s in sitePricesTask.Result.prices
                     orderby s.FuelSortOrder, s.PriceInCents, s.BrandSortOrder
                     group s by s.FuelId into g
                     select g);

                foreach (var g in sitesByFuelId)
                    _sitePrices[new UserFuel()
                    {
                        Id = g.First().FuelId,
                        Name = g.First().FuelName,
                        SortOrder = g.First().FuelSortOrder,
                        IsActive = true
                    }] = g.ToList();

                await delayTask;

                FilteredSites = _sitePrices.Select(i => new PriceByFuelGrouping(i.Key, i.Value)).ToList();
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
            finally
            {
                InitialLoadComplete = true;
                IsBusy = false;
            }
        }

        private async Task SearchAsync(string searchText, CancellationToken ct)
        {
            if (ct.IsCancellationRequested ||
                searchText is null)
                return;

            await Task.Delay(250);

            if (IsBusy ||
                ct.IsCancellationRequested ||
                !searchText.Equals(SearchText, StringComparison.OrdinalIgnoreCase))
                return;

            IsBusy = true;

            try
            {
                var filtered = _sites.AsEnumerable();

                if (!string.IsNullOrWhiteSpace(searchText))
                {
                    var compareInfo = CultureInfo.InvariantCulture.CompareInfo;
                    var parts = searchText.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    filtered = filtered
                        .Where(i => parts.Any(p => compareInfo.IndexOf(i.Name, p, CompareOptions.OrdinalIgnoreCase) >= 0 || p == i.Postcode) ||
                                    parts.All(p => compareInfo.IndexOf(i.Address, p, CompareOptions.OrdinalIgnoreCase) >= 0));
                }

                var filteredSiteIds = new HashSet<int>(filtered.Select(i => i.SiteId));
                var filteredSites = new List<PriceByFuelGrouping>();

                foreach (var kv in _sitePrices)
                {
                    var prices = kv.Value.Where(i => filteredSiteIds.Contains(i.SiteId));
                    if (prices.Any())
                    {
                        filteredSites.Add(new PriceByFuelGrouping(
                            new UserFuel()
                            {
                                Id = prices.First().FuelId,
                                Name = prices.First().FuelName,
                                SortOrder = prices.First().FuelSortOrder,
                                IsActive = true
                            },
                            prices));
                    }
                }

                FilteredSites = filteredSites;
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                await DialogService.AlertAsync(
                    Resources.UnableToLoadStations,
                    Resources.Error,
                    Resources.OK);
            }
            finally
            {
                IsBusy = false;
            }
        }


        private async Task TappedAsync(SiteFuelPrice site)
        {
            if (site is null)
                return;

            await NavigationService.PopAsync();
            await NavigationService.NavigateToAsync<MapViewModel>(new Dictionary<string, string>()
            {
                { NavigationKeys.SiteIdQueryProperty, site.SiteId.ToString() },
                { NavigationKeys.FuelIdQueryProperty, site.FuelId.ToString() }
            });
        }
    }
}