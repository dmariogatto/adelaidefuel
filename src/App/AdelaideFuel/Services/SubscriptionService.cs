﻿using AdelaideFuel.Models;
using AdelaideFuel.Storage;
using Microsoft.Maui.Devices;
using Microsoft.Maui.Networking;
using Microsoft.Maui.Storage;
using Plugin.InAppBilling;
using System;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace AdelaideFuel.Services
{
    public class SubscriptionService : BaseService, ISubscriptionService
    {
        private readonly string _productId;

        private readonly IAppClock _clock;

        private readonly IDeviceInfo _deviceInfo;
        private readonly IConnectivity _connectivity;
        private readonly ISecureStorage _secureStorage;
        private readonly IPreferences _preferences;

        private readonly IInAppBilling _inAppBilling;
        private readonly IIapVerifyService _iapVerifyService;

        private ICacheStore<InAppBillingProduct> _iapCache;
        private SemaphoreSlim _iapSemaphore = new SemaphoreSlim(1, 1);

        public SubscriptionService(
            IAppClock clock,
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

            _clock = clock;

            _deviceInfo = deviceInfo;
            _connectivity = connectivity;
            _secureStorage = secureStorage;
            _preferences = preferences;

            _inAppBilling = inAppBilling;
            _iapVerifyService = iapVerifyService;
            _iapCache = storeFactory.GetCacheStore<InAppBillingProduct>();
        }

        public bool HasValidSubscription
            => !SubscriptionSuspended && IsSubscriptionValidForDate(_clock.UtcNow);

        public DateTime? SubscriptionRestoreDateUtc
        {
            get => GetDateTimeAsync(null).Result;
            set
            {
                var utc = value.HasValue && value.Value.Kind != DateTimeKind.Utc
                    ? _clock.ToUniversal(value.Value)
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
                   ? _clock.ToUniversal(value.Value)
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
            => SubscriptionExpiryDateUtc?.AddDays(SubscriptionGraceDays) > _clock.ToUniversal(dateTime);

        public async Task<bool> UpdateSubscriptionAsync()
        {
            var updated = false;

            try
            {
                var utcNow = _clock.UtcNow;
                var expiryDate = SubscriptionExpiryDateUtc ?? DateTime.MinValue;
                var lastRestoreDate = SubscriptionRestoreDateUtc ?? DateTime.MinValue;
                var hasRestoredToday = _clock.ToLocal(lastRestoreDate).Date == _clock.Today;

                if (expiryDate > DateTime.MinValue && !hasRestoredToday)
                {
                    // Lock out restores to once every couple weeks, or
                    // when subscription has expired
                    var canRestore =
                        (utcNow - lastRestoreDate).TotalDays > 14 ||
                        !HasValidSubscription;

                    // Stop trying to restore once expiry and last restore are outside of grace period
                    var longExpired =
                        utcNow >= expiryDate &&
                        (utcNow - expiryDate).TotalDays > SubscriptionGraceDays &&
                        lastRestoreDate > expiryDate &&
                        (lastRestoreDate - expiryDate).TotalDays > SubscriptionGraceDays;

                    if (canRestore && !longExpired)
                        updated = (await RestoreAsync().ConfigureAwait(false)) is not null;
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
            var product = _iapCache.Get(cacheKey, false);

            if (product is null)
            {
                await _iapSemaphore.WaitAsync().ConfigureAwait(false);

                var connected = await ConnectIapAsync().ConfigureAwait(false);

                try
                {
                    if (connected)
                    {
                        var items = await _inAppBilling.GetProductInfoAsync(ItemType.Subscription, new[] { _productId }).ConfigureAwait(false);
                        product = items?.FirstOrDefault();

                        if (product is not null)
                        {
                            _iapCache.Upsert(cacheKey, product, TimeSpan.FromDays(7));
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

                    SubscriptionRestoreDateUtc = _clock.UtcNow;
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
                await AckPurchaseAsync(purchase).ConfigureAwait(false);

                var validatedReceipt = await _iapVerifyService.ValidateReceiptAsync(purchase).ConfigureAwait(false);
                if (validatedReceipt is not null)
                {
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

        private async Task<bool> AckPurchaseAsync(InAppBillingPurchase purchase)
        {
            if (string.IsNullOrEmpty(purchase?.TransactionIdentifier))
                return false;

            var success = false;

            try
            {
                var results = await _inAppBilling.FinalizePurchaseAsync(new[] { purchase.TransactionIdentifier }).ConfigureAwait(false);
                success = results?.FirstOrDefault().Success ?? false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
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