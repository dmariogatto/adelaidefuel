using Android.Widget;
using Microsoft.Maui.Controls.Platform;
using Microsoft.Maui.Platform;

namespace AdelaideFuel.Maui.Effects
{
    public class SearchBarIconPlatformEffect : PlatformEffect
    {
        private ImageView _searchIconImageView;

        public SearchBarIconPlatformEffect()
        {
        }

        protected override void OnAttached()
        {
            if (Element is null)
                return;
            if (Control?.FindViewById(Resource.Id.search_mag_icon) is not ImageView iv)
                return;

            _searchIconImageView = iv;
            SetTintColor(SearchBarIconEffect.GetTintColor(Element));
        }

        protected override void OnDetached()
        {
            SetTintColor(null);
            _searchIconImageView = null;
        }

        private void SetTintColor(Color tint)
        {
            if (_searchIconImageView is null)
                return;

            try
            {
                if (tint is null)
                    _searchIconImageView.ClearColorFilter();
                else
                    _searchIconImageView.SetColorFilter(tint.ToPlatform());
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
            }
        }
    }
}