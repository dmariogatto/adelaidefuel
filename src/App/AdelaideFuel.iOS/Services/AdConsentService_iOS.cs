using AdelaideFuel.Services;
using Foundation;
using Google.UserMessagingPlatform;
using System;
using System.Threading.Tasks;

namespace AdelaideFuel.iOS.Services
{
    [Preserve(AllMembers = true)]
    public class AdConsentService_iOS : IAdConsentService
    {
        public AdConsentService_iOS()
        {
        }

        public event EventHandler<AdConsentStatusChangedEventArgs> AdConsentStatusChanged;

        public AdConsentStatus Status
            => ConvertStatus(UmpConsent.Status);

        public bool CanServeAds
            => Status switch
            {
                AdConsentStatus.NotRequired or AdConsentStatus.Obtained => true,
                _ => false,
            };

        public async Task<AdConsentStatus> RequestAsync()
        {
            var oldCanServeAds = CanServeAds;
            var oldStatus = Status;

            var consent = await UmpConsent.RequestAsync(false);

            if (oldCanServeAds != CanServeAds || oldStatus != Status)
            {
                AdConsentStatusChanged?.Invoke(this, new AdConsentStatusChangedEventArgs(
                    oldCanServeAds,
                    CanServeAds,
                    oldStatus,
                    Status));
            }

            return ConvertStatus(consent);
        }

        private static AdConsentStatus ConvertStatus(ConsentStatus status)
            => status switch
            {
                ConsentStatus.Required => AdConsentStatus.Required,
                ConsentStatus.NotRequired => AdConsentStatus.NotRequired,
                ConsentStatus.Obtained => AdConsentStatus.Obtained,
                _ => AdConsentStatus.Unknown,
            };
    }
}