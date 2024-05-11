using Android.Content.Res;
using Android.OS;

namespace AdelaideFuel.Services
{
    public class EnvironmentPlatformService : IEnvironmentService
    {
        private readonly IStoreFactory _storeFactory;
        private readonly ILogger _logger;

        public EnvironmentPlatformService(
            IStoreFactory storeFactory,
            ILogger logger)
        {
            _storeFactory = storeFactory;
            _logger = logger;
        }

        // manual dark mode for the moment
        public bool NativeDarkMode => (int)Build.VERSION.SdkInt >= 29; // Q

        public Theme GetOperatingSystemTheme()
        {
            var uiModeFlags = Platform.AppContext.Resources.Configuration.UiMode & UiMode.NightMask;
            switch (uiModeFlags)
            {
                case UiMode.NightYes:
                    return Theme.Dark;
                case UiMode.NightNo:
                    return Theme.Light;
                default:
                    throw new NotSupportedException($"UiMode {uiModeFlags} not supported");
            }
        }
    }
}