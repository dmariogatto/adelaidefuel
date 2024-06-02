using Acr.UserDialogs;
using AdelaideFuel.Maui.Controls;
using AdelaideFuel.Maui.Effects;
using AdelaideFuel.Maui.Handlers;
using AdelaideFuel.Maui.Services;
using AdelaideFuel.Models;
using AdelaideFuel.Services;
using AiForms.Settings;
using BetterMaps.Maui;
using BetterMaps.Maui.Handlers;
using Cats.Maui.AdMob;
using FFImageLoading.Maui;
using MemoryToolkit.Maui;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Handlers;
using Sharpnado.CollectionView;
using System.Net;
using ILogger = AdelaideFuel.Services.ILogger;
using IMap = BetterMaps.Maui.IMap;

namespace AdelaideFuel.Maui;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        MigrateVersionTracking();

        AppActions.OnAppAction += OnAppAction;
        VersionTracking.Track();

        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseSentry(options =>
            {
                options.Dsn = Constants.SentryDsn;
#if DEBUG
                options.Debug = false;
#endif
                options.AutoSessionTracking = true;
                options.IsGlobalModeEnabled = false;
                options.TracesSampleRate = 0.1;

                options.SetBeforeSend((sentryEvent, hint) =>
                {
                    if (sentryEvent.Exception is not null && !ShouldLogException(sentryEvent.Exception))
                    {
                        return null;
                    }
                    return sentryEvent;
                });
                options.DisableUnobservedTaskExceptionCapture();
            })
            .ConfigureMauiHandlers(handlers =>
            {
                handlers.AddSettingsViewHandler();

                ImageHandler.Mapper.AppendToMapping(nameof(TintImage.TintColor), TintImageHandler.MapTintColor);
#if IOS || MACCATALYST
                MapHandler.Mapper.ModifyMapping(nameof(IMap.ShowUserLocationButton), (h, e, _) => MapCustomHandler.MapShowUserLocationButton(h, e));
                handlers.AddHandler(typeof(ContentPage), typeof(PageCustomHandler));
#elif ANDROID
                MapHandler.CommandMapper.AppendToMapping(nameof(Android.Gms.Maps.MapView.ViewAttachedToWindow), MapCustomHandler.MapViewAttachedToWindow);
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
            .UseFFImageLoading()
            .UseSharpnadoCollectionView(false);

#if DEBUG
        // Configure logging
        builder.Logging.AddDebug();
        // Ensure UseLeakDetection is called after logging has been configured!
        builder.UseLeakDetection();
#endif

        builder.Services.AddSingleton<IMapCache, MapCache>();
#if ANDROID
        builder.UseMauiMaps(
            lightThemeAsset: "map.style.light.json",
            darkThemeAsset: "map.style.dark.json",
            renderer: BetterMaps.Maui.Android.GoogleMapsRenderer.Legacy);
#else
        builder.UseMauiMaps();
#endif

        builder.UseMauiAdMob();
        builder.Services.AddDefaultAdConsentService();

        builder.Services.ConfigureIoC();

        builder.Services.AddSingleton<IUserDialogs>(_ => UserDialogs.Instance);
        builder.Services.AddSingleton<ILogger, Logger>();
        builder.Services.AddSingleton<ILocalise, LocalisePlatformService>();
        builder.Services.AddSingleton<IEnvironmentService, EnvironmentPlatformService>();
        builder.Services.AddSingleton<IRetryPolicyService, RetryPolicyPlatformService>();
        builder.Services.AddSingleton<INavigationService, TabbedNavigationService>();
        builder.Services.AddSingleton<IThemeService, ThemeService>();

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

    private static bool ShouldLogException(Exception ex)
    {
        switch (ex)
        {
            case TaskCanceledException _:
            case TimeoutException _:
            case OperationCanceledException _:
            case HttpRequestException httpRequstEx
                    when httpRequstEx.Message.Contains("No such host is known") ||
                         httpRequstEx.Message.Contains("The network connection was lost.") ||
                         httpRequstEx.Message.Contains("Network subsystem is down") ||
                         httpRequstEx.Message.Contains("A server with the specified hostname could not be found.") ||
                         httpRequstEx.Message.Contains("The Internet connection appears to be offline.") ||
                         httpRequstEx.Message.Contains("Could not connect to the server."):
            case WebException webEx
                    when webEx.Message.Contains("Canceled") ||
                         webEx.Message.Contains("Socket closed"):
            case IOException ioEx
                    when ioEx.Message.Contains("Network subsystem is down"):
                return false;
            default:
                return true;
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
}