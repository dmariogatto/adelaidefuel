using AdelaideFuel.iOS.Services;
using AdelaideFuel.Services;
using AdelaideFuel.UI;
using AdelaideFuel.UI.Services;
using Foundation;
using Microsoft.AppCenter;
using Microsoft.AppCenter.Analytics;
using Microsoft.AppCenter.Crashes;
using System;
using System.Collections.Generic;
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

            var formsApp = default(App);
            var lineNum = string.Empty;

            try
            {
                lineNum = "1";
                global::Xamarin.Forms.Forms.Init();
                lineNum = "2";
                FFImageLoading.Forms.Platform.CachedImageRenderer.Init();
                lineNum = "3";
                Sharpnado.CollectionView.iOS.Initializer.Initialize();
                lineNum = "4";
                AiForms.Renderers.iOS.SettingsViewInit.Init();
                lineNum = "5";
                Xamarin.FormsBetterMaps.Init(new Xamarin.Forms.BetterMaps.MapCache());
                lineNum = "6";
                Google.MobileAds.MobileAds.SharedInstance.Start(null);
                lineNum = "7";

#if DEBUG
                Google.MobileAds.MobileAds.SharedInstance.RequestConfiguration.TestDeviceIdentifiers =
                    new string[] { "Simulator" };
                UmpConsent.Reset();
                UmpConsent.SetDebugSettings(new[] { "" }, Google.UserMessagingPlatform.DebugGeography.Eea);
#endif

                formsApp = new App();
                lineNum = "8";
            }
            catch (Exception ex)
            {
                var logger = IoC.Resolve<ILogger>();
                logger.Error(ex, new Dictionary<string, string>()
                {
                    { "line_num", lineNum }
                });
            }

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

            LoadApplication(formsApp ??= new App(true));
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
