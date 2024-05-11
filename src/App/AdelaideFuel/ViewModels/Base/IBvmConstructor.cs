using Acr.UserDialogs;
using AdelaideFuel.Services;
using Microsoft.Maui.ApplicationModel;

namespace AdelaideFuel.ViewModels
{
    public interface IBvmConstructor
    {
        IFuelService FuelService { get; }
        INavigationService NavigationService { get; }
        IAppPreferences AppPrefs { get; }
        IUserDialogs UserDialogs { get; }
        ILogger Logger { get; }

        IThemeService ThemeService { get; }

        IBrowser Browser { get; }
    }
}