using AdelaideFuel.iOS.Effects;
using AdelaideFuel.UI.Effects;
using Foundation;
using System;
using UIKit;
using Xamarin.Forms;
using Xamarin.Forms.Platform.iOS;

[assembly: ExportEffect(typeof(SearchBarIconEffect_iOS), nameof(SearchBarIconEffect))]
namespace AdelaideFuel.iOS.Effects
{
    [Preserve(AllMembers = true)]
    public class SearchBarIconEffect_iOS : PlatformEffect
    {
        private UIView _leftView;

        protected override void OnAttached()
        {
            var color = SearchBarIconEffect.GetTintColor(Element);

            if (Element is not null && Control is UISearchBar searchBar)
            {
                _leftView = searchBar.SearchTextField.LeftView;
                SetTintColor(color);
            }
        }

        protected override void OnDetached()
        {
            SetTintColor(Color.Default);
            _leftView = null;
        }

        private void SetTintColor(Color tint)
        {
            if (_leftView is null)
                return;

            try
            {
                if (tint == Color.Default)
                    _leftView.TintColor = null;
                else
                    _leftView.TintColor = tint.ToUIColor();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
            }
        }
    }
}