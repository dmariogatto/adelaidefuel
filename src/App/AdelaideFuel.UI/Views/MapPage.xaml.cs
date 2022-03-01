using AdelaideFuel.Models;
using AdelaideFuel.Services;
using AdelaideFuel.UI.Attributes;
using AdelaideFuel.UI.Controls;
using AdelaideFuel.ViewModels;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.Forms;
using Xamarin.Forms.BetterMaps;

namespace AdelaideFuel.UI.Views
{
    [NavigationRoute(NavigationRoutes.Map, true)]
    [QueryProperty(nameof(SiteId), NavigationKeys.SiteIdQueryProperty)]
    [QueryProperty(nameof(FuelId), NavigationKeys.FuelIdQueryProperty)]
    public partial class MapPage : BaseAdPage<MapViewModel>
    {
        private const string BottomDrawerHandleShake = nameof(BottomDrawerHandleShake);

        private CancellationTokenSource _timerCancellation;

        public MapPage() : base()
        {
            InitializeComponent();

            AdUnitId = Constants.AdMobMapUnitId;

            UpdateMapTheme();
            ThemeManager.CurrentThemeChanged += (sender, args) => UpdateMapTheme();

            SiteMap.MoveToRegion(MapSpan.FromCenterAndRadius(
                    new Position(ViewModel.InitialCameraUpdate.Latitude, ViewModel.InitialCameraUpdate.Longitude),
                    Distance.FromMeters(ViewModel.InitialCameraUpdate.RadiusMetres)));

            ViewModel.PropertyChanged += ViewModelPropertyChanged;
        }

        private string _siteId;
        public string SiteId
        {
            get => _siteId;
            set
            {
                _siteId = value;
                UpdateFromQueryParams();
            }
        }

        private string _fuelId;
        public string FuelId
        {
            get => _fuelId;
            set
            {
                _fuelId = value;
                UpdateFromQueryParams();
            }
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();

            SetupAutoRefresh();
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();

            TearDownAutoRefresh();

            FuelId = string.Empty;
            SiteId = string.Empty;
        }

        private void SetupAutoRefresh()
        {
            _timerCancellation?.Cancel();
            _timerCancellation = new CancellationTokenSource();

            // safe copy
            var cts = _timerCancellation;

            // delay until navigation completes
            Task.Delay(!ViewModel.InitialLoadComplete ? 0 : 350).ContinueWith(r =>
            {
                if (cts.IsCancellationRequested)
                    return;

                ViewModel.LoadFuelsCommand.ExecuteAsync(int.TryParse(FuelId, out var fuelId) ? fuelId : -1);
                if (ViewModel.InitialLoadComplete)
                    ViewModel.LoadSitesCommand.ExecuteAsync(ViewModel.Fuel);

                Device.StartTimer(TimeSpan.FromSeconds(60), () =>
                {
                    if (cts.IsCancellationRequested)
                    {
                        if (cts == _timerCancellation)
                            _timerCancellation = null;

                        return false;
                    }

                    ViewModel.LoadSitesCommand.ExecuteAsync(ViewModel.Fuel);
                    return true;
                });
            }, TaskScheduler.FromCurrentSynchronizationContext());
        }

        private void TearDownAutoRefresh()
        {
            _timerCancellation?.Cancel();
            _timerCancellation = null;
        }

        private void UpdateFromQueryParams()
        {
            if (int.TryParse(FuelId, out var fuelId))
            {
                ViewModel.When(vm => !vm.IsBusy && vm.Fuels.Count > 0, () =>
                {
                    var fuel = ViewModel.Fuels.FirstOrDefault(f => f.Id == fuelId);
                    if (fuel != null)
                    {
                        ViewModel.Fuel = fuel;

                        if (int.TryParse(SiteId, out var siteId))
                        {
                            ViewModel.When(vm => !vm.IsBusy && vm.Sites.Count > 0 && vm.LoadedFuel == fuel, () =>
                            {
                                var sitePin = SiteMap.Pins
                                    .FirstOrDefault(p => p.BindingContext is Site s && s.Id == siteId);
                                if (sitePin != null)
                                {
                                    SiteMap.MoveToRegion(MapSpan.FromCenterAndRadius(sitePin.Position, Distance.FromKilometers(1)));
                                    SiteMap.SelectedPin = sitePin;
                                }
                            }, 7500);
                        }
                    }
                }, 7500);
            }
        }

        private void ViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ViewModel.InitialCameraUpdate))
            {
                SiteMap.MoveToRegion(MapSpan.FromCenterAndRadius(
                    new Position(ViewModel.InitialCameraUpdate.Latitude, ViewModel.InitialCameraUpdate.Longitude),
                    Distance.FromMeters(ViewModel.InitialCameraUpdate.RadiusMetres)));

                ViewModel.PropertyChanged -= ViewModelPropertyChanged;
            }
        }

        private bool _skipSelectedPinChange = false;
        private void SiteMapPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Map.VisibleRegion))
            {
                ViewModel.MapPosition = new Coords(
                    SiteMap.VisibleRegion.Center.Latitude,
                    SiteMap.VisibleRegion.Center.Longitude,
                    SiteMap.VisibleRegion.Radius.Meters,
                    SiteMap.VisibleRegion.Bearing);

                if (SiteMap.SelectedPin != null)
                {
                    _skipSelectedPinChange = true;
                    var pin = SiteMap.SelectedPin;
                    SiteMap.SelectedPin = null;
                    SiteMap.SelectedPin = pin;
                    _skipSelectedPinChange = false;
                }
            }
            else if (!_skipSelectedPinChange && e.PropertyName == nameof(Map.SelectedPin))
            {
                ViewModel.SelectedSite = SiteMap.SelectedPin?.BindingContext as Site;

                var handle = BottomDrawerHandle;
                if (BottomDrawerControl.ExpandedPercentage == 0 && ViewModel.SelectedSite != null)
                {
                    new Animation
                    {
                        { 0.000, 0.125, new Animation (v => handle.TranslationX = v,   0, -13) },
                        { 0.125, 0.250, new Animation (v => handle.TranslationX = v, -13,  13) },
                        { 0.250, 0.375, new Animation (v => handle.TranslationX = v,  13, -11) },
                        { 0.375, 0.500, new Animation (v => handle.TranslationX = v, -11,  11) },
                        { 0.500, 0.625, new Animation (v => handle.TranslationX = v,  11,  -7) },
                        { 0.625, 0.750, new Animation (v => handle.TranslationX = v,  -7,   7) },
                        { 0.750, 0.875, new Animation (v => handle.TranslationX = v,   7,  -5) },
                        { 0.875, 1.000, new Animation (v => handle.TranslationX = v,  -5,   0) }
                    }
                    .Commit(this, BottomDrawerHandleShake, length: 450, easing: Easing.Linear);
                }
                else
                {
                    handle.CancelAnimations();
                    handle.TranslationX = 0;
                }
            }
        }

        private void BottomDrawerControlPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(BottomDrawer.Height):
                case nameof(BottomDrawer.LockStates):
                case nameof(BottomDrawer.TranslationY):
                    var opacity = 1d;

                    var maxLockState = BottomDrawerControl.LockStates.Length > 1
                        ? BottomDrawerControl.LockStates.Last()
                        : 0.45;
                    var expandedPercentage = BottomDrawerControl.Height > 0
                        ? Math.Abs(BottomDrawerControl.TranslationY) / BottomDrawerControl.Height
                        : -1;

                    if (expandedPercentage > 0 && maxLockState > 0)
                    {
                        opacity = 1 - expandedPercentage / maxLockState;

                        if (opacity < 0) opacity = 0;
                        if (opacity > 1) opacity = 1;
                    }

                    if (opacity == 0)
                    {

                    }

                    SearchButtonLayout.Opacity = opacity;
                    SearchButtonLayout.IsEnabled = opacity > 0;
                    break;
            }
        }

        private void MainContentSizeChanged(object sender, EventArgs e)
        {
            CalculateLockStates();
        }

        private void BottomSheetContentSizeChanged(object sender, EventArgs e)
        {
            CalculateLockStates();
        }

        private void CalculateLockStates()
        {
            if (!BottomDrawerControl.IsVisible)
                return;

            var drawer = BottomDrawerControl;
            var layout = (Layout<View>)BottomDrawerControl.Content;
            var offset = double.MaxValue;

            static double getControlHeight(View v) => v.Height + v.Margin.Top + v.Margin.Bottom;

            var lockStates = new List<double>() { 0 };
            var heightAcc = drawer.Padding.Top;

            foreach (var c in layout.Children)
            {
                if (!c.IsVisible) continue;

                heightAcc += getControlHeight(c);

                if (c == BottomSheetDivider)
                {
                    offset = heightAcc;
                    var offsetMargin = MainContentLayout.Height - offset;
                    drawer.Margin = new Thickness(0, offsetMargin, 0, -offsetMargin);
                }
            }

            heightAcc += drawer.Padding.Bottom;

            if (ViewModel.SelectedSite != null)
                lockStates.Add((heightAcc - offset) / MainContentLayout.Height);

            var expIdx = drawer.LockStates.Length == lockStates.Count
                ? Array.IndexOf(drawer.LockStates, drawer.ExpandedPercentage)
                : 0;

            drawer.LockStates = lockStates.ToArray();
            drawer.ExpandedPercentage = expIdx >= 0 && expIdx < lockStates.Count
                ? lockStates[expIdx]
                : 0;
        }

        private void UpdateMapTheme()
        {
            SiteMap.MapTheme = ThemeManager.CurrentTheme switch
            {
                Theme.Light => MapTheme.Light,
                Theme.Dark => MapTheme.Dark,
                _ => MapTheme.System
            };
        }
    }
}