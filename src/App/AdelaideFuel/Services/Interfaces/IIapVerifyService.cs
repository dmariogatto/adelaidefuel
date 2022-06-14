using AdelaideFuel.Models;
using Plugin.InAppBilling;
using System.Threading.Tasks;

namespace AdelaideFuel.Services
{
    public interface IIapVerifyService : IInAppBillingVerifyPurchase
    {
        Task<IapValidatedReceipt> ValidateReceiptAsync(InAppBillingPurchase purchase);
    }
}