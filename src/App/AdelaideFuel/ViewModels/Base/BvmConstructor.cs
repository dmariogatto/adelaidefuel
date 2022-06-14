using Acr.UserDialogs;
using AdelaideFuel.Services;
using System;
using Xamarin.Essentials.Interfaces;

namespace AdelaideFuel.ViewModels
{
    public class BvmConstructor : IBvmConstructor
    {
        public IFuelService FuelService { get; }
        public INavigationService NavigationService { get; }
        public IAppPreferences AppPrefs { get; }
        public IUserDialogs UserDialogs { get; }
        public ILogger Logger { get; }

        public IThemeService ThemeService { get; }

        public IBrowser Browser { get; }

        public BvmConstructor(
            IFuelService fuelService,
            INavigationService navigationService,
            IAppPreferences appPrefs,
            IUserDialogs userDialogs,
            ILogger log,
            IThemeService themeService,
            IBrowser browser) : base()
        {
            FuelService = fuelService;
            NavigationService = navigationService;
            AppPrefs = appPrefs;
            UserDialogs = userDialogs;
            Logger = log;

            ThemeService = themeService;

            Browser = browser;
        }
    }
}