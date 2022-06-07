using AdelaideFuel.Models;
using AdelaideFuel.Storage;
using Plugin.InAppBilling;
using System;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.Essentials;
using Xamarin.Essentials.Interfaces;

namespace AdelaideFuel.Services
{
    public class SubscriptionService : BaseService, ISubscriptionService
    {
        private const int GraceDays = 3;

        private readonly string _productId;

        private readonly IDeviceInfo _deviceInfo;
        private readonly IConnectivity _connectivity;
        private readonly ISecureStorage _secureStorage;
        private readonly IPreferences _preferences;

        private readonly IInAppBilling _inAppBilling;
        private readonly IIapVerifyService _iapVerifyService;

        private IStore<InAppBillingProduct> _iapCache;
        private SemaphoreSlim _iapSemaphore = new SemaphoreSlim(1, 1);

        public SubscriptionService(
            IDeviceInfo deviceInfo,
            IConnectivity connectivity,
            ISecureStorage secureStorage,
            IPreferences preferences,
            IInAppBilling inAppBilling,
            IIapVerifyService iapVerifyService,
            IStoreFactory storeFactory,
            ICacheService cacheService,
            ILogger logger) : base(cacheService, logger)
        {
            _productId = Constants.SubscriptionProductId;

            _deviceInfo = deviceInfo;
            _connectivity = connectivity;
            _secureStorage = secureStorage;
            _preferences = preferences;

            _inAppBilling = inAppBilling;
            _iapVerifyService = iapVerifyService;
            _iapCache = storeFactory.GetCacheStore<InAppBillingProduct>();
        }

        public async Task<DateTime?> ExpiryDateUtcAsync()
        {
            var result = default(DateTime?);

            try
            {
                var val = _deviceInfo.DeviceType != DeviceType.Virtual
                    ? await _secureStorage.GetAsync(nameof(ExpiryDateUtcAsync)).ConfigureAwait(false)
                    : _preferences.Get(nameof(ExpiryDateUtcAsync), string.Empty);
                if (!string.IsNullOrEmpty(val) && long.TryParse(val, out var ticks) && ticks > 0)
                {
                    result = new DateTime(ticks, DateTimeKind.Utc);
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }

            return result;
        }

        public async Task ExpiryDateUtcAsync(DateTime? expiryDateUtc)
        {
            try
            {
                var val = (expiryDateUtc?.Ticks ?? 0).ToString(CultureInfo.InvariantCulture);
                if (_deviceInfo.DeviceType != DeviceType.Virtual)
                {
                    await _secureStorage.SetAsync(nameof(ExpiryDateUtcAsync), val)
                        .ConfigureAwait(false);
                }
                else
                {
                    _preferences.Set(nameof(ExpiryDateUtcAsync), val);
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
        }

        public async Task<bool> BannerAdsAsync()
        {
            var result = true;

            try
            {
                if (await IsValidAsync().ConfigureAwait(false))
                {
                    var val = _deviceInfo.DeviceType != DeviceType.Virtual
                        ? await _secureStorage.GetAsync(nameof(BannerAdsAsync)).ConfigureAwait(false)
                        : _preferences.Get(nameof(BannerAdsAsync), string.Empty);
                    if (!string.IsNullOrEmpty(val) && bool.TryParse(val, out var enabled))
                    {
                        result = enabled;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }

            return result;
        }

        public async Task BannerAdsAsync(bool enabled)
        {
            try
            {
                if (await IsValidAsync().ConfigureAwait(false))
                {
                    var val = enabled.ToString(CultureInfo.InvariantCulture);
                    if (_deviceInfo.DeviceType != DeviceType.Virtual)
                    {
                        await _secureStorage.SetAsync(nameof(BannerAdsAsync), val)
                            .ConfigureAwait(false);
                    }
                    else
                    {
                        _preferences.Set(nameof(BannerAdsAsync), val);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
        }

        public async Task<bool> UpdateSubscriptionAsync()
        {
            var updated = false;

            try
            {
                var expiryDate = await ExpiryDateUtcAsync().ConfigureAwait(false);
                if (expiryDate.HasValue &&
                    DateTime.UtcNow.Date >= expiryDate.Value.Date.AddDays(-1) &&
                    DateTime.UtcNow.Date <= expiryDate.Value.Date.AddDays(30))
                {
                    await RestoreAsync().ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }

            return updated;
        }

        public async Task<bool> IsValidAsync()
        {
            var expiryDate = await ExpiryDateUtcAsync().ConfigureAwait(false);
            return expiryDate?.AddDays(GraceDays) >= DateTime.UtcNow;
        }

        public async Task<InAppBillingProduct> GetProductAsync()
        {
            var cacheKey = $"{nameof(InAppBillingProduct)}_{_productId}";
            var product = default(InAppBillingProduct);

            product = await _iapCache.GetAsync(cacheKey, false, default).ConfigureAwait(false);

            if (product == default)
            {
                await _iapSemaphore.WaitAsync().ConfigureAwait(false);

                try
                {
                    var connected = await _inAppBilling.ConnectAsync().ConfigureAwait(false);
                    if (connected)
                    {
                        var items = await _inAppBilling.GetProductInfoAsync(ItemType.Subscription, _productId).ConfigureAwait(false);
                        product = items?.FirstOrDefault();

                        if (product != default)
                        {
                            await _iapCache.UpsertAsync(cacheKey, product, TimeSpan.FromDays(7), default).ConfigureAwait(false);
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(ex);
                    throw;
                }
                finally
                {
                    await _inAppBilling.DisconnectAsync().ConfigureAwait(false);
                    _iapSemaphore.Release();
                }
            }

            return product;
        }

        public async Task<IapValidatedReceipt> PurchaseAsync()
        {
            var validatedReceipt = default(IapValidatedReceipt);

            try
            {
                // Will throw when no purchase to restore
                validatedReceipt = await RestoreAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }

            if (validatedReceipt == default || validatedReceipt.IsExpired)
            {
                await _iapSemaphore.WaitAsync().ConfigureAwait(false);

                try
                {
                    var connected = await _inAppBilling.ConnectAsync().ConfigureAwait(false);
                    if (connected)
                    {
                        var purchase = await _inAppBilling.PurchaseAsync(_productId, ItemType.Subscription).ConfigureAwait(false);
                        if (purchase != null)
                        {
                            validatedReceipt = await _iapVerifyService.ValidateReceiptAsync(purchase).ConfigureAwait(false);
                            if (validatedReceipt != null)
                            {
                                if (_deviceInfo.Platform == DevicePlatform.Android && !purchase.IsAcknowledged)
                                    await _inAppBilling.FinalizePurchaseAsync(purchase.TransactionIdentifier).ConfigureAwait(false);
                                await ExpiryDateUtcAsync(validatedReceipt.ExpiryUtc).ConfigureAwait(false);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(ex);
                    throw;
                }
                finally
                {
                    await _inAppBilling.DisconnectAsync().ConfigureAwait(false);
                    _iapSemaphore.Release();
                }
            }

            return validatedReceipt;
        }

        public async Task<IapValidatedReceipt> RestoreAsync()
        {
            var validatedReceipt = default(IapValidatedReceipt);

            await _iapSemaphore.WaitAsync().ConfigureAwait(false);

            try
            {
                var connected = await _inAppBilling.ConnectAsync().ConfigureAwait(false);

                if (connected)
                {
                    var purchases = await _inAppBilling.GetPurchasesAsync(ItemType.Subscription).ConfigureAwait(false);
                    var purchase = purchases?.OrderByDescending(p => p.TransactionDateUtc)?.FirstOrDefault(p => p.ProductId == _productId);
                    if (purchase != null)
                    {
                        validatedReceipt = await _iapVerifyService.ValidateReceiptAsync(purchase).ConfigureAwait(false);
                        if (validatedReceipt != null)
                        {
                            if (_deviceInfo.Platform == DevicePlatform.Android && !purchase.IsAcknowledged)
                                await _inAppBilling.FinalizePurchaseAsync(purchase.TransactionIdentifier).ConfigureAwait(false);
                            await ExpiryDateUtcAsync(validatedReceipt.ExpiryUtc).ConfigureAwait(false);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
                throw;
            }
            finally
            {
                await _inAppBilling.DisconnectAsync().ConfigureAwait(false);
                _iapSemaphore.Release();
            }

            return validatedReceipt;
        }
    }
}