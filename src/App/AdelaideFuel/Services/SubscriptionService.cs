using AdelaideFuel.Models;
using AdelaideFuel.Storage;
using Plugin.InAppBilling;
using System;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.Essentials;
using Xamarin.Essentials.Interfaces;

namespace AdelaideFuel.Services
{
    public class SubscriptionService : BaseService, ISubscriptionService
    {
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

        public bool HasValidSubscription
            => !SubscriptionSuspended && IsSubscriptionValidForDate(DateTime.UtcNow);

        public DateTime? SubscriptionRestoreDateUtc
        {
            get => GetDateTimeAsync(null).Result;
            set
            {
                var utc = value.HasValue && value.Value.Kind != DateTimeKind.Utc
                    ? value.Value.ToUniversalTime()
                    : value;

                if (SubscriptionRestoreDateUtc != utc)
                {
                    SetDateTimeAsync(utc).Wait();
                }
            }
        }

        public DateTime? SubscriptionExpiryDateUtc
        {
            get => GetDateTimeAsync(null).Result;
            set
            {
                var utc = value.HasValue && value.Value.Kind != DateTimeKind.Utc
                   ? value.Value.ToUniversalTime()
                   : value;

                if (SubscriptionExpiryDateUtc != utc)
                {
                    SetDateTimeAsync(utc).Wait();
                }
            }
        }

        public bool SubscriptionSuspended
        {
            get => GetBoolAsync(false).Result.Value;
            set
            {
                if (SubscriptionSuspended != value)
                {
                    SetBoolAsync(value).Wait();
                }
            }
        }

        public bool AdsEnabled
        {
            get => !HasValidSubscription || GetBoolAsync(true).Result.Value;
            set
            {
                if (HasValidSubscription && GetBoolAsync(true).Result != value)
                {
                    SetBoolAsync(value).Wait();
                }
            }
        }

        public bool IsSubscriptionValidForDate(DateTime dateTime)
            => SubscriptionExpiryDateUtc?.AddDays(SubscriptionGraceDays) > dateTime.ToUniversalTime();

        public async Task<bool> UpdateSubscriptionAsync()
        {
            var updated = false;

            try
            {
                var expiryDate = SubscriptionExpiryDateUtc ?? DateTime.MinValue;
                var lastRestoreDate = SubscriptionRestoreDateUtc ?? DateTime.MinValue;
                var hasRestoredLast24Hours = (DateTime.UtcNow - lastRestoreDate).TotalDays < 1;

                if (expiryDate > DateTime.MinValue && !hasRestoredLast24Hours)
                {
                    var inGrace =
                        expiryDate <= DateTime.UtcNow &&
                        (DateTime.UtcNow - expiryDate).TotalDays <= SubscriptionGraceDays;

                    // Lock out restores to once every couple weeks
                    var canRestore = (DateTime.UtcNow - lastRestoreDate).TotalDays > 14;

                    // Stop trying to restore once a subscription is outside of grace period
                    // But ensure the latest restore date occurred after expiry
                    var longExpired =
                        (DateTime.UtcNow - expiryDate).TotalDays > SubscriptionGraceDays &&
                        (lastRestoreDate > expiryDate);

                    if (inGrace || (canRestore && !longExpired))
                        await RestoreAsync().ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }

            return updated;
        }

        public async Task<InAppBillingProduct> GetProductAsync()
        {
            var cacheKey = $"{nameof(InAppBillingProduct)}_{_productId}";
            var product = default(InAppBillingProduct);

            product = await _iapCache.GetAsync(cacheKey, false, default).ConfigureAwait(false);

            if (product is null)
            {
                await _iapSemaphore.WaitAsync().ConfigureAwait(false);

                var connected = await ConnectIapAsync().ConfigureAwait(false);

                try
                {
                    if (connected)
                    {
                        var items = await _inAppBilling.GetProductInfoAsync(ItemType.Subscription, _productId).ConfigureAwait(false);
                        product = items?.FirstOrDefault();

                        if (product is not null)
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
                    if (connected)
                        await DisconnectIapAsync();
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

            if (validatedReceipt is null || validatedReceipt.IsExpired)
            {
                await _iapSemaphore.WaitAsync().ConfigureAwait(false);

                var connected = await ConnectIapAsync().ConfigureAwait(false);

                try
                {
                    if (connected)
                    {
                        var purchase = await _inAppBilling.PurchaseAsync(_productId, ItemType.Subscription).ConfigureAwait(false);
                        if (purchase is not null)
                        {
                            await ValidatePurchaseAsync(purchase).ConfigureAwait(false);
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
                    if (connected)
                        await DisconnectIapAsync();
                    _iapSemaphore.Release();
                }
            }

            return validatedReceipt;
        }

        public async Task<IapValidatedReceipt> RestoreAsync()
        {
            var validatedReceipt = default(IapValidatedReceipt);

            await _iapSemaphore.WaitAsync().ConfigureAwait(false);

            var connected = await ConnectIapAsync().ConfigureAwait(false);

            try
            {
                if (connected)
                {
                    var purchases = await _inAppBilling.GetPurchasesAsync(ItemType.Subscription).ConfigureAwait(false);
                    var purchase = purchases?.OrderByDescending(p => p.TransactionDateUtc)?.FirstOrDefault(p => p.ProductId == _productId);
                    if (purchase is not null)
                    {
                        await ValidatePurchaseAsync(purchase).ConfigureAwait(false);
                    }

                    SubscriptionRestoreDateUtc = DateTime.UtcNow;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
                throw;
            }
            finally
            {
                if (connected)
                    await DisconnectIapAsync();
                _iapSemaphore.Release();
            }

            return validatedReceipt;
        }

        private int SubscriptionGraceDays
        {
            get => GetIntAsync(3).Result.Value;
            set
            {
                if (SubscriptionGraceDays != value)
                {
                    SetIntAsync(value).Wait();
                }
            }
        }

        private async Task<bool> ValidatePurchaseAsync(InAppBillingPurchase purchase)
        {
            if (purchase is not null)
            {
                var validatedReceipt = await _iapVerifyService.ValidateReceiptAsync(purchase).ConfigureAwait(false);
                if (validatedReceipt is not null)
                {
                    if (_deviceInfo.Platform == DevicePlatform.Android && !purchase.IsAcknowledged)
                        await _inAppBilling.FinalizePurchaseAsync(purchase.TransactionIdentifier).ConfigureAwait(false);

                    SetIntAsync(validatedReceipt.GraceDays, nameof(SubscriptionGraceDays)).Wait();
                    SubscriptionExpiryDateUtc = validatedReceipt.ExpiryUtc;
                    SubscriptionSuspended = validatedReceipt.IsSuspended;

                    return true;
                }
            }

            return false;
        }

        private async Task<bool> ConnectIapAsync()
        {
            var success = false;

            try
            {
                success = await _inAppBilling.ConnectAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }

            return success;
        }

        private async Task<bool> DisconnectIapAsync()
        {
            var success = false;

            try
            {
                await _inAppBilling.DisconnectAsync().ConfigureAwait(false);
                success = true;
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }

            return success;
        }

        private async Task<int?> GetIntAsync(int? defaultValue, [CallerMemberName] string key = "")
        {
            var val = await GetValueAsync(key).ConfigureAwait(false);
            if (int.TryParse(val, out var intValue))
            {
                return intValue;
            }

            return defaultValue;
        }

        private async Task SetIntAsync(int? value, [CallerMemberName] string key = "")
        {
            if (value.HasValue)
                await SetValueAsync(key, value.Value.ToString(CultureInfo.InvariantCulture)).ConfigureAwait(false);
            else
                RemoveValue(key);
        }

        private async Task<DateTime?> GetDateTimeAsync(DateTime? defaultValue, [CallerMemberName] string key = "")
        {
            var result = defaultValue;

            var val = await GetValueAsync(key).ConfigureAwait(false);
            if (long.TryParse(val, out var binary))
            {
                result = DateTime.FromBinary(binary);
            }

            return result;
        }

        private async Task SetDateTimeAsync(DateTime? value, [CallerMemberName] string key = "")
        {
            if (value.HasValue)
                await SetValueAsync(key, value.Value.ToBinary().ToString(CultureInfo.InvariantCulture)).ConfigureAwait(false);
            else
                RemoveValue(key);
        }

        private async Task<bool?> GetBoolAsync(bool? defaultValue, [CallerMemberName] string key = "")
        {
            var val = await GetValueAsync(key).ConfigureAwait(false);
            return bool.TryParse(val, out var result)
                ? result
                : defaultValue;
        }

        private async Task SetBoolAsync(bool? value, [CallerMemberName] string key = "")
        {
            if (value.HasValue)
                await SetValueAsync(key, value.Value.ToString(CultureInfo.InvariantCulture)).ConfigureAwait(false);
            else
                RemoveValue(key);
        }

        private async Task<string> GetValueAsync(string key)
        {
            if (string.IsNullOrEmpty(key))
                return string.Empty;

            return _deviceInfo.DeviceType != DeviceType.Virtual
                ? await _secureStorage.GetAsync(key).ConfigureAwait(false)
                : _preferences.Get(key, string.Empty);
        }

        private async Task SetValueAsync(string key, string value)
        {
            if (string.IsNullOrEmpty(key))
                return;

            if (_deviceInfo.DeviceType != DeviceType.Virtual)
            {
                await _secureStorage.SetAsync(key, value).ConfigureAwait(false);
            }
            else
            {
                _preferences.Set(key, value);
            }
        }

        private void RemoveValue(string key)
        {
            if (string.IsNullOrEmpty(key))
                return;

            if (_deviceInfo.DeviceType != DeviceType.Virtual)
            {
                _secureStorage.Remove(key);
            }
            else
            {
                _preferences.Remove(key);
            }
        }
    }
}