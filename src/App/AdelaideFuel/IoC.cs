using Acr.UserDialogs;
using AdelaideFuel.Api;
using AdelaideFuel.Services;
using AdelaideFuel.ViewModels;
using Newtonsoft.Json;
using Plugin.InAppBilling;
using Plugin.StoreReview;
using Refit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using TinyIoC;
using Xamarin.Essentials.Implementation;
using Xamarin.Essentials.Interfaces;

[assembly: AdelaideFuel.Attributes.Preserve]
namespace AdelaideFuel
{
    public static class IoC
    {
        #region Type Maps
        private static readonly Dictionary<Type, Type> EssentialTypes = new Dictionary<Type, Type>()
        {
            { typeof(IAccelerometer), typeof(AccelerometerImplementation) },
            { typeof(IAppActions), typeof(AppActionsImplementation) },
            { typeof(IAppInfo), typeof(AppInfoImplementation) },
            { typeof(IBarometer), typeof(BarometerImplementation) },
            { typeof(IBattery), typeof(BatteryImplementation) },
            { typeof(IBrowser), typeof(BrowserImplementation) },
            { typeof(IClipboard), typeof(ClipboardImplementation) },
            { typeof(ICompass), typeof(CompassImplementation) },
            { typeof(IConnectivity), typeof(ConnectivityImplementation) },
            { typeof(IContacts), typeof(ContactsImplementation) },
            { typeof(IDeviceDisplay), typeof(DeviceDisplayImplementation) },
            { typeof(IDeviceInfo), typeof(DeviceInfoImplementation) },
            { typeof(IEmail), typeof(EmailImplementation) },
            { typeof(IFilePicker), typeof(FilePickerImplementation) },
            { typeof(IFileSystem), typeof(FileSystemImplementation) },
            { typeof(IFlashlight), typeof(FlashlightImplementation) },
            { typeof(IGeocoding), typeof(GeocodingImplementation) },
            { typeof(IGeolocation), typeof(GeolocationImplementation) },
            { typeof(IGyroscope), typeof(GyroscopeImplementation) },
            { typeof(IHapticFeedback), typeof(HapticFeedbackImplementation) },
            { typeof(ILauncher), typeof(LauncherImplementation) },
            { typeof(IMagnetometer), typeof(MagnetometerImplementation) },
            { typeof(IMainThread), typeof(MainThreadImplementation) },
            { typeof(IMap), typeof(MapImplementation) },
            { typeof(IMediaPicker), typeof(MediaPickerImplementation) },
            { typeof(IOrientationSensor), typeof(OrientationSensorImplementation) },
            { typeof(IPermissions), typeof(PermissionsImplementation) },
            { typeof(IPhoneDialer), typeof(PhoneDialerImplementation) },
            { typeof(IPreferences), typeof(PreferencesImplementation) },
            { typeof(IScreenshot), typeof(ScreenshotImplementation) },
            { typeof(ISecureStorage), typeof(SecureStorageImplementation) },
            { typeof(IShare), typeof(ShareImplementation) },
            { typeof(ISms), typeof(SmsImplementation) },
            { typeof(ITextToSpeech), typeof(TextToSpeechImplementation) },
            { typeof(IVersionTracking), typeof(VersionTrackingImplementation) },
            { typeof(IVibration), typeof(VibrationImplementation) },
            { typeof(IWebAuthenticator), typeof(WebAuthenticatorImplementation) }
        };

        private static readonly List<Type> ViewModelTypes = new List<Type>()
        {
            typeof(BrandsViewModel),
            typeof(FuelsViewModel),
            typeof(MapViewModel),
            typeof(PricesViewModel),
            typeof(RadiiViewModel),
            typeof(SettingsViewModel),
            typeof(SiteSearchViewModel),
            typeof(SubscriptionViewModel),
        };
        #endregion

        private static readonly TinyIoCContainer Container = new TinyIoCContainer();

        static IoC()
        {
            JsonConvert.DefaultSettings = () => new JsonSerializerSettings()
            {
                DateTimeZoneHandling = DateTimeZoneHandling.Utc
            };

            var refitNewtonsoftSettings = new RefitSettings(new NewtonsoftJsonContentSerializer());
            Container.Register((c, e) =>
            {
                var client = new HttpClient()
                {
                    BaseAddress = new Uri(Constants.ApiUrlBase),
                };
                var metroApi = RestService.For<IAdelaideFuelApi>(client, refitNewtonsoftSettings);
                return metroApi;
            }).AsSingleton();
            Container.Register((c, e) =>
            {
                var client = new HttpClient()
                {
                    BaseAddress = new Uri(Constants.ApiUrlIapBase),
                };
                var iapApi = RestService.For<IIapVerifyApi>(client, refitNewtonsoftSettings);
                return iapApi;
            }).AsSingleton();

            Container.Register((c, e) => CrossInAppBilling.Current).AsSingleton();
            Container.Register((c, e) => UserDialogs.Instance).AsSingleton();
            Container.Register((c, e) => CrossStoreReview.Current).AsSingleton();

            Container.Register<ILogger, Logger>().AsSingleton();
            Container.Register<ICacheService, CacheService>().AsSingleton();
            Container.Register<IStoreFactory, StoreFactory>().AsSingleton();
            Container.Register<IFuelService, FuelService>().AsSingleton();
            Container.Register<IIapVerifyService, IapVerifyService>().AsSingleton();
            Container.Register<ISubscriptionService, SubscriptionService>().AsSingleton();
            Container.Register<IAppPreferences, AppPreferences>().AsSingleton();
            Container.Register<IRetryPolicyFactory, RetryPolicyFactory>().AsSingleton();
            Container.Register<IBvmConstructor, BvmConstructor>().AsSingleton();

#if DEBUG
            var reflectedEssentials = GetEssentialInterfaceAndImplementations();
            if (!reflectedEssentials.DictionaryEqual(EssentialTypes))
            {
                foreach (var i in reflectedEssentials)
                    System.Diagnostics.Debug.WriteLine($"{{ typeof({i.Key.Name}), typeof({i.Value.Name}) }},");
                throw new Exception("Essential Types do not match!");
            }

            var reflectedViewModels = GetViewModelTypes();
            if (!reflectedViewModels.SequenceEqual(ViewModelTypes))
            {
                foreach (var i in reflectedViewModels)
                    System.Diagnostics.Debug.WriteLine($"{{ typeof({i.Name}) }},");
                throw new Exception("ViewModel Types do not match!");
            }
#endif

            foreach (var e in EssentialTypes)
                Container.Register(e.Key, e.Value).AsSingleton();

            foreach (var vmType in ViewModelTypes)
                Container.Register(vmType).AsMultiInstance();
        }

        public static IDictionary<Type, Type> GetEssentialInterfaceAndImplementations()
        {
            var result = new Dictionary<Type, Type>();

            var essentialImpls = typeof(IEssentialsImplementation)
                .Assembly
                .GetTypes()
                .Where(t => t.IsClass && t.Namespace.EndsWith(nameof(Xamarin.Essentials.Implementation), StringComparison.Ordinal));

            foreach (var impl in essentialImpls)
            {
                var implInterface = impl.GetInterfaces().First(i => i != typeof(IEssentialsImplementation));
                result.Add(implInterface, impl);
            }

            return result;
        }

        public static IList<Type> GetViewModelTypes()
        {
            return typeof(BaseViewModel)
                .Assembly
                .GetTypes()
                .Where(t => t.IsClass &&
                            !t.IsAbstract &&
                            t.GetInterfaces().Contains(typeof(IViewModel)))
                .ToList();
        }

        public static T Resolve<T>() where T : class
        {
            return Container.Resolve<T>();
        }

        public static TViewModel ResolveViewModel<TViewModel>() where TViewModel : class, IViewModel
        {
            return Container.Resolve<TViewModel>();
        }

        public static void RegisterSingleton<TService, TImplementation>() where TService : class where TImplementation : class, TService
        {
            Container.Register<TService, TImplementation>().AsSingleton();
        }

        public static void RegisterSingleton(Type serviceType, Type implementationType)
        {
            Container.Register(serviceType, implementationType).AsSingleton();
        }

        public static void RegisterSingleton<T>(Func<T> instanceCreator) where T : class
        {
            Container.Register((_, _) => instanceCreator()).AsSingleton();
        }

        public static void RegisterTransient<TService, TImplementation>() where TService : class where TImplementation : class, TService
        {
            Container.Register<TService, TImplementation>().AsMultiInstance();
        }

        public static void RegisterTransient(Type serviceType, Type implementationType)
        {
            Container.Register(serviceType, implementationType).AsMultiInstance();
        }

        public static void RegisterTransient(Type serviceType, Func<object> instanceCreator)
        {
            Container.Register(serviceType, instanceCreator).AsMultiInstance();
        }
    }
}