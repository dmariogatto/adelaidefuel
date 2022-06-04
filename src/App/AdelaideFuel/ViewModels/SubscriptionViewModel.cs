﻿using AdelaideFuel.Localisation;
using AdelaideFuel.Services;
using MvvmHelpers.Commands;
using Plugin.InAppBilling;
using System;
using System.Threading.Tasks;
using Xamarin.Essentials;
using Xamarin.Essentials.Interfaces;

namespace AdelaideFuel.ViewModels
{
    public class SubscriptionViewModel : BaseViewModel
    {
        private readonly IAppInfo _appInfo;
        private readonly IDeviceInfo _deviceInfo;
        private readonly ILauncher _launcher;

        private readonly ISubscriptionService _subscriptionService;

        public SubscriptionViewModel(
            IAppInfo appInfo,
            IDeviceInfo deviceInfo,
            ILauncher launcher,
            ISubscriptionService subscriptionService,
            IBvmConstructor bvmConstructor) : base(bvmConstructor)
        {
            Title = Resources.ShouldIFuel;

            _appInfo = appInfo;
            _deviceInfo = deviceInfo;
            _launcher = launcher;

            _subscriptionService = subscriptionService;

            LoadProductCommand = new AsyncCommand(LoadProductAsync);
            LoadProductCommand.ExecuteAsync();

            PurchaseCommand = new AsyncCommand(PurchaseAsync);
            RestorePurchasesCommand = new AsyncCommand(RestorePurchasesAsync);

            ManageSubscriptionsCommand = new AsyncCommand(ManageSubscriptionsAsync);
        }

        private InAppBillingProduct _subscriptionProduct;
        public InAppBillingProduct SubscriptionProduct
        {
            get => _subscriptionProduct;
            set => SetProperty(ref _subscriptionProduct, value);
        }

        public DateTime? ExpiryDate => _subscriptionService.ExpiryDateUtcAsync().Result;
        public bool HasValidSubscription => _subscriptionService.IsValidAsync().Result;

        public bool BannerAds
        {
            get => _subscriptionService.BannerAdsAsync().Result;
            set
            {
                _subscriptionService.BannerAdsAsync(value).Wait();
                OnPropertyChanged(nameof(BannerAds));
            }
        }

        public AsyncCommand LoadProductCommand { get; private set; }
        public AsyncCommand PurchaseCommand { get; private set; }
        public AsyncCommand RestorePurchasesCommand { get; private set; }
        public AsyncCommand ManageSubscriptionsCommand { get; private set; }

        private async Task LoadProductAsync()
        {
            if (IsBusy || SubscriptionProduct is not null)
                return;

            IsBusy = true;

            try
            {
                SubscriptionProduct = await _subscriptionService.GetProductAsync();
            }
            catch (Exception ex)
            {
                SubscriptionProduct = null;

                Logger.Error(ex);
                await UserDialogs.AlertAsync(ex.Message, Resources.Error, Resources.OK);
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task PurchaseAsync()
        {
            if (IsBusy)
                return;

            IsBusy = true;

            try
            {
                await _subscriptionService.PurchaseAsync();
                OnPropertyChanged(nameof(ExpiryDate));
                OnPropertyChanged(nameof(HasValidSubscription));
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                await UserDialogs.AlertAsync(ex.Message, Resources.Error, Resources.OK);
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task RestorePurchasesAsync()
        {
            if (IsBusy)
                return;

            IsBusy = true;

            try
            {
                await _subscriptionService.RestoreAsync();
                OnPropertyChanged(nameof(ExpiryDate));
                OnPropertyChanged(nameof(HasValidSubscription));
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                await UserDialogs.AlertAsync(ex.Message, Resources.Error, Resources.OK);
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task ManageSubscriptionsAsync()
        {
            if (IsBusy)
                return;

            const string appleUrl = "itms-apps://apps.apple.com/account/subscriptions";
            const string droidUrlFmt = "https://play.google.com/store/account/subscriptions?sku={0}&package={1}";

            var url = _deviceInfo.Platform == DevicePlatform.iOS
                ? appleUrl
                : string.Format(droidUrlFmt, Constants.SubscriptionProductId, _appInfo.PackageName);
            await _launcher.TryOpenAsync(url);
        }
    }
}