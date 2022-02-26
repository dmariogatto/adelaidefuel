using AdelaideFuel.Localisation;
using AdelaideFuel.Shared;
using MvvmHelpers;
using MvvmHelpers.Commands;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.Essentials;
using Xamarin.Essentials.Interfaces;

namespace AdelaideFuel.ViewModels
{
    public class SiteSearchViewModel : BaseViewModel
    {
        private readonly IGeolocation _geolocation;
        private readonly IPermissions _permissions;

        private CancellationTokenSource _searchCancellation;

        public SiteSearchViewModel(
            IGeolocation geolocation,
            IPermissions permissions,
            IBvmConstructor bvmConstructor) : base(bvmConstructor)
        {
            Title = Resources.Stations;

            _geolocation = geolocation;
            _permissions = permissions;

            Sites = new ObservableRangeCollection<SiteDto>();
            FilteredSites = new ObservableRangeCollection<SiteDto>();

            LoadCommand = new AsyncCommand(LoadAsync);
            SearchCommand = new AsyncCommand<(string, CancellationToken)>(t => SearchAsync(t.Item1, t.Item2));
            TappedCommand = new AsyncCommand<SiteDto>(TappedAsync);
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
                SetProperty(ref _searchText, value?.Trim());

                _searchCancellation?.Cancel();
                _searchCancellation?.Dispose();
                _searchCancellation = null;

                _searchCancellation = new CancellationTokenSource();
                SearchCommand.ExecuteAsync((_searchText, _searchCancellation.Token));
            }
        }

        public ObservableRangeCollection<SiteDto> Sites { get; private set; }
        public ObservableRangeCollection<SiteDto> FilteredSites { get; private set; }

        public AsyncCommand LoadCommand { get; private set; }
        public AsyncCommand<(string searchText, CancellationToken ct)> SearchCommand { get; private set; }
        public AsyncCommand<SiteDto> TappedCommand { get; private set; }

        private async Task LoadAsync()
        {
            if (IsBusy)
                return;

            IsBusy = true;

            try
            {
                var locationTask = GetLastLocationAsync();
                var sitesTask = FuelService.GetSitesAsync(default);
                var userBrandsTask = FuelService.GetUserBrandsAsync(default);

                await Task.WhenAll(locationTask, sitesTask, userBrandsTask);

                var sites =
                    (from s in sitesTask.Result
                     join ub in userBrandsTask.Result on s.BrandId equals ub.Id
                     let distanceKm = locationTask.Result is not null
                        ? locationTask.Result.CalculateDistance(s.Latitude, s.Longitude, DistanceUnits.Kilometers)
                        : double.MaxValue
                     orderby ub.IsActive descending, distanceKm, ub.SortOrder, s.Name
                     select s).ToList();

                Sites.ReplaceRange(sites);
                FilteredSites.ReplaceRange(sites);
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
                var filtered = (IList<SiteDto>)Sites;

                if (!string.IsNullOrWhiteSpace(searchText))
                {
                    var compareInfo = CultureInfo.InvariantCulture.CompareInfo;
                    filtered = Sites
                        .Where(i => compareInfo.IndexOf(i.Name, searchText, CompareOptions.IgnoreCase) >= 0)
                        .ToList();

                    if (!filtered.Any() && searchText.Length >= 3)
                    {
                        var parts = searchText.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        filtered = Sites
                            .Where(i => parts.Any(p => compareInfo.IndexOf(i.Name, p, CompareOptions.IgnoreCase) >= 0) ||
                                        parts.All(p => compareInfo.IndexOf(i.Address, p, CompareOptions.IgnoreCase) >= 0)).ToList();
                    }
                }

                FilteredSites.Clear();
                FilteredSites.ReplaceRange(filtered);
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

        private async Task TappedAsync(SiteDto site)
        {
            if (site is null)
                return;

            await NavigationService.PopAsync();
            await NavigationService.NavigateToAsync<MapViewModel>(new Dictionary<string, string>()
            {
                { NavigationKeys.SiteIdQueryProperty, site.SiteId.ToString() },
                { NavigationKeys.LatLongQueryProperty, $"{site.Latitude},{site.Longitude}" }
            });
        }

        private async Task<Location> GetLastLocationAsync()
        {
            var location = default(Location);

            try
            {
                var status = await _permissions.CheckStatusAsync<Permissions.LocationWhenInUse>()
                    .ConfigureAwait(false);
                if (status == PermissionStatus.Granted)
                {
                    location = await _geolocation.GetLastKnownLocationAsync().ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
            }

            return location;
        }
    }
}