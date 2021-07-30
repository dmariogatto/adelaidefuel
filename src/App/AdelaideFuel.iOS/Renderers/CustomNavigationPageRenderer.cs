using AdelaideFuel.iOS.Renderers;
using AdelaideFuel.Services;
using AdelaideFuel.UI;
using Foundation;
using UIKit;
using Xamarin.Forms;
using Xamarin.Forms.Platform.iOS;

[assembly: ExportRenderer(typeof(NavigationPage), typeof(CustomNavigationPageRenderer))]
namespace AdelaideFuel.iOS.Renderers
{
    [Preserve(AllMembers = true)]
    public class CustomNavigationPageRenderer : NavigationRenderer
    {
        public bool ViewAdded = false;

        public override void ViewWillAppear(bool animated)
        {
            base.ViewWillAppear(animated);

            // for some reason XForms forces DarkContent on iOS 13
            UIApplication.SharedApplication.StatusBarStyle = UIStatusBarStyle.Default;
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            UpdateStyle();
            ThemeManager.CurrentThemeChanged += ThemeChanged;

            NavigationBar.ShadowImage = new UIImage();
        }

        public override void ViewDidUnload()
        {
            base.ViewDidUnload();

            ThemeManager.CurrentThemeChanged -= ThemeChanged;
        }

        public override void TraitCollectionDidChange(UITraitCollection previousTraitCollection)
        {
            base.TraitCollectionDidChange(previousTraitCollection);

            TraitCollection?.UpdateTheme(previousTraitCollection);
        }

        private void ThemeChanged(object sender, ThemeEventArgs e)
        {
            UpdateStyle();
        }

        private void UpdateStyle()
        {
            if (UIDevice.CurrentDevice.CheckSystemVersion(13, 0))
            {
                OverrideUserInterfaceStyle = ThemeManager.CurrentTheme switch
                {
                    Theme.Light => UIUserInterfaceStyle.Light,
                    Theme.Dark => UIUserInterfaceStyle.Dark,
                    _ => UIUserInterfaceStyle.Unspecified
                };
            }

            StatusBarStyle.SetTheme(ThemeManager.CurrentTheme);
        }
    }
}