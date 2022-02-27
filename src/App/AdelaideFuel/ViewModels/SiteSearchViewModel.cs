using AdelaideFuel.Localisation;
using AdelaideFuel.Models;
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
        public readonly Dictionary<UserFuel, List<SiteFuelPrice>> _sites = new Dictionary<UserFuel, List<SiteFuelPrice>>();

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

            TrackEvent(AppCenterEvents.PageView.SiteSearchView);
        }
        #endregion

        private string _searchText;
        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value?.Trim()))
                {
                    _searchCancellation?.Cancel();
                    _searchCancellation?.Dispose();
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
            if (IsBusy)
                return;

            IsBusy = true;

            try
            {
                var sitePricesTask = FuelService.GetSitePricesAsync(default);

                var sitesByFuelId =
                    (from s in await sitePricesTask
                     orderby s.FuelSortOrder, s.PriceInCents, s.BrandSortOrder
                     group s by s.FuelId into g
                     select g);

                foreach (var g in sitesByFuelId)
                    _sites[new UserFuel()
                    {
                        Id = g.First().FuelId,
                        Name = g.First().FuelName,
                        SortOrder = g.First().FuelSortOrder,
                        IsActive = true
                    }] = g.ToList();

                FilteredSites.ReplaceRange(
                    _sites.Select(i => new Grouping<UserFuel, SiteFuelPrice>(i.Key, i.Value)));
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
                var sites = _sites.SelectMany(i => i.Value);
                var filtered = sites;

                if (!string.IsNullOrWhiteSpace(searchText))
                {
                    var compareInfo = CultureInfo.InvariantCulture.CompareInfo;
                    filtered = sites
                        .Where(i => compareInfo.IndexOf(i.SiteName, searchText, CompareOptions.IgnoreCase) >= 0); ;

                    if (!filtered.Any() && searchText.Length >= 3)
                    {
                        var parts = searchText.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        filtered = sites
                            .Where(i => parts.Any(p => compareInfo.IndexOf(i.SiteName, p, CompareOptions.IgnoreCase) >= 0 || p == i.SitePostcode) ||
                                        parts.All(p => compareInfo.IndexOf(i.SiteAddress, p, CompareOptions.IgnoreCase) >= 0));
                    }
                }

                FilteredSites.Clear();
                FilteredSites.ReplaceRange(
                    from s in filtered
                    group s by s.FuelId into g
                    select new Grouping<UserFuel, SiteFuelPrice>(
                        new UserFuel()
                        {
                            Id = g.First().FuelId,
                            Name = g.First().FuelName,
                            SortOrder = g.First().FuelSortOrder,
                            IsActive = true
                        },
                        g));
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