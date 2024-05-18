using Acr.UserDialogs;
using AdelaideFuel.Maui.Controls;
using AdelaideFuel.Maui.Effects;
using AdelaideFuel.Maui.Extensions;
using AdelaideFuel.Maui.Handlers;
using AdelaideFuel.Maui.Services;
using AdelaideFuel.Models;
using AdelaideFuel.Services;
using AiForms.Settings;
using BetterMaps.Maui;
using BetterMaps.Maui.Handlers;
using Cats.Maui.AdMob;
using FFImageLoading.Maui;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Handlers;
using Sharpnado.CollectionView;
using ILogger = AdelaideFuel.Services.ILogger;
using IMap = BetterMaps.Maui.IMap;

namespace AdelaideFuel.Maui;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        AppActions.OnAppAction += OnAppAction;
        VersionTracking.Default.Migrate();
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
                options.EnableTracing = false;
                options.TracesSampleRate = 0.25;
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
}