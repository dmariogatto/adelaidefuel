using AdelaideFuel.Services;
using AdelaideFuel.ViewModels;

namespace AdelaideFuel.Maui.Views
{
    public partial class SettingsPage : BasePage<SettingsViewModel>
    {
        public SettingsPage() : base()
        {
            InitializeComponent();

            if (DeviceInfo.Current.Platform == DevicePlatform.iOS && !IoC.Resolve<IEnvironmentService>().NativeDarkMode)
            {
                // hide on iOS 12 and below
                AccessibilitySection.Remove(ThemePicker);
            }
        }
    }
}