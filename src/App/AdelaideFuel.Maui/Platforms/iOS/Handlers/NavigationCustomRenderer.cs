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

            TopViewController.NavigationItem.BackButtonTitle =
                Localisation.Resources.Back;
        }
    }
}