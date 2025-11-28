using AdelaideFuel.Maui.Controls;
using AdelaideFuel.Maui.Effects;
using AdelaideFuel.Maui.Handlers;
using AdelaideFuel.Maui.Helpers;
using AdelaideFuel.Maui.ImageSources;
using AdelaideFuel.Maui.Services;
using AdelaideFuel.Models;
using AdelaideFuel.Services;
using AiForms.Settings;
using Android.Gms.Maps;
using BetterMaps.Maui;
using BetterMaps.Maui.Handlers;
using Cats.Maui.AdMob;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Handlers;
using Sharpnado.CollectionView;
using Sharpnado.Tabs;
using ILogger = AdelaideFuel.Services.ILogger;
using IMap = BetterMaps.Maui.IMap;

namespace AdelaideFuel.Maui;

public static class MauiProgram
{
    private static readonly DateTime AppStartTime = DateTime.Now;

    public static MauiApp CreateMauiApp()
    {
#if DEBUG || !SENTRY
        GlobalExceptionHandler.UnhandledException += GlobalExceptionHandlerOnUnhandledException;
#endif

        MigrateVersionTracking();

#if ANDROID
        AndroidSecureStorageWorkaroundAsync().Wait();
#endif

        AppActions.OnAppAction += OnAppAction;
        VersionTracking.Track();

        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
#if SENTRY
            .UseSentry(options =>
            {
                options.Dsn = Constants.SentryDsn;

#if DEBUG
                options.Debug = false;
                options.TracesSampleRate = 0.5;
#else
                options.Debug = false;
                options.TracesSampleRate = 0.0;
#endif

                options.DisableUnobservedTaskExceptionCapture();

                options.SetBeforeSend((sentryEvent, hint) =>
                {
                    var logger = IoC.Resolve<ILogger>();
                    if (sentryEvent.Exception is not null && !logger.ShouldLogException(sentryEvent.Exception))
                    {
                        return null;
                    }

                    return sentryEvent;
                });
            })
#endif
            .ConfigureMauiHandlers(handlers =>
            {
                handlers.AddSettingsViewHandler();

                ImageHandler.Mapper.AppendToMapping(nameof(TintImage.TintColor), TintImageHandler.MapTintColor);
#if IOS || MACCATALYST
                MapHandler.Mapper.ModifyMapping(nameof(IMap.ShowUserLocationButton), (h, e, _) => MapCustomHandler.MapShowUserLocationButton(h, e));

                handlers.AddHandler(typeof(ContentPage), typeof(PageCustomHandler));
                handlers.AddHandler(typeof(NavigationPage), typeof(NavigationCustomRenderer));
                handlers.AddHandler(typeof(Border), typeof(BorderCustomHandler));

                SearchBarHandler.Mapper.AppendToMapping(nameof(SearchBar.CancelButtonColor), (handler, _) => handler.PlatformView.SetShowsCancelButton(false, false));
#elif ANDROID
                MapHandler.CommandMapper.AppendToMapping(nameof(GoogleMap.IOnMapLoadedCallback.OnMapLoaded), MapCustomHandler.MapOnMapLoaded);
#endif
            })
            .ConfigureEffects(effects =>
            {
                effects.Add<SafeAreaInsetEffect, SafeAreaInsetPlatformEffect>();
                effects.Add<SearchBarIconEffect, SearchBarIconPlatformEffect>();
            })
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("NunitoSans-Regular.ttf", "NunitoSans-Regular");
                fonts.AddFont("NunitoSans-Italic.ttf", "NunitoSans-Italic");
                fonts.AddFont("NunitoSans-SemiBold.ttf", "NunitoSans-SemiBold");
                fonts.AddFont("NunitoSans-Bold.ttf", "NunitoSans-Bold");
                fonts.AddFont("NunitoSans-BoldItalic.ttf", "NunitoSans-BoldItalic");
            })
            .ConfigureImageSources(services =>
            {
                services.AddService<IFileAsyncImageSource>(svcs =>
                {
                    var provider = svcs.GetService<IImageSourceServiceProvider>();
                    var fileImageSourceService = provider.GetImageSourceService(typeof(IFileImageSource)) as IImageSourceService<IFileImageSource>;
                    var logger = svcs.GetService<ILogger<FileAsyncImageSourceService>>();
                    return new FileAsyncImageSourceService(fileImageSourceService, logger);
                });
            })
            .UseSharpnadoTabs(loggerEnable: false)
            .UseSharpnadoCollectionView(false);


#if DEBUG
        // Configure logging
        builder.Logging.AddDebug();
#endif

        builder.Services.AddSingleton<IMapCache, MapCache>();
#if ANDROID
        builder.UseMauiMaps(
            lightThemeAsset: "map.style.light.json",
            darkThemeAsset: "map.style.dark.json");
#else
        builder.UseMauiMaps();
#endif

        builder.UseMauiAdMob();
        builder.Services.AddDefaultAdConsentService();

        builder.Services.ConfigureIoC();

        builder.Services.AddSingleton<IDialogService, DialogService>();
        builder.Services.AddSingleton<ILogger, Logger>();
        builder.Services.AddSingleton<ILocalise, LocalisePlatformService>();
        builder.Services.AddSingleton<IEnvironmentService, EnvironmentPlatformService>();
        builder.Services.AddSingleton<IRetryPolicyService, RetryPolicyPlatformService>();
        builder.Services.AddSingleton<INavigationService, TabbedNavigationService>();
        builder.Services.AddSingleton<IThemeService, ThemeService>();
        builder.Services.AddSingleton<IStoreReview, StoreReview>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        var mauiApp = builder.Build();

        IoC.Init(mauiApp.Services);

        var localise = IoC.Resolve<ILocalise>();
        var culture = localise.GetCurrentCultureInfo();
        culture.DateTimeFormat.SetTimePatterns(localise.Is24Hour);
        localise.SetLocale(culture);

        _ = Task.Run(async () => await IoC.Resolve<IFuelService>().SyncAllAsync(default).ConfigureAwait(false));

        return mauiApp;
    }

    private static void GlobalExceptionHandlerOnUnhandledException(object sender, UnhandledExceptionEventArgs args)
    {
        if (args.ExceptionObject is not Exception ex)
            return;

        try
        {
            IoC.Resolve<ILogger>().Error(ex, new Dictionary<string, string>
            {
                { nameof(args.IsTerminating), args.IsTerminating.ToString() }
            });
        }
        catch (Exception loggingEx)
        {
            System.Diagnostics.Debug.WriteLine(loggingEx);
            System.Diagnostics.Debug.WriteLine(ex);
        }

        if (args.IsTerminating && Email.Default.IsComposeSupported)
        {
            var appRunTime = DateTime.Now - AppStartTime;
            if (appRunTime < TimeSpan.FromSeconds(20))
            {
                var message = Email.Default.GetCrashingEmailMessage(AppInfo.Current, DeviceInfo.Current, ex);
                _ = Email.Default.ComposeAsync(message);
            }
        }
    }

    private static void OnAppAction(object sender, AppActionEventArgs args)
    {
        const string uriFormat = "{0}://{1}";

        if (Application.Current is App app)
        {
            var id = args.AppAction.Id;

            if (id.StartsWith(nameof(UserFuel), StringComparison.Ordinal) && id.LastIndexOf('_') is int idx && idx > 0)
            {
                var fuelId = id.Substring(idx + 1, id.Length - idx - 1);
                app.SendOnAppLinkRequestReceived(new Uri(string.Format(uriFormat, App.Scheme, $"{App.Map}?{NavigationKeys.FuelIdQueryProperty}={fuelId}")));
            }

            IoC.Resolve<ILogger>().Event(Events.Action.AppAction);
        }
    }

    private static void MigrateVersionTracking()
    {
        const string VersionsKey = "VersionTracking.Versions";
        const string BuildsKey = "VersionTracking.Builds";

        const string XamarinSharedGroupFmt = "{0}.xamarinessentials.versiontracking";
        const string MauiSharedGroupFmt = "{0}.microsoft.maui.essentials.versiontracking";

        var xamGroupName = string.Format(XamarinSharedGroupFmt, AppInfo.PackageName);
        var mauiGroupName = string.Format(MauiSharedGroupFmt, AppInfo.PackageName);

        migrate(VersionsKey, xamGroupName, mauiGroupName);
        migrate(BuildsKey, xamGroupName, mauiGroupName);

        static bool migrate(string key, string oldSharedGroup, string newSharedGroup)
        {
            if (Preferences.ContainsKey(key, oldSharedGroup))
            {
                var data = Preferences.Get(key, null, oldSharedGroup);
                Preferences.Set(key, data, newSharedGroup);
                Preferences.Remove(key, oldSharedGroup);
                return true;
            }

            return false;
        }
    }

#if ANDROID
    private static async Task AndroidSecureStorageWorkaroundAsync()
    {
        try
        {
            await SecureStorage.GetAsync("key").ConfigureAwait(false);
        }
        catch (Exception)
        {
            var alias = $"{AppInfo.Current.PackageName}.microsoft.maui.essentials.preferences";
            var preferences = Platform.AppContext.GetSharedPreferences(alias, Android.Content.FileCreationMode.Private);
            preferences?.Edit()?.Clear()?.Commit();
        }
    }
#endif
}