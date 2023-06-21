using AdelaideFuel.Services;
using Foundation;
using Google.UserMessagingPlatform;
using System.Threading.Tasks;

namespace AdelaideFuel.iOS.Services
{
    [Preserve(AllMembers = true)]
    public class AdConsentService_iOS : IAdConsentService
    {
        public AdConsentService_iOS()
        {
        }

        public AdConsentStatus Status
            => ConvertStatus(UmpConsent.Instance.ConsentStatus);

        public bool CanServeAds
            => Status switch
            {
                AdConsentStatus.NotRequired or AdConsentStatus.Obtained => true,
                _ => false,
            };

        public async Task<AdConsentStatus> RequestAsync()
        {
            var consent = await UmpConsent.RequestAsync(false);
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