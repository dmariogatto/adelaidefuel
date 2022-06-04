using AdelaideFuel.Models;
using Plugin.InAppBilling;
using System;
using System.Threading.Tasks;

namespace AdelaideFuel.Services
{
    public interface ISubscriptionService
    {
        Task<DateTime?> ExpiryDateUtcAsync();
        Task ExpiryDateUtcAsync(DateTime? expiryDateUtc);
        Task<bool> BannerAdsAsync();
        Task BannerAdsAsync(bool enabled);

        Task<bool> UpdateSubscriptionAsync();
        Task<bool> IsValidAsync();

        Task<InAppBillingProduct> GetProductAsync();
        Task<IapValidatedReceipt> PurchaseAsync();
        Task<IapValidatedReceipt> RestoreAsync();
    }
}