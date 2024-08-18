using CoreGraphics;
using Microsoft.Maui.Controls.Handlers.Compatibility;
using UIKit;

namespace AdelaideFuel.Maui.Handlers
{
    public class NavigationCustomRenderer : NavigationRenderer
    {
        public NavigationCustomRenderer() : base()
        {
            if (DeviceInfo.Version.Major > 12)
                throw new InvalidOperationException("Cannot be used on iOS 13 or higher!");
        }

        public override void ViewWillAppear(bool animated)
        {
            base.ViewWillAppear(animated);
            AddCustomBackButton();
        }

        public override void PushViewController(UIViewController viewController, bool animated)
        {
            base.PushViewController(viewController, animated);
            AddCustomBackButton();
        }

        protected override Task<bool> OnPopViewAsync(Page page, bool animated)
        {
            UnregisterBackButton(TopViewController);

            return base.OnPopViewAsync(page, animated);
        }

        protected override Task<bool> OnPopToRoot(Page page, bool animated)
        {
            foreach (var vc in ViewControllers.Skip(1))
                UnregisterBackButton(vc);

            return base.OnPopToRoot(page, animated);
        }

        private void AddCustomBackButton()
        {
            if (ViewControllers is null || ViewControllers.Length <= 1)
                return;
            if (TopViewController?.NavigationItem is null)
                return;
            if (TopViewController.NavigationItem.LeftBarButtonItem is not null)
                return;

            var backButton = new UIButton(UIButtonType.System);
            backButton.SetImage(GetBackImage(), UIControlState.Normal);
            backButton.SetTitle("Back", UIControlState.Normal);
            backButton.TintColor = UIColor.FromRGB(0, 122, 255);
            backButton.TouchUpInside += OnCustomBackButtonPressed;

            TopViewController.NavigationItem.LeftBarButtonItem = new UIBarButtonItem(customView: backButton);
        }

        private void UnregisterBackButton(UIViewController viewController)
        {
            if (viewController.NavigationItem?.LeftBarButtonItem is not UIBarButtonItem buttonItem)
                return;
            if (buttonItem.CustomView is not UIButton button)
                return;

            button.TouchUpInside -= OnCustomBackButtonPressed;
        }

        private void OnCustomBackButtonPressed(object sender, EventArgs e)
        {
            if (Element is NavigationPage navPage && navPage.CurrentPage?.SendBackButtonPressed() is false)
            {
                _ = navPage.PopAsync();
            }
        }

        private static UIImage GetBackImage()
        {
            var originalImage = new UIImage("next.png").GetImageWithHorizontallyFlippedOrientation();
            var pixels = (int)(originalImage.Size.Width * 0.325);

            var cropRect = new CGRect(pixels, 0, originalImage.Size.Width - pixels, originalImage.Size.Height);
#pragma warning disable CA1416 // Validate platform compatibility
            UIGraphics.BeginImageContextWithOptions(cropRect.Size, false, originalImage.CurrentScale);
            originalImage.Draw(new CGPoint(-pixels, 0));
            var croppedImage = UIGraphics.GetImageFromCurrentImageContext();
            UIGraphics.EndImageContext();
#pragma warning restore CA1416 // Validate platform compatibility

            return croppedImage;
        }
    }
}