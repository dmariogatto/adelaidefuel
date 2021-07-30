using AdelaideFuel.Services;
using UIKit;

namespace AdelaideFuel.iOS
{
    public static class StatusBarStyle
    {
        public static void SetTheme(Theme theme)
        {
            var uiStyle = theme switch
            {
                Theme.Light => UIStatusBarStyle.DarkContent,
                Theme.Dark => UIStatusBarStyle.LightContent,
                _ => UIStatusBarStyle.Default
            };

            UIApplication.SharedApplication.SetStatusBarStyle(uiStyle, false);
            UpdateStatusBarAppearance();
        }

        private static void UpdateStatusBarAppearance()
        {
            if (UIDevice.CurrentDevice.CheckSystemVersion(13, 0))
            {
                foreach (var window in UIApplication.SharedApplication.Windows)
                {
                    UpdateStatusBarAppearance(window);
                }
            }
            else
            {
                var window = UIApplication.SharedApplication.KeyWindow;
                UpdateStatusBarAppearance(window);
            }
        }

        private static void UpdateStatusBarAppearance(UIWindow window)
        {
            if (window == null)
                return;

            var vc = window.RootViewController;
            while (vc.PresentedViewController != null)
            {
                vc = vc.PresentedViewController;
            }

            vc?.SetNeedsStatusBarAppearanceUpdate();
        }
    }
}
