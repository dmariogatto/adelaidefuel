﻿using AdelaideFuel.Models;
using AdelaideFuel.Services;
using AdelaideFuel.UI.Services;
using AdelaideFuel.ViewModels;
using Microsoft.AppCenter;
using Microsoft.AppCenter.Analytics;
using Microsoft.AppCenter.Crashes;
using Plugin.StoreReview;
using Polly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
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
        public const string Scheme = "adl-sif";
        public const string Map = "map";

        public static void IoCRegister()
        {
            IoC.RegisterSingleton<INavigationService, TabbedNavigationService>();
            IoC.RegisterSingleton<IThemeService, ThemeService>();

            var appCenterId = Constants.AppCenterSecret;
            if (!string.IsNullOrEmpty(appCenterId))
                AppCenter.Start(appCenterId, typeof(Analytics), typeof(Crashes));
        }

        static App()
        {
            AppActions.OnAppAction += (sender, args) =>
            {
                const string uriFormat = "{0}://{1}";

                if (Current is App app)
                {
                    var id = args.AppAction.Id;
                    if (id.StartsWith(nameof(UserFuel), StringComparison.Ordinal) && id.LastIndexOf("_", StringComparison.Ordinal) is int idx && idx > 0)
                    {
                        var fuelId = id.Substring(idx + 1, id.Length - idx - 1);
                        app.SendOnAppLinkRequestReceived(new Uri(string.Format(uriFormat, Scheme, $"{Map}?{NavigationKeys.FuelIdQueryProperty}={fuelId}")));
                    }

                    IoC.Resolve<ILogger>().Event(AppCenterEvents.Action.AppAction);
                }
            };
        }

        private readonly bool _skipUmp;
        private readonly SemaphoreSlim _umpSemaphore = new SemaphoreSlim(1, 1);

        public App(bool skipUmp = false)
        {
            _skipUmp = skipUmp;

            Device.SetFlags(new string[] { });

            InitializeComponent();

            Sharpnado.CollectionView.Initializer.Initialize(false, true);

            VersionTracking.Track();

            Resources.Add(Styles.Keys.CellDescriptionFontSize, Device.GetNamedSize(NamedSize.Caption, typeof(Label)));

            // Hoping this will fix a SIGABRT "Xamarin_iOS_CoreAnimation_CATransaction_Commit"
            // that I think is getting caused by AdMob's UIWebView
            Current.On<iOS>().SetHandleControlUpdatesOnMainThread(true);
            Current.On<Android>().UseWindowSoftInputModeAdjust(WindowSoftInputModeAdjust.Resize);

            var localise = IoC.Resolve<ILocalise>();
            var culture = localise.GetCurrentCultureInfo();
            culture.DateTimeFormat.SetTimePatterns(localise.Is24Hour);
            localise.SetLocale(culture);

            _ = Task.Run(async () => await IoC.Resolve<IFuelService>().SyncAllAsync(default).ConfigureAwait(false));

            IoC.Resolve<INavigationService>().Init();
            _ = AppReviewRequestAsync();
        }

        protected override void OnStart()
        {
            // Handle when your app starts

            var prefService = IoC.Resolve<IAppPreferences>();
            var lastDateOpened = prefService.LastDateOpened;

            UpdateDayCount();

            ThemeManager.LoadTheme();

            Task.Run(async () =>
            {
                var subscriptionService = IoC.Resolve<ISubscriptionService>();

#if DEBUG
                subscriptionService.SubscriptionRestoreDateUtc = null;
                subscriptionService.SubscriptionExpiryDateUtc = null;
#endif

                var wasValid = !subscriptionService.SubscriptionSuspended && subscriptionService.IsSubscriptionValidForDate(lastDateOpened);
                await subscriptionService.UpdateSubscriptionAsync().ConfigureAwait(false);

                if (wasValid && !subscriptionService.HasValidSubscription)
                {
                    Device.BeginInvokeOnMainThread(async () =>
                    {
                        var goToSubscriptionPage = await Acr.UserDialogs.UserDialogs.Instance.ConfirmAsync(
                                Localisation.Resources.SubscriptionExpiredDescription,
                                Localisation.Resources.SubscriptionExpired,
                                Localisation.Resources.GoToSubscription,
                                Localisation.Resources.Cancel);

                        if (goToSubscriptionPage)
                        {
                            var navService = IoC.Resolve<INavigationService>();
                            await navService.NavigateToAsync<SettingsViewModel>(animated: false);
                            await navService.NavigateToAsync<SubscriptionViewModel>(animated: false);
                        }
                    });
                }
            });

            Device.InvokeOnMainThreadAsync(RequestAdConsentAsync);
        }

        protected override void OnSleep()
        {
            // Handle when your app sleeps

#if DEBUG
            var sw = new System.Diagnostics.Stopwatch();
            sw.Start();
#endif

            var appShortcutsTask = SetupAppShortcutsAsync();

            // insurance policy
            IoC.Resolve<IUserNativeService>().SyncUserBrandsAsync().Wait();
            IoC.Resolve<IUserNativeService>().SyncUserFuelsAsync().Wait();
            IoC.Resolve<IUserNativeService>().SyncUserRadiiAsync().Wait();

            IoC.Resolve<IStoreFactory>().UserCheckpoint();
            IoC.Resolve<IStoreFactory>().CacheCheckpoint();

            appShortcutsTask.Wait();

#if DEBUG
            sw.Stop();
            System.Diagnostics.Debug.WriteLine($"{nameof(OnSleep)}: completed in {sw.ElapsedMilliseconds}ms");
#endif
        }

        protected override void OnResume()
        {
            // Handle when your app resumes

            UpdateDayCount();

            Device.InvokeOnMainThreadAsync(RequestAdConsentAsync);
        }

        protected override void OnAppLinkRequestReceived(Uri uri)
        {
            base.OnAppLinkRequestReceived(uri);

            if (!uri.Scheme.Equals(Scheme, StringComparison.OrdinalIgnoreCase))
                return;

            var navService = IoC.Resolve<INavigationService>();
            var task = Task.CompletedTask;

            if (uri.Host.Equals(Map, StringComparison.OrdinalIgnoreCase))
            {
                var queryParams = HttpUtility.ParseQueryString(uri.Query);
                var fuelId = queryParams.Get(NavigationKeys.FuelIdQueryProperty);

                if (int.TryParse(fuelId, out var id))
                {
                    if (navService.TopViewModel is MapViewModel mapVm)
                    {
                        mapVm.When(vm => !vm.IsBusy && vm.Fuels.Count > 0, () =>
                        {
                            var fuel = mapVm.Fuels.FirstOrDefault(f => f.Id == id);
                            if (fuel is not null)
                            {
                                mapVm.Fuel = fuel;
                            }
                        }, 2500);
                    }
                    else
                    {
                        task = navService.PopToRootAsync(false)
                            .ContinueWith(async t =>
                            {
                                await navService.NavigateToAsync<MapViewModel>(new Dictionary<string, string>()
                                {
                                    { NavigationKeys.FuelIdQueryProperty, fuelId }
                                });
                            }, TaskScheduler.FromCurrentSynchronizationContext());
                    }
                }
            }
        }

        private void UpdateDayCount()
        {
            var clock = IoC.Resolve<IAppClock>();
            var prefService = IoC.Resolve<IAppPreferences>();
            var today = clock.Today;
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
                var fuelService = IoC.Resolve<IFuelService>();

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

        private async Task SetupAppShortcutsAsync()
        {
            try
            {
                var nativeService = IoC.Resolve<IUserNativeService>();
                var fuels = nativeService.GetUserFuels();

                var actions = fuels
                    .Where(i => i.IsActive)
                    .OrderBy(i => i.SortOrder)
                    .Take(3)
                    .Select(i => new AppAction($"{nameof(UserFuel)}_{i.Id}", i.Name, icon: "fuel_shortcut"))
                    .ToArray();

                await AppActions.SetAsync(actions).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
            }
        }

        private async Task RequestAdConsentAsync()
        {
            if (_skipUmp)
                return;

            await _umpSemaphore.WaitAsync();

            try
            {
                var subscriptionService = IoC.Resolve<ISubscriptionService>();
                if (!subscriptionService.AdsEnabled)
                    return;

                var adConsentService = IoC.Resolve<IAdConsentService>();
                if (!adConsentService.ShouldRequest)
                    return;

                var retryPolicy = Policy
                    .Handle<ConsentException>()
                    .WaitAndRetryAsync
                    (
                        retryCount: 3,
                        sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt))
                    );

                var oldConsent = adConsentService.Status;
                var newConsent = await retryPolicy.ExecuteAsync(adConsentService.RequestAsync);

                if (oldConsent != newConsent)
                {
                    IoC.Resolve<ILogger>().Event(
                        AppCenterEvents.Action.AdConsent, new Dictionary<string, string>()
                        {
                            { "consent" , newConsent.ToString() }
                        });
                }
            }
            catch (Exception ex)
            {
                IoC.Resolve<ILogger>().Error(ex);
            }
            finally
            {
                _umpSemaphore.Release();
            }
        }
    }
}