using AdelaideFuel.Models;
using AdelaideFuel.Services;
using AdelaideFuel.UI.Attributes;
using AdelaideFuel.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Xamarin.Forms;
using Xamarin.Forms.Maps;

namespace AdelaideFuel.UI.Views
{
    [NavigationRoute(NavigationRoutes.Map)]
    [QueryProperty(nameof(SiteId), NavigationKeys.SiteIdQueryProperty)]
    [QueryProperty(nameof(FuelId), NavigationKeys.FuelIdQueryProperty)]
    public partial class MapPage : BaseAdPage<MapViewModel>
    {
        private CancellationTokenSource _timerCancellation;

        public MapPage() : base()
        {
            InitializeComponent();

            AdUnitId = $"{Constants.AdMobPublisherId}/{Constants.AdMobPricesUnitId}";

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
                UpdateSelectedSite();
            }
        }

        private string _fuelId;
        public string FuelId
        {
            get => _fuelId;
            set
            {
                _fuelId = value;
                UpdateSelectedSite();
            }
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();

            _timerCancellation = new CancellationTokenSource();

            // safe copy
            var cts = _timerCancellation;

            ViewModel.LoadFuelsCommand.ExecuteAsync();
            if (ViewModel.InitialLoadComplete)
                ViewModel.LoadSitesCommand.ExecuteAsync(ViewModel.Fuel);

            Device.StartTimer(TimeSpan.FromSeconds(60), () =>
            {
                if (cts.IsCancellationRequested)
                {
                    if (cts == _timerCancellation)
                        _timerCancellation = null;

                    cts.Dispose();
                    return false;
                }

                ViewModel.LoadSitesCommand.ExecuteAsync(ViewModel.Fuel);
                return true;
            });
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();

            SiteId = string.Empty;
            FuelId = string.Empty;

            _timerCancellation?.Cancel();
        }

        private void UpdateSelectedSite()
        {
            if (!string.IsNullOrEmpty(SiteId) &&
                !string.IsNullOrEmpty(FuelId) &&
                int.TryParse(SiteId, out var siteId) &&
                int.TryParse(FuelId, out var fuelId))
            {
                ViewModel.When(vm => !vm.IsBusy && vm.Sites.Count > 0, () =>
                {
                    var site = ViewModel.Sites.FirstOrDefault(s => s.Id == siteId);
                    var fuel = ViewModel.Fuels.FirstOrDefault(f => f.Id == fuelId);

                    if (site != null && fuel != null)
                    {
                        ViewModel.Fuel = fuel;
                        ViewModel.When(vm => !vm.IsBusy, () =>
                        {
                            var sitePin = SiteMap.Pins
                                .FirstOrDefault(p => p.BindingContext is Site s && s.Id == siteId);
                            if (sitePin != null)
                            {
                                SiteMap.MoveToRegion(MapSpan.FromCenterAndRadius(sitePin.Position, Distance.FromKilometers(1)));
                                SiteMap.SelectedPin = sitePin;
                            }
                        }, 10000);
                    }
                }, 10000);
            }
        }

        private void ViewModelPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
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
        private void SiteMapPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
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
                    .Commit(this, "BottomDrawerHandleShake", length: 450, easing: Easing.Linear);
                }
                else
                {
                    handle.CancelAnimations();
                    handle.TranslationX = 0;
                }
            }
        }

        private void BottomSheetLayoutSizeChanged(object sender, EventArgs e)
        {
            if (sender is StackLayout layout)
            {
                var drawer = BottomDrawerControl;
                var offset = double.MaxValue;

                double getControlHeight(View v) => v.Height + v.Margin.Top + v.Margin.Bottom;

                var lockStates = new List<double>() { 0 };
                var heightAcc = drawer.Padding.Top;

                foreach (var c in layout.Children)
                {
                    if (!c.IsVisible) continue;

                    heightAcc += getControlHeight(c);

                    if (c == BottomSheetDivider)
                    {
                        offset = heightAcc;
                        RelativeLayout.SetYConstraint(drawer,
                            Constraint.RelativeToParent(p => p.Height - offset));
                    }
                }

                heightAcc += drawer.Padding.Bottom;

                if (ViewModel.SelectedSite != null)
                    lockStates.Add((heightAcc - offset) / Height);

                var expIdx = drawer.LockStates.Length == lockStates.Count
                    ? Array.IndexOf(drawer.LockStates, drawer.ExpandedPercentage)
                    : 0;

                drawer.LockStates = lockStates.ToArray();
                drawer.ExpandedPercentage = expIdx >= 0 && expIdx < lockStates.Count
                    ? lockStates[expIdx]
                    : 0;
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