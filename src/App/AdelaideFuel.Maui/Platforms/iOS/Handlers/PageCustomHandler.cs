using UIKit;

namespace AdelaideFuel.Maui.Handlers
{
    public class PageCustomHandler : Microsoft.Maui.Handlers.PageHandler
    {
        protected override void ConnectHandler(Microsoft.Maui.Platform.ContentView nativeView)
        {
            base.ConnectHandler(nativeView);

            if (VirtualView is ContentPage page)
                page.Loaded += OnLoaded;
        }

        protected override void DisconnectHandler(Microsoft.Maui.Platform.ContentView nativeView)
        {
            if (VirtualView is ContentPage page)
                page.Loaded -= OnLoaded;

            base.DisconnectHandler(nativeView);
        }

        private void OnLoaded(object sender, EventArgs e)
        {
            if (ViewController?.ParentViewController?.NavigationController?.NavigationBar is UINavigationBar navBar)
            {
                //navBar.ClipsToBounds = true;
            }
        }
    }
}