﻿using AdelaideFuel.Essentials;
using AdelaideFuel.Maui.Dispatching;
using AdelaideFuel.ViewModels;

namespace AdelaideFuel.Maui.Views
{
    public partial class PricesPage : BaseAdPage<PricesViewModel>
    {
        private readonly IPermissions _permissions;

        private bool _isFirstLoad = true;
        private IDispatcherTimer _timer;
        private CancellationTokenSource _timerCancellation;

        public PricesPage() : base()
        {
            InitializeComponent();

            _permissions = IoC.Resolve<IPermissions>();

            AdUnitId = Constants.AdMobPricesUnitId;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();

            var permissionsTask = _isFirstLoad
                ? _permissions.CheckAndRequestAsync<Permissions.LocationWhenInUse>()
                : _permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();

            _isFirstLoad = false;

            permissionsTask
                .ContinueWith(
                    t =>
                    {
                        NoLocationTryAgainButton.IsVisible = t.Result == PermissionStatus.Granted;
                        SetupAutoRefresh();
                    },
                    TaskScheduler.FromCurrentSynchronizationContext());
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();

            TearDownAutoRefresh();
        }

        private void SetupAutoRefresh()
        {
            _timerCancellation?.Cancel();
            _timerCancellation = new CancellationTokenSource();

            // safe copy
            var cts = _timerCancellation;

            // delay until navigation completes
            Task.Delay(!ViewModel.FuelPriceGroups.Any() ? 0 : 350).ContinueWith(r =>
            {
                if (cts.IsCancellationRequested)
                    return;

                ViewModel.LoadFuelPriceGroupsCommand.ExecuteAsync(default);

                _timer = Dispatcher.CreateAndStartTimer(TimeSpan.FromSeconds(60), () =>
                {
                    if (cts.IsCancellationRequested)
                    {
                        if (cts == _timerCancellation)
                            _timerCancellation = null;

                        return false;
                    }

                    if (ViewModel.IsBusy)
                        return true;

                    ViewModel.LoadFuelPriceGroupsCommand.ExecuteAsync(cts.Token);
                    return true;
                });
            }, TaskScheduler.FromCurrentSynchronizationContext());
        }

        private void TearDownAutoRefresh()
        {
            _timerCancellation?.Cancel();
            _timer?.Stop();

            _timerCancellation = null;
            _timer = null;
        }

        private void TryAgainClicked(object sender, EventArgs e)
        {
            ViewModel.LoadFuelPriceGroupsCommand.ExecuteAsync(default);
        }
    }
}