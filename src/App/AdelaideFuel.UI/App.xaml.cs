using AdelaideFuel.Services;
using AdelaideFuel.UI.Services;
using Microsoft.AppCenter;
using Microsoft.AppCenter.Analytics;
using Microsoft.AppCenter.Crashes;
using Plugin.StoreReview;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xamarin.Essentials;
using Xamarin.Forms;
using Xamarin.Forms.PlatformConfiguration;
using Xamarin.Forms.PlatformConfiguration.AndroidSpecific;
using Xamarin.Forms.PlatformConfiguration.iOSSpecific;
using Device = Xamarin.Forms.Device;

namespace AdelaideFuel.UI
{
    public partial class App : Xamarin.Forms.Application
    {
        static App()
        {
            IoC.RegisterSingleton<INavigationService, TabbedNavigationService>();
            IoC.RegisterSingleton<IThemeService, ThemeService>();
        }

        public App()
        {
            Device.SetFlags(new string[] { });

            InitializeComponent();

            Sharpnado.HorizontalListView.Initializer.Initialize(false, true);

            var appCenterId = Constants.AppCenterSecret;
            if (!string.IsNullOrEmpty(appCenterId))
                AppCenter.Start(appCenterId, typeof(Analytics), typeof(Crashes));

            VersionTracking.Track();
            UpdateDayCount();

            Resources.Add(Styles.Keys.CellDescriptionFontSize, Device.GetNamedSize(NamedSize.Caption, typeof(Label)));

            // Hoping this will fix a SIGABRT "Xamarin_iOS_CoreAnimation_CATransaction_Commit"
            // that I think is getting caused by AdMob's UIWebView
            Current.On<iOS>().SetHandleControlUpdatesOnMainThread(true);
            Current.On<Android>().UseWindowSoftInputModeAdjust(WindowSoftInputModeAdjust.Resize);

            var localise = IoC.Resolve<ILocalise>();
            var culture = localise.GetCurrentCultureInfo();
            culture.DateTimeFormat.SetTimePatterns(localise.Is24Hour);
            localise.SetLocale(culture);

            IoC.Resolve<INavigationService>().Init();
            _ = AppReviewRequestAsync();
        }

        protected override void OnStart()
        {
            // Handle when your app starts

            ThemeManager.LoadTheme();
        }

        protected override void OnSleep()
        {
            // Handle when your app sleeps

            var sw = new System.Diagnostics.Stopwatch();
            sw.Start();

            // insurance policy
            IoC.Resolve<IUserNativeService>().SyncUserBrandsAsync().Wait();
            IoC.Resolve<IUserNativeService>().SyncUserFuelsAsync().Wait();

            IoC.Resolve<IStoreFactory>().UserCheckpoint();
            IoC.Resolve<IStoreFactory>().CacheCheckpoint();

            sw.Stop();
        }

        protected override void OnResume()
        {
            // Handle when your app resumes

            UpdateDayCount();
        }

        private void UpdateDayCount()
        {
            var prefService = IoC.Resolve<IAppPreferences>();
            var today = DateTime.Now.Date;
            if (prefService.LastDateOpened < today)
            {
                prefService.LastDateOpened = today;
                prefService.DayCount++;
            }
        }

        private async Task AppReviewRequestAsync()
        {
            var appPrefs = IoC.Resolve<IAppPreferences>();

            if (appPrefs.ReviewRequested)
                return;

            try
            {
                var metroService = IoC.Resolve<IFuelService>();

                if (appPrefs.DayCount >= 14)
                {
                    var testMode = false;
#if DEBUG
                    testMode = true;
#endif
                    await CrossStoreReview.Current.RequestReview(testMode);
                    appPrefs.ReviewRequested = true;
                    IoC.Resolve<ILogger>().Event(AppCenterEvents.Action.ReviewRequested, new Dictionary<string, string>(1)
                    {
                        { nameof(DeviceInfo.Platform).ToLower(), DeviceInfo.Platform.ToString() }
                    });
                }
            }
            catch (Exception ex)
            {
                IoC.Resolve<ILogger>().Error(ex);
            }
        }
    }
}