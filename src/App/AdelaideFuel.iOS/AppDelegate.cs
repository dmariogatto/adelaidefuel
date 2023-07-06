using AdelaideFuel.iOS.Services;
using AdelaideFuel.Services;
using AdelaideFuel.UI;
using AdelaideFuel.UI.Services;
using Foundation;
using Microsoft.AppCenter;
using Microsoft.AppCenter.Analytics;
using Microsoft.AppCenter.Crashes;
using UIKit;

[assembly: Xamarin.Forms.ResolutionGroupName("AdelaideFuel.Effects")]

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
            IoC.RegisterSingleton<IAdConsentService, AdConsentService_iOS>();
            IoC.RegisterSingleton<ILocalise, LocaliseService_iOS>();
            IoC.RegisterSingleton<IEnvironmentService, EnvironmentService_iOS>();
            IoC.RegisterSingleton<IUserNativeReadOnlyService, UserNativeReadOnlyService_iOS>();
            IoC.RegisterSingleton<IUserNativeService, UserNativeService_iOS>();
            IoC.RegisterSingleton<IRendererService, RendererService_iOS>();
            IoC.RegisterSingleton<IRetryPolicyService, RetryPolicyService_iOS>();

            App.IoCRegister();

            var appCenterId = Constants.AppCenterSecret;
            if (!string.IsNullOrEmpty(appCenterId))
                AppCenter.Start(appCenterId, typeof(Analytics), typeof(Crashes));

            Xamarin.Forms.Forms.Init();
            FFImageLoading.Forms.Platform.CachedImageRenderer.Init();
            Sharpnado.CollectionView.iOS.Initializer.Initialize();
            AiForms.Renderers.iOS.SettingsViewInit.Init();
            Xamarin.FormsBetterMaps.Init(new Xamarin.Forms.BetterMaps.MapCache());
            Google.MobileAds.MobileAds.SharedInstance.Start(null);

#if DEBUG
            Google.MobileAds.MobileAds.SharedInstance.RequestConfiguration.TestDeviceIdentifiers =
                new string[] { "Simulator" };
            UmpConsent.Reset();
            UmpConsent.SetDebugSettings(new[] { "" }, Google.UserMessagingPlatform.DebugGeography.Eea);
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

            return base.FinishedLaunching(app, options);
        }

        public override void PerformActionForShortcutItem(UIApplication application, UIApplicationShortcutItem shortcutItem, UIOperationHandler completionHandler)
        {
            Xamarin.Essentials.Platform.PerformActionForShortcutItem(application, shortcutItem, completionHandler);
        }

        [Preserve]
        private static void Linker()
        {
            var types = new[]
            {
                // AdMob
                typeof(JavaScriptCore.JSContext)
            };
        }
    }
}
