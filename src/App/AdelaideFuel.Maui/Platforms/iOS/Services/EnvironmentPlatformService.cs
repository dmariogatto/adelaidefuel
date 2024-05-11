using UIKit;

namespace AdelaideFuel.Services
{
    public class EnvironmentPlatformService : IEnvironmentService
    {
        private readonly ILogger _logger;

        public EnvironmentPlatformService(ILogger logger)
        {
            _logger = logger;
        }

        // manual dark mode for the moment
        public bool NativeDarkMode => UIDevice.CurrentDevice.CheckSystemVersion(13, 0);

        public Theme GetOperatingSystemTheme()
        {
            var theme = Theme.Light;

            // 'TraitCollection.UserInterfaceStyle' was introduced in iOS 12.0
            if (UIDevice.CurrentDevice.CheckSystemVersion(12, 0))
            {
                try
                {
#pragma warning disable CA1416 // Validate platform compatibility
                    var userInterfaceStyle = UIScreen.MainScreen.TraitCollection.UserInterfaceStyle;
                    theme = userInterfaceStyle switch
                    {
                        UIUserInterfaceStyle.Light => Theme.Light,
                        UIUserInterfaceStyle.Dark => Theme.Dark,
                        _ => throw new NotSupportedException($"UIUserInterfaceStyle {userInterfaceStyle} not supported"),
                    };
#pragma warning restore CA1416 // Validate platform compatibility
                }
                catch (Exception ex)
                {
                    _logger.Error(ex);
                }
            }

            return theme;
        }
    }
}