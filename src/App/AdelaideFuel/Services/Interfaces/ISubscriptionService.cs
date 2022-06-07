using AdelaideFuel.Models;
using Plugin.InAppBilling;
using System;
using System.Threading.Tasks;

namespace AdelaideFuel.Services
{
    public interface ISubscriptionService
    {
        bool HasValidSubscription { get; }

        DateTime? SubscriptionRestoreDateUtc { get; set; }
        DateTime? SubscriptionExpiryDateUtc { get; set; }
        bool AdsEnabled { get; set; }

        Task<bool> UpdateSubscriptionAsync();

        Task<InAppBillingProduct> GetProductAsync();
        Task<IapValidatedReceipt> PurchaseAsync();
        Task<IapValidatedReceipt> RestoreAsync();
    }
}