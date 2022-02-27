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

            Fuels = new ObservableRangeCollection<UserFuel>();
            FilteredSites = new ObservableRangeCollection<Grouping<UserFuel, SiteFuelPrice>>();

            LoadCommand = new AsyncCommand(LoadAsync);
            SearchCommand = new AsyncCommand<(string, int, int)>(t => SearchAsync(t.Item1, t.Item2, t.Item3));
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
                    SearchCommand.ExecuteAsync((SearchText, Fuel?.Id ?? -1, 250));
            }
        }

        private UserFuel _fuel;
        public UserFuel Fuel
        {
            get => _fuel;
            set
            {
                if (SetProperty(ref _fuel, value))
                    SearchCommand.ExecuteAsync((SearchText, Fuel?.Id ?? -1, 0));
            }
        }

        public ObservableRangeCollection<UserFuel> Fuels { get; private set; }

        public ObservableRangeCollection<Grouping<UserFuel, SiteFuelPrice>> FilteredSites { get; private set; }

        public AsyncCommand LoadCommand { get; private set; }
        public AsyncCommand<(string searchText, int fuelId, int delayMs)> SearchCommand { get; private set; }
        public AsyncCommand<SiteFuelPrice> TappedCommand { get; private set; }

        private async Task LoadAsync()
        {
            if (IsBusy)
                return;

            IsBusy = true;

            try
            {
                Fuels.Add(new UserFuel()
                {
                    Id = -1,
                    Name = Resources.All,
                    SortOrder = -1,
                    IsActive = true
                });
                Fuel = Fuels.First();

                var userFuelsTask = FuelService.GetUserFuelsAsync(default);
                var sitePricesTask = FuelService.GetSitePricesAsync(default);

                await Task.WhenAll(userFuelsTask, sitePricesTask);

                Fuels.AddRange(await userFuelsTask);

                var sitesByFuelId =
                    (from s in sitePricesTask.Result
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

        private async Task SearchAsync(string searchText, int fuelId, int delayMs)
        {
            _searchCancellation?.Cancel();
            _searchCancellation?.Dispose();
            _searchCancellation = null;

            _searchCancellation = new CancellationTokenSource();
            var ct = _searchCancellation.Token;

            if (searchText is null || !Fuels.Any(i => i.Id == fuelId))
                return;

            if (delayMs > 0)
                await Task.Delay(delayMs);

            if (IsBusy ||
                ct.IsCancellationRequested ||
                !searchText.Equals(SearchText, StringComparison.OrdinalIgnoreCase) ||
                fuelId != Fuel.Id)
                return;

            IsBusy = true;

            try
            {
                var sites = _sites
                    .Where(i => Fuel.Id < 1 || i.Key.Id == Fuel.Id)
                    .SelectMany(i => i.Value);
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