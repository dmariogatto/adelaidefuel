using AdelaideFuel.Api;
using AdelaideFuel.Essentials;
using AdelaideFuel.Services;
using AdelaideFuel.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.ApplicationModel.Communication;
using Microsoft.Maui.ApplicationModel.DataTransfer;
using Microsoft.Maui.Authentication;
using Microsoft.Maui.Devices;
using Microsoft.Maui.Devices.Sensors;
using Microsoft.Maui.Media;
using Microsoft.Maui.Networking;
using Microsoft.Maui.Storage;
using Plugin.InAppBilling;
using System;
using System.Net.Http;

[assembly: AdelaideFuel.Attributes.Preserve]
namespace AdelaideFuel
{
    public static class IoC
    {
        public static IServiceProvider Services { get; private set; }

        public static void Init(IServiceProvider services)
        {
            if (Services is not null)
                throw new InvalidOperationException("Init can only be called once!");

            Services = services;
        }

        public static void ConfigureIoC(this IServiceCollection services)
        {
            services.AddSingleton<IAdelaideFuelApi>(_ =>
            {
                var client = new HttpClient()
                {
                    BaseAddress = new Uri(Constants.ApiUrlBase),
                };
                var fuelApi = new AdelaideFuelApi(client);
                return fuelApi;
            });
            services.AddSingleton<IIapVerifyApi>(_ =>
            {
                var client = new HttpClient()
                {
                    BaseAddress = new Uri(Constants.ApiUrlIapBase),
                };
                var iapApi = new IapVerifyApi(client);
                return iapApi;
            });

            services.AddSingleton<IInAppBilling>(_ => CrossInAppBilling.Current);

            services.AddSingleton<IAppClock, AppClock>();
            services.AddSingleton<ICacheService, CacheService>();
            services.AddSingleton<IStoreFactory, StoreFactory>();
            services.AddSingleton<IFuelService, FuelService>();
            services.AddSingleton<IBrandService, BrandService>();
            services.AddSingleton<IIapVerifyService, IapVerifyService>();
            services.AddSingleton<ISubscriptionService, SubscriptionService>();
            services.AddSingleton<IAppPreferences, AppPreferences>();
            services.AddSingleton<IRetryPolicyFactory, RetryPolicyFactory>();
            services.AddSingleton<IBvmConstructor, BvmConstructor>();

            services.RegisterEssentials();
            services.RegisterViewModels();
        }

        public static T Resolve<T>() where T : class
            => Services.GetService<T>();

        private static void RegisterEssentials(this IServiceCollection services)
        {
            services.AddSingleton<IAccelerometer>(_ => Accelerometer.Default);
            services.AddSingleton<IAppActions>(_ => AppActions.Current);
            services.AddSingleton<IAppInfo>(_ => AppInfo.Current);
            services.AddSingleton<IBarometer>(_ => Barometer.Default);
            services.AddSingleton<IBattery>(_ => Battery.Default);
            services.AddSingleton<IBrowser>(_ => Browser.Default);
            services.AddSingleton<IClipboard>(_ => Clipboard.Default);
            services.AddSingleton<ICompass>(_ => Compass.Default);
            services.AddSingleton<IConnectivity>(_ => Connectivity.Current);
            services.AddSingleton<IContacts>(_ => Contacts.Default);
            services.AddSingleton<IDeviceDisplay>(_ => DeviceDisplay.Current);
            services.AddSingleton<IDeviceInfo>(_ => DeviceInfo.Current);
            services.AddSingleton<IEmail>(_ => Email.Default);
            services.AddSingleton<IFilePicker>(_ => FilePicker.Default);
            services.AddSingleton<IFileSystem>(_ => FileSystem.Current);
            services.AddSingleton<IFlashlight>(_ => Flashlight.Default);
            services.AddSingleton<IGeocoding>(_ => Geocoding.Default);
            services.AddSingleton<IGeolocation>(_ => Geolocation.Default);
            services.AddSingleton<IGyroscope>(_ => Gyroscope.Default);
            services.AddSingleton<IHapticFeedback>(_ => HapticFeedback.Default);
            services.AddSingleton<ILauncher>(_ => Launcher.Default);
            services.AddSingleton<IMagnetometer>(_ => Magnetometer.Default);
            services.AddSingleton<IMap>(_ => Map.Default);
            services.AddSingleton<IMediaPicker>(_ => MediaPicker.Default);
            services.AddSingleton<IOrientationSensor>(_ => OrientationSensor.Default);
            services.AddSingleton<IPermissions>(_ => new PermissionsImplementation());
            services.AddSingleton<IPhoneDialer>(_ => PhoneDialer.Default);
            services.AddSingleton<IPreferences>(_ => Preferences.Default);
            services.AddSingleton<IScreenshot>(_ => Screenshot.Default);
            services.AddSingleton<ISecureStorage>(_ => SecureStorage.Default);
            services.AddSingleton<IShare>(_ => Share.Default);
            services.AddSingleton<ISms>(_ => Sms.Default);
            services.AddSingleton<ITextToSpeech>(_ => TextToSpeech.Default);
            services.AddSingleton<IVersionTracking>(_ => VersionTracking.Default);
            services.AddSingleton<IVibration>(_ => Vibration.Default);
            services.AddSingleton<IWebAuthenticator>(_ => WebAuthenticator.Default);
        }

        private static void RegisterViewModels(this IServiceCollection services)
        {
            services.AddTransient<BrandsViewModel>();
            services.AddTransient<FuelsViewModel>();
            services.AddTransient<MapViewModel>();
            services.AddTransient<PricesViewModel>();
            services.AddTransient<RadiiViewModel>();
            services.AddTransient<SettingsViewModel>();
            services.AddTransient<SiteSearchViewModel>();
            services.AddTransient<SubscriptionViewModel>();
        }
    }
}