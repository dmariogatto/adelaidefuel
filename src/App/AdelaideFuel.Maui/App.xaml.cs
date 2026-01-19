using AdelaideFuel.Maui.Services;
using AdelaideFuel.Services;
using AdelaideFuel.ViewModels;
using Cats.Maui.AdMob;
using Polly;
using System.Web;

namespace AdelaideFuel.Maui;

public partial class App : Application
{
    public const string Scheme = "adl-sif";
    public const string Map = "map";

    private readonly WeakEventManager _weakEventManager = new WeakEventManager();

    private readonly SemaphoreSlim _umpSemaphore = new SemaphoreSlim(1, 1);

    public App()
    {
        InitializeComponent();

        ThemeManager.LoadTheme();
        RequestedThemeChanged += (_, _) => ThemeManager.OsThemeChanged();
    }

    public event EventHandler<EventArgs> Resumed
    {
        add => _weakEventManager.AddEventHandler(value);
        remove => _weakEventManager.RemoveEventHandler(value);
    }

    protected override Window CreateWindow(IActivationState activationState)
    {
        var mainPage = IoC.Resolve<INavigationService>().GetMainPage();

        Dispatcher.DispatchAsync(async () =>
        {
            await Task.Delay(2000);
            await AppReviewRequestAsync();
        });

        return new Window(mainPage);
    }

    protected override void OnStart()
    {
        base.OnStart();

        var prefService = IoC.Resolve<IAppPreferences>();
        var lastDateOpened = prefService.LastDateOpened;

        UpdateDayCount();

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
                Dispatcher.Dispatch(async () =>
                {
                    var goToSubscriptionPage = await IoC.Resolve<IDialogService>().ConfirmAsync(
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

        Dispatcher.DispatchAsync(RequestAdConsentAsync);
    }

    protected override void OnSleep()
    {
        base.OnSleep();
    }

    protected override void OnResume()
    {
        base.OnResume();

        UpdateDayCount();

        Dispatcher.DispatchAsync(RequestAdConsentAsync);

        _weakEventManager.HandleEvent(this, EventArgs.Empty, nameof(Resumed));
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
                await IoC.Resolve<IStoreReview>().RequestReviewAsync(testMode);
                appPrefs.ReviewRequested = true;
                IoC.Resolve<ILogger>().Event(Events.Action.ReviewRequested, new Dictionary<string, string>(1)
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

    private async Task RequestAdConsentAsync()
    {
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
                    Events.Action.AdConsent, new Dictionary<string, string>()
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