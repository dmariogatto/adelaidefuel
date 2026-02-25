using Foundation;
using UIKit;

namespace AdelaideFuel.Maui;

[Register("AppDelegate")]
public class AppDelegate : MauiUIApplicationDelegate
{
    public override bool FinishedLaunching(UIApplication application, NSDictionary launchOptions)
    {
        return base.FinishedLaunching(application, launchOptions);
    }

    protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
}