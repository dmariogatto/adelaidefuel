using AdelaideFuel.Essentials;
using AdelaideFuel.Maui.Dispatching;
using AdelaideFuel.ViewModels;

namespace AdelaideFuel.Maui.Views
{
    public partial class PricesTab : BaseTabAdView<PricesViewModel>
    {
        private readonly IPermissions _permissions;

        private bool _isFirstLoad = true;
        private IDispatcherTimer _timer;
        private CancellationTokenSource _timerCancellation;

        public PricesTab() : base()
        {
            InitializeComponent();

            _permissions = IoC.Resolve<IPermissions>();

            AdUnitId = Constants.AdMobPricesUnitId;
        }

        public override void OnAppearing()
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

        public override void OnDisappearing()
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
            var token = _timerCancellation.Token;

            // delay until navigation completes
            Dispatcher.DispatchDelayed(TimeSpan.FromMilliseconds(!ViewModel.FuelPriceGroups.Any() ? 0 : 350), () =>
            {
                if (token.IsCancellationRequested)
                    return;

                ViewModel.LoadFuelPriceGroupsCommand.ExecuteAsync(default);

                _timer = Dispatcher.CreateAndStartTimer(TimeSpan.FromSeconds(60), () =>
                {
                    if (token.IsCancellationRequested)
                    {
                        if (ReferenceEquals(cts, _timerCancellation))
                            _timerCancellation = null;

                        return false;
                    }

                    if (ViewModel.IsBusy)
                        return true;

                    ViewModel.LoadFuelPriceGroupsCommand.ExecuteAsync(token);
                    return true;
                });
            });
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