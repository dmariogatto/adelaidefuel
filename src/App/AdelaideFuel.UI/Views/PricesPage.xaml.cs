using AdelaideFuel.UI.Attributes;
using AdelaideFuel.ViewModels;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.Essentials;
using Xamarin.Essentials.Interfaces;
using Xamarin.Forms;

namespace AdelaideFuel.UI.Views
{
    [NavigationRoute(NavigationRoutes.Prices, true)]
    public partial class PricesPage : BaseAdPage<PricesViewModel>
    {
        private readonly IPermissions _permissions;

        private bool _isFirstLoad = true;
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
                    r => SetupAutoRefresh(),
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

                Device.StartTimer(TimeSpan.FromSeconds(60), () =>
                {
                    if (cts.IsCancellationRequested)
                    {
                        if (cts == _timerCancellation)
                            _timerCancellation = null;

                        return false;
                    }

                    ViewModel.LoadFuelPriceGroupsCommand.ExecuteAsync(cts.Token);
                    return true;
                });
            }, TaskScheduler.FromCurrentSynchronizationContext());
        }

        private void TearDownAutoRefresh()
        {
            _timerCancellation?.Cancel();
            _timerCancellation = null;
        }
    }
}