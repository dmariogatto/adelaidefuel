using AdelaideFuel.Services;
using AdelaideFuel.UI.Attributes;
using AdelaideFuel.ViewModels;
using Xamarin.Forms;

namespace AdelaideFuel.UI.Views
{
    [NavigationRoute(NavigationRoutes.Settings, true)]
    public partial class SettingsPage : BasePage<SettingsViewModel>
    {
        public SettingsPage() : base()
        {
            InitializeComponent();

            if (Device.RuntimePlatform == Device.iOS && !IoC.Resolve<IEnvironmentService>().NativeDarkMode)
            {
                // hide on iOS 12 and below
                AccessibilitySection.Remove(ThemePicker);
            }
        }
    }
}