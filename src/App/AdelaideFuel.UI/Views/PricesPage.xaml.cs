using AdelaideFuel.ViewModels;
using System;
using System.Threading;
using Xamarin.Forms;

namespace AdelaideFuel.UI.Views
{
    public partial class PricesPage : BaseAdPage<PricesViewModel>
    {
        private CancellationTokenSource _timerCancellation;

        public PricesPage() : base()
        {
            InitializeComponent();

            AdUnitId = $"{Constants.AdMobPublisherId}/{Constants.AdMobPricesUnitId}";
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();

            _timerCancellation = new CancellationTokenSource();

            // safe copy
            var cts = _timerCancellation;

            ViewModel.LoadFuelPriceGroupsCommand.ExecuteAsync(cts.Token);

            Device.StartTimer(TimeSpan.FromSeconds(60), () =>
            {
                if (cts.IsCancellationRequested)
                {
                    if (cts == _timerCancellation)
                        _timerCancellation = null;

                    cts.Dispose();
                    return false;
                }

                ViewModel.LoadFuelPriceGroupsCommand.ExecuteAsync(cts.Token);
                return true;
            });
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();

            _timerCancellation?.Cancel();
            _timerCancellation = null;
        }
    }
}