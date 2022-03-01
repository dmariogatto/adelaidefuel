using AdelaideFuel.Droid.Effects;
using AdelaideFuel.UI.Effects;
using Android.Runtime;
using Android.Widget;
using Xamarin.Forms;
using Xamarin.Forms.Platform.Android;

[assembly: ExportEffect(typeof(SearchBarIconEffect_Droid), nameof(SearchBarIconEffect))]
namespace AdelaideFuel.Droid.Effects
{
    [Preserve(AllMembers = true)]
    public class SearchBarIconEffect_Droid : PlatformEffect
    {
        private ImageView _searchIcon;

        public SearchBarIconEffect_Droid()
        {
        }

        protected override void OnAttached()
        {
            var color = SearchBarIconEffect.GetTintColor(Element);

            if (Element is not null && Control?.Context is not null)
            {
                _searchIcon = Control?.FindViewById(Control.Context.Resources.GetIdentifier("android:id/search_mag_icon", null, null)) as ImageView;
                SetTintColor(color);
            }
        }

        protected override void OnDetached()
        {
            SetTintColor(Color.Default);
            _searchIcon = null;
        }

        private void SetTintColor(Color tint)
        {
            if (_searchIcon is null)
                return;

            if (tint == Color.Default)
                _searchIcon.SetColorFilter(null);
            else
                _searchIcon.SetColorFilter(tint.ToAndroid());
        }
    }
}