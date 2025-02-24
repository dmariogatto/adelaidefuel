using AdelaideFuel.Services;
using Microsoft.Maui.ApplicationModel;

namespace AdelaideFuel.ViewModels
{
    public class BvmConstructor : IBvmConstructor
    {
        public IFuelService FuelService { get; }
        public INavigationService NavigationService { get; }
        public IAppPreferences AppPrefs { get; }
        public IDialogService DialogService { get; }
        public ILogger Logger { get; }

        public IThemeService ThemeService { get; }

        public IBrowser Browser { get; }

        public BvmConstructor(
            IFuelService fuelService,
            INavigationService navigationService,
            IAppPreferences appPrefs,
            IDialogService dialogService,
            ILogger log,
            IThemeService themeService,
            IBrowser browser) : base()
        {
            FuelService = fuelService;
            NavigationService = navigationService;
            AppPrefs = appPrefs;
            DialogService = dialogService;
            Logger = log;

            ThemeService = themeService;

            Browser = browser;
        }
    }
}