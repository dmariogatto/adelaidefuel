using AdelaideFuel.Api;
using AdelaideFuel.Models;
using Newtonsoft.Json.Linq;
using Plugin.InAppBilling;
using Polly;
using Polly.Retry;
using System;
using System.Threading.Tasks;
using Xamarin.Essentials;
using Xamarin.Essentials.Interfaces;

namespace AdelaideFuel.Services
{
    public class IapVerifyService : BaseService, IIapVerifyService
    {
        private readonly IDeviceInfo _deviceInfo;
        private readonly IAppInfo _appInfo;

        private readonly IInAppBilling _inAppBilling;
        private readonly IIapVerifyApi _iapVerifyApi;

        private readonly AsyncRetryPolicy _retryPolicy;

        public IapVerifyService(
            IDeviceInfo deviceInfo,
            IAppInfo appInfo,
            IInAppBilling inAppBilling,
            IIapVerifyApi iapVerifyApi,
            ICacheService cacheService,
            IRetryPolicyFactory retryPolicyFactory,
            ILogger logger) : base(cacheService, logger)
        {
            _deviceInfo = deviceInfo;
            _appInfo = appInfo;

            _inAppBilling = inAppBilling;
            _iapVerifyApi = iapVerifyApi;

            _retryPolicy =
               retryPolicyFactory.GetNetRetryPolicy()
                     .WaitAndRetryAsync
                       (
                           retryCount: 2,
                           sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt))
                       );
        }

        public async Task<bool> VerifyPurchase(string signedData, string signature, string productId = null, string transactionId = null)
        {
            const string purchaseTokenProperty = "purchaseToken";

            var purchaseToken = string.Empty;

            if (_deviceInfo.Platform == DevicePlatform.iOS)
                purchaseToken = _inAppBilling.ReceiptData;
            else if (_deviceInfo.Platform == DevicePlatform.Android)
                purchaseToken = JObject.Parse(signedData).Property(purchaseTokenProperty).Value.Value<string>();
            else
                throw new NotImplementedException();

            var validatedReceipt = await ValidateReceiptAsync(transactionId, productId, purchaseToken).ConfigureAwait(false);
            return validatedReceipt != null;
        }

        public Task<IapValidatedReceipt> ValidateReceiptAsync(InAppBillingPurchase purchase)
        {
            var purchaseToken = string.Empty;

            if (_deviceInfo.Platform == DevicePlatform.iOS)
                purchaseToken = _inAppBilling.ReceiptData;
            else if (_deviceInfo.Platform == DevicePlatform.Android)
                purchaseToken = purchase.PurchaseToken;
            else
                throw new NotImplementedException();

            return ValidateReceiptAsync(purchase.Id, purchase.ProductId, purchaseToken);
        }

        private async Task<IapValidatedReceipt> ValidateReceiptAsync(string transactionId, string productId, string purchaseToken)
        {
            var validated = default(IapValidatedReceipt);

            if (string.IsNullOrEmpty(transactionId) || string.IsNullOrEmpty(productId) || string.IsNullOrEmpty(purchaseToken))
                return validated;

            var cacheKey = CacheKey(transactionId);

            if (!MemoryCache.TryGetValue(cacheKey, out validated))
            {
                var receipt = new IapReceipt()
                {
                    BundleId = _appInfo.PackageName,
                    ProductId = productId,
                    TransactionId = transactionId,
                    Token = purchaseToken,
                    AppVersion = $"{_appInfo.VersionString} ({_appInfo.BuildString})"
                };

                try
                {
                    if (_deviceInfo.Platform == DevicePlatform.iOS)
                    {
                        validated = await _retryPolicy.ExecuteAsync(
                            async (ct) => await _iapVerifyApi.AppleAsync(receipt, ct).ConfigureAwait(false),
                            default).ConfigureAwait(false);
                    }
                    else if (_deviceInfo.Platform == DevicePlatform.Android)
                    {
                        validated = await _retryPolicy.ExecuteAsync(
                            async (ct) => await _iapVerifyApi.GoogleAsync(receipt, ct).ConfigureAwait(false),
                            default).ConfigureAwait(false);
                    }

                    if (validated != null)
                    {
                        MemoryCache.SetAbsolute(cacheKey, validated, TimeSpan.FromMinutes(10));
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error(ex);
                }
            }

            return validated;
        }
    }
}