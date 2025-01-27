using Android.Content.Res;
using Android.OS;

namespace AdelaideFuel.Services
{
    public class EnvironmentPlatformService : IEnvironmentService
    {
        private readonly ILogger _logger;

        public EnvironmentPlatformService(
            ILogger logger)
        {
            _logger = logger;
        }

        // manual dark mode for the moment
        public bool NativeDarkMode => (int)Build.VERSION.SdkInt >= 29; // Q

        public Theme GetOperatingSystemTheme()
        {
            var uiModeFlags = Platform.AppContext.Resources.Configuration.UiMode & UiMode.NightMask;
            return uiModeFlags switch
            {
                UiMode.NightYes => Theme.Dark,
                UiMode.NightNo => Theme.Light,
                _ => throw new NotSupportedException($"UiMode {uiModeFlags} not supported"),
            };
        }
    }
}