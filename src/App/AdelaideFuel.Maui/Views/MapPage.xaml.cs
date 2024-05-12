using AdelaideFuel.Maui.Controls;
using AdelaideFuel.Maui.Converters;
using AdelaideFuel.Maui.Extensions;
using AdelaideFuel.Models;
using AdelaideFuel.Services;
using AdelaideFuel.ViewModels;
using BetterMaps.Maui;
using System.ComponentModel;
using Map = BetterMaps.Maui.Map;

namespace AdelaideFuel.Maui.Views
{
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

            if (DeviceInfo.Current.Idiom == DeviceIdiom.Tablet)
            {
                BottomDrawerControl.HorizontalOptions = LayoutOptions.Center;
                BottomDrawerControl.SetBinding(Page.WidthRequestProperty, new Binding(
                    nameof(Width),
                    converter: App.Current.FindResource<IValueConverter>(nameof(MultiplyByConverter)),
                    converterParameter: 0.7d,
                    source: this));
            }

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

            BottomDrawerControl.FadeTo(1);
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();

            TearDownAutoRefresh();

            FuelId = string.Empty;
            SiteId = string.Empty;

            BottomDrawerControl.FadeTo(0);
        }

        protected override void OnSizeAllocated(double width, double height)
        {
            base.OnSizeAllocated(width, height);

#if IOS || MACCATALYST
            var insets = WindowStateManager.Default.GetCurrentUIWindow().SafeAreaInsets;
            SiteMap.Margin = new Thickness(0, -insets.Top, 0, 0);
#endif
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

                Dispatcher.StartTimer(TimeSpan.FromSeconds(60), () =>
                {
                    if (cts.IsCancellationRequested)
                    {
                        if (cts == _timerCancellation)
                            _timerCancellation = null;

                        return false;
                    }

                    if (ViewModel.IsBusy)
                        return true;

                    ViewModel.LoadSitesCommand.ExecuteAsync(ViewModel.Fuel);
                    return true;
                });

                if (IoC.Resolve<IAppPreferences>().ShowRadiiOnMap)
                    _ = DrawRadiiAsync(cts.Token);
                else if (SiteMap.MapElements.Any())
                    SiteMap.MapElements.Clear();
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
                    if (fuel is not null)
                    {
                        ViewModel.Fuel = fuel;

                        if (int.TryParse(SiteId, out var siteId))
                        {
                            ViewModel.When(vm => !vm.IsBusy && vm.Sites.Count > 0 && vm.LoadedFuel == fuel, () =>
                            {
                                var sitePin = SiteMap.Pins
                                    .FirstOrDefault(p => p.BindingContext is Site s && s.Id == siteId);
                                if (sitePin is not null)
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

                if (SiteMap.SelectedPin is not null)
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
                if (BottomDrawerControl.ExpandedPercentage == 0 && ViewModel.SelectedSite is not null)
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
                    if (DeviceInfo.Current.Idiom == DeviceIdiom.Phone)
                    {
                        var opacity = 1d;

                        var lockState = BottomDrawerControl.LockStates.Length > 1
                            ? BottomDrawerControl.LockStates.Last()
                            : 0.45;
                        var expandedPercentage = BottomDrawerControl.Height > 0
                            ? Math.Abs(BottomDrawerControl.TranslationY) / BottomDrawerControl.Height
                            : -1;

                        if (expandedPercentage > 0 && lockState > 0)
                        {
                            opacity = 1 - expandedPercentage / lockState;

                            if (opacity < 0) opacity = 0;
                            if (opacity > 1) opacity = 1;
                        }

                        SearchButtonLayout.Opacity = opacity;
                        SearchButtonLayout.InputTransparent = opacity == 0;
                    }
                    break;
            }
        }

        private void MainGridSizeChanged(object sender, EventArgs e)
        {
            DispatchCalculateLockStates();
        }

        private void BottomSheetSizeChanged(object sender, EventArgs e)
        {
            DispatchCalculateLockStates();
        }

        private void DispatchCalculateLockStates()
        {
            var frameTime = 1 / Math.Max(60f, DeviceDisplay.MainDisplayInfo.RefreshRate);
            Dispatcher.DispatchDelayed(
                TimeSpan.FromSeconds(frameTime),
                () => CalculateLockStates(BottomDrawerControl));
        }

        private void CalculateLockStates(BottomDrawer drawer)
        {
            static bool isViewHeightValid(View v) => !double.IsNaN(v.Height) && v.Height >= 0;
            static double getViewHeight(View v) => v.Height + v.Margin.Top + v.Margin.Bottom;

            if (!drawer.IsVisible)
            {
                drawer.Margin = new Thickness(0, MainGrid.Height, 0, -MainGrid.Height);
                return;
            }

            var layout = (IList<IView>)drawer.Content;
            var views = layout
                .OfType<View>()
                .Where(i => i.IsVisible && isViewHeightValid(i));

            var offset = double.MaxValue;
            var lockStates = new List<double>() { 0 };
            var heightAcc = drawer.Padding.Top;

            foreach (var c in views)
            {
                heightAcc += getViewHeight(c);

                if (c == BottomSheetDivider)
                {
                    offset = heightAcc;
                    var offsetMargin = MainGrid.Height - offset;
                    drawer.Margin = new Thickness(0, offsetMargin, 0, -offsetMargin);
                }
            }

            heightAcc += drawer.Padding.Bottom;

            if (ViewModel.SelectedSite is not null)
                lockStates.Add((heightAcc - offset) / MainGrid.Height);

            var expIdx = drawer.LockStates.Length == lockStates.Count
                ? Array.IndexOf(drawer.LockStates, drawer.ExpandedPercentage)
                : 0;

            drawer.LockStates = [.. lockStates];
            drawer.ExpandedPercentage = expIdx >= 0 && expIdx < lockStates.Count
                ? lockStates[expIdx]
                : 0;
        }

        private async Task DrawRadiiAsync(CancellationToken ct)
        {
            try
            {
                var locationTask = IoC.Resolve<IGeolocation>().GetLastKnownLocationAsync();
                var radiiTask = IoC.Resolve<IFuelService>().GetUserRadiiAsync(ct);

                await Task.WhenAll(locationTask, radiiTask);

                if (!ct.IsCancellationRequested)
                {
                    var location = locationTask.Result;

                    SiteMap.MapElements.Clear();

                    if (location is not null)
                    {
                        var radii =
                            (from i in radiiTask.Result
                             where i.Id < int.MaxValue
                             select i.Id);

                        foreach (var i in radii)
                        {
                            var circle = new Circle()
                            {
                                Center = new Position(location.Latitude, location.Longitude),
                                Radius = Distance.FromKilometers(i),
                                StrokeColor = ThemeManager.ResolvedTheme switch
                                {
                                    Theme.Dark => Colors.LightSkyBlue,
                                    _ => Colors.DeepSkyBlue
                                },
                                StrokeWidth = 8,
                            };

                            SiteMap.MapElements.Add(circle);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                SiteMap.MapElements.Clear();
                System.Diagnostics.Debug.WriteLine(ex);
            }
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