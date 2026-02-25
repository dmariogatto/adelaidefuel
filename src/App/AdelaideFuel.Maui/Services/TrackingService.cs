using AdelaideFuel.Services;

#if __IOS__
using AdSupport;
using AppTrackingTransparency;
#endif

#if __ANDROID__
using Google.Ads.Identifier;
#endif

namespace AdelaideFuel.Maui.Services;

public class TrackingService : ITrackingService
{
    public async Task<Guid> GetIdfaAsync()
    {
        try
        {
#if __IOS__
            var status = await ATTrackingManager.RequestTrackingAuthorizationAsync();

            System.Diagnostics.Debug.WriteLine($"ATTrackingManager.AuthorizationStatus: {status}");

            switch (status)
            {
                case ATTrackingManagerAuthorizationStatus.Authorized:
                    var ifda = ASIdentifierManager.SharedManager.AdvertisingIdentifier.AsString();
                    System.Diagnostics.Debug.WriteLine($"ASIdentifierManager.AdvertisingIdentifier: {ifda}");
                    return Guid.Parse(ifda);
                case ATTrackingManagerAuthorizationStatus.Denied:
                case ATTrackingManagerAuthorizationStatus.Restricted:
                case ATTrackingManagerAuthorizationStatus.NotDetermined:
                default:
                    break;
            }
#elif __ANDROID__
            var (limited, idfa)  = await Task.Run(() =>
            {
                var adInfo = AdvertisingIdClient.GetAdvertisingIdInfo(Platform.AppContext);
                return (adInfo.IsLimitAdTrackingEnabled, adInfo.Id);
            });
            System.Diagnostics.Debug.WriteLine($"AdvertisingIdClient.IsLimitAdTrackingEnabled: {limited}");
            System.Diagnostics.Debug.WriteLine($"AdvertisingIdClient.Id: {idfa ?? string.Empty}");
            return !string.IsNullOrWhiteSpace(idfa) ? Guid.Parse(idfa) : Guid.Empty;
#else
            throw new NotImplementedException();
#endif
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine("Unable request IDFA: " + ex.Message);
        }

        return Guid.Empty;
    }
}