using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;

namespace AdelaideFuel.Maui;

[Activity(
    Theme = "@style/Maui.SplashTheme",
    MainLauncher = true,
    ConfigurationChanges =
        ConfigChanges.ScreenSize |
        ConfigChanges.Orientation |
        ConfigChanges.UiMode |
        ConfigChanges.ScreenLayout |
        ConfigChanges.SmallestScreenSize |
        ConfigChanges.Density,
    LaunchMode = LaunchMode.SingleTask)]
[IntentFilter(
   new[] { Platform.Intent.ActionAppAction },
   Categories = new[] { Intent.CategoryDefault })]
public class MainActivity : MauiAppCompatActivity
{
    protected override void OnCreate(Bundle savedInstanceState)
    {
        base.OnCreate(savedInstanceState);

        if (DeviceInfo.Current.Idiom == DeviceIdiom.Phone)
            RequestedOrientation = ScreenOrientation.UserPortrait;

#if DEBUG
        var testDevices = new List<string>()
            {
                Android.Gms.Ads.AdRequest.DeviceIdEmulator
            };

        Android.Gms.Ads.MobileAds.RequestConfiguration = new Android.Gms.Ads.RequestConfiguration.Builder()
            .SetTestDeviceIds(testDevices)
            .Build();
#endif
    }
}
