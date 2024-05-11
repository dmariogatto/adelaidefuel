using Microsoft.Maui.Controls.Platform;
using Microsoft.Maui.Platform;
using UIKit;

namespace AdelaideFuel.Maui.Effects
{
    public class SearchBarIconPlatformEffect : PlatformEffect
    {
        private UIView _leftView;

        protected override void OnAttached()
        {
            var color = SearchBarIconEffect.GetTintColor(Element);

            if (UIDevice.CurrentDevice.CheckSystemVersion(13, 0) && Element is not null && Control is UISearchBar searchBar)
            {
#pragma warning disable CA1416 // Validate platform compatibility
                _leftView = searchBar.SearchTextField.LeftView;
#pragma warning restore CA1416 // Validate platform compatibility
                SetTintColor(color);
            }
        }

        protected override void OnDetached()
        {
            SetTintColor(null);
            _leftView = null;
        }

        private void SetTintColor(Color tint)
        {
            if (_leftView is null)
                return;

            try
            {
                if (tint is null)
                    _leftView.TintColor = null;
                else
                    _leftView.TintColor = tint.ToPlatform();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
            }
        }
    }
}