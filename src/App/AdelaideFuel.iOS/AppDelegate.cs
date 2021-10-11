using AdelaideFuel.iOS.Services;
using AdelaideFuel.Services;
using AdelaideFuel.UI;
using AdelaideFuel.UI.Services;
using Foundation;
using UIKit;
using Xamarin.Forms;

[assembly: ResolutionGroupName("AdelaideFuel.Effects")]

namespace AdelaideFuel.iOS
{
    // The UIApplicationDelegate for the application. This class is responsible for launching the
    // User Interface of the application, as well as listening (and optionally responding) to
    // application events from iOS.
    [Register("AppDelegate")]
    public class AppDelegate : global::Xamarin.Forms.Platform.iOS.FormsApplicationDelegate
    {
        //
        // This method is invoked when the application has loaded and is ready to run. In this
        // method you should instantiate the window, load the UI into it and then make the window
        // visible.
        //
        // You have 17 seconds to return from this method, or iOS will terminate your application.
        //
        public override bool FinishedLaunching(UIApplication app, NSDictionary options)
        {
            // Linker
            var types = new[]
            {
                // AdMob
                typeof(JavaScriptCore.JSContext)
            };

            IoC.RegisterSingleton<ILocalise, LocaliseService_iOS>();
            IoC.RegisterSingleton<IEnvironmentService, EnvironmentService_iOS>();
            IoC.RegisterSingleton<IUserNativeReadOnlyService, UserNativeReadOnlyService_iOS>();
            IoC.RegisterSingleton<IUserNativeService, UserNativeService_iOS>();
            IoC.RegisterSingleton<IRendererService, RendererService_iOS>();
            IoC.RegisterSingleton<IRetryPolicyService, RetryPolicyService_iOS>();

            FFImageLoading.Forms.Platform.CachedImageRenderer.Init();
            Sharpnado.HorizontalListView.iOS.SharpnadoInitializer.Initialize();
            AiForms.Renderers.iOS.SettingsViewInit.Init();
            Xamarin.FormsBetterMaps.Init(new Xamarin.Forms.BetterMaps.MapCache());
            Google.MobileAds.MobileAds.SharedInstance.Start(null);

            global::Xamarin.Forms.Forms.Init();

#if DEBUG
            Google.MobileAds.MobileAds.SharedInstance.RequestConfiguration.TestDeviceIdentifiers =
                new string[] { "Simulator" };
#endif

            var formsApp = new App();
            LoadApplication(formsApp);

            // Not even worth asking 🙄
            //if (false &&
            //    UIDevice.CurrentDevice.CheckSystemVersion(14, 0) &&
            //    ATTrackingManager.TrackingAuthorizationStatus == ATTrackingManagerAuthorizationStatus.NotDetermined)
            //{
            //    ATTrackingManager.RequestTrackingAuthorization(status =>
            //    {
            //        IoC.Resolve<ILogger>().Event(AppCenterEvents.Action.TrackingAuthorization, new Dictionary<string, string>()
            //            {
            //                { nameof(status), status.ToString() }
            //            });
            //    });
            //}
            // Info.plist entry
            // <key>NSUserTrackingUsageDescription</key>
            // <string>Personalised ads for the best experience.</string>

            if (UIDevice.CurrentDevice.CheckSystemVersion(15, 0))
            {
                var tabAppearance = new UITabBarAppearance();
                tabAppearance.ConfigureWithOpaqueBackground();
                UITabBar.Appearance.StandardAppearance = tabAppearance;
                UITabBar.Appearance.ScrollEdgeAppearance = tabAppearance;
            }

            return base.FinishedLaunching(app, options);
        }
    }
}
