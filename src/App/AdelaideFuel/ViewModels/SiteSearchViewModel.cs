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

namespace AdelaideFuel.ViewModels
{
    public class SiteSearchViewModel : BaseViewModel
    {
        private readonly SemaphoreSlim _searchSemaphore = new SemaphoreSlim(1, 1);

        private CancellationTokenSource _searchCancellation;

        public SiteSearchViewModel(
            IBvmConstructor bvmConstructor) : base(bvmConstructor)
        {
            Title = Resources.Stations;

            Sites = new ObservableRangeCollection<SiteDto>();
            FilteredSites = new ObservableRangeCollection<SiteDto>();

            LoadCommand = new AsyncCommand(LoadAsync);
            SearchCommand = new AsyncCommand<CancellationToken>(ct => SearchAsync(SearchText, ct));
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
                SearchCommand.ExecuteAsync(_searchCancellation.Token);
            }
        }

        public ObservableRangeCollection<SiteDto> Sites { get; private set; }
        public ObservableRangeCollection<SiteDto> FilteredSites { get; private set; }

        public AsyncCommand LoadCommand { get; private set; }
        public AsyncCommand<CancellationToken> SearchCommand { get; private set; }
        public AsyncCommand<SiteDto> TappedCommand { get; private set; }

        private async Task LoadAsync()
        {
            if (IsBusy)
                return;

            IsBusy = true;

            try
            {
                var sitesTask = FuelService.GetSitesAsync(default);
                var userBrandsTask = FuelService.GetUserBrandsAsync(default);

                await Task.WhenAll(sitesTask, userBrandsTask);

                var sites =
                    (from s in sitesTask.Result
                     join ub in userBrandsTask.Result on s.BrandId equals ub.Id
                     orderby ub.IsActive descending, ub.SortOrder, s.Name
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

        private async Task SearchAsync(string searchText, CancellationToken cancellationToken)
        {
            // Not great but stops a race condition when the ObserableCollection changes too quickly
            await Task.Delay(250);
            if (cancellationToken.IsCancellationRequested ||
                searchText == null ||
                !searchText.Equals(SearchText, StringComparison.OrdinalIgnoreCase))
                return;

            await _searchSemaphore.WaitAsync();

            if (!IsBusy && !cancellationToken.IsCancellationRequested)
            {
                IsBusy = true;

                try
                {
                    var filtered = (IList<SiteDto>)Sites;

                    if (!cancellationToken.IsCancellationRequested)
                    {
                        if (!string.IsNullOrWhiteSpace(searchText))
                        {
                            var compareInfo = CultureInfo.InvariantCulture.CompareInfo;
                            filtered = Sites
                                .Where(i => compareInfo.IndexOf(i.Name, searchText, CompareOptions.IgnoreCase) >= 0)
                                .ToList();

                            if (!filtered.Any() &&
                                searchText.Length >= 3)
                            {
                                var parts = searchText.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                                filtered = Sites
                                    .Where(i => parts.Any(p => compareInfo.IndexOf(i.Name, p, CompareOptions.IgnoreCase) >= 0) ||
                                                parts.All(p => compareInfo.IndexOf(i.Address, p, CompareOptions.IgnoreCase) >= 0)).ToList();
                            }
                        }

                        FilteredSites.Clear();
                        FilteredSites.ReplaceRange(filtered.ToList());
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

            _searchSemaphore.Release();
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
    }
}