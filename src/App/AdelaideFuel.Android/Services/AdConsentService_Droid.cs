using AdelaideFuel.Services;
using Android.Runtime;
using System.Threading.Tasks;
using Xamarin.Google.UserMesssagingPlatform;

namespace AdelaideFuel.Droid.Services
{
    [Preserve(AllMembers = true)]
    public class AdConsentService_Droid : IAdConsentService
    {
        public AdConsentService_Droid()
        {
        }

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
            var consent = await UmpConsent.RequestAsync(false);
            return ConvertStatus(consent);
        }

        private static AdConsentStatus ConvertStatus(int status)
            => status switch
            {
                ConsentInformationConsentStatus.Required => AdConsentStatus.Required,
                ConsentInformationConsentStatus.NotRequired => AdConsentStatus.NotRequired,
                ConsentInformationConsentStatus.Obtained => AdConsentStatus.Obtained,
                _ => AdConsentStatus.Unknown,
            };
    }
}