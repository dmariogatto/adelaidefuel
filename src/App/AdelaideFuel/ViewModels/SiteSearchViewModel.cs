using AdelaideFuel.Localisation;
using AdelaideFuel.Models;
using AdelaideFuel.Shared;
using MvvmHelpers;
using MvvmHelpers.Commands;
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
        private readonly List<SiteDto> _sites = new List<SiteDto>();
        private readonly Dictionary<UserFuel, List<SiteFuelPrice>> _sitePrices = new Dictionary<UserFuel, List<SiteFuelPrice>>();

        private CancellationTokenSource _searchCancellation;

        public SiteSearchViewModel(
            IBvmConstructor bvmConstructor) : base(bvmConstructor)
        {
            Title = Resources.Stations;

            FilteredSites = new ObservableRangeCollection<Grouping<UserFuel, SiteFuelPrice>>();

            LoadCommand = new AsyncCommand(LoadAsync);
            SearchCommand = new AsyncCommand<(string, CancellationToken)>(t => SearchAsync(t.Item1, t.Item2));
            TappedCommand = new AsyncCommand<SiteFuelPrice>(TappedAsync);
        }

        #region Overrides
        public override void OnAppearing()
        {
            base.OnAppearing();

            LoadCommand.ExecuteAsync();

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

        public ObservableRangeCollection<Grouping<UserFuel, SiteFuelPrice>> FilteredSites { get; private set; }

        public AsyncCommand LoadCommand { get; private set; }
        public AsyncCommand<(string searchText, CancellationToken ct)> SearchCommand { get; private set; }
        public AsyncCommand<SiteFuelPrice> TappedCommand { get; private set; }

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

                _sites.AddRange(sitesTask.Result);

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

                FilteredSites.ReplaceRange(
                    _sitePrices.Select(i => new Grouping<UserFuel, SiteFuelPrice>(i.Key, i.Value)));
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
                var sites = (IEnumerable<SiteDto>)_sites;
                var filtered = sites;

                if (!string.IsNullOrWhiteSpace(searchText))
                {
                    var compareInfo = CultureInfo.InvariantCulture.CompareInfo;
                    var parts = searchText.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    filtered = sites
                        .Where(i => parts.Any(p => compareInfo.IndexOf(i.Name, p, CompareOptions.OrdinalIgnoreCase) >= 0 || p == i.Postcode) ||
                                    parts.All(p => compareInfo.IndexOf(i.Address, p, CompareOptions.OrdinalIgnoreCase) >= 0));
                }

                var filteredSiteIds = new HashSet<int>(filtered.Select(i => i.SiteId));

                FilteredSites.Clear();

                foreach (var kv in _sitePrices)
                {
                    var prices = kv.Value.Where(i => filteredSiteIds.Contains(i.SiteId));
                    if (prices.Any())
                    {
                        FilteredSites.Add(new Grouping<UserFuel, SiteFuelPrice>(
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
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                await UserDialogs.AlertAsync(
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