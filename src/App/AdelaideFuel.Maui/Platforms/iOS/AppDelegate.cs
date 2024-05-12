using Foundation;
using UIKit;

namespace AdelaideFuel.Maui;

[Register("AppDelegate")]
public class AppDelegate : MauiUIApplicationDelegate
{
    public override bool FinishedLaunching(UIApplication application, NSDictionary launchOptions)
    {
#if DEBUG
        Cats.Google.MobileAds.MobileAds.SharedInstance.RequestConfiguration.TestDeviceIdentifiers =
            new string[] { "Simulator" };
#endif

        return base.FinishedLaunching(application, launchOptions);
    }

    protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
}