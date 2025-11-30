using Microsoft.Maui.Controls.Handlers.Compatibility;
using UIKit;

namespace AdelaideFuel.Maui.Handlers
{
    public class NavigationCustomRenderer : NavigationRenderer
    {
        public NavigationCustomRenderer() : base()
        {
        }

        public override void PushViewController(UIViewController viewController, bool animated)
        {
            base.PushViewController(viewController, animated);
            SetBackButtonTitle();
        }

        private void SetBackButtonTitle()
        {
            if (Element is not NavigationPage navPage)
                return;
            if (navPage.Navigation.NavigationStack.Count <= 1)
                return;
            if (navPage.Navigation.NavigationStack[^2] is not { } previousPage)
                return;

            TopViewController.NavigationItem.BackButtonTitle =
                !string.IsNullOrWhiteSpace(previousPage.Title)
                ? previousPage.Title
                : Localisation.Resources.Back;
        }
    }
}