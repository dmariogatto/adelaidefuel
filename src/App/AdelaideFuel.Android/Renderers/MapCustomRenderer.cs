using AdelaideFuel.Droid.Renderers;
using AdelaideFuel.UI.Controls;
using Android.Content;
using Android.Content.Res;
using Android.Widget;
using Xamarin.Forms;
using Xamarin.Forms.BetterMaps.Android;
using Xamarin.Forms.Internals;
using AView = Android.Views.View;
using RL = Android.Widget.RelativeLayout;

[assembly: ExportRenderer(typeof(FuelMap), typeof(MapCustomRenderer))]
namespace AdelaideFuel.Droid.Renderers
{
    [Preserve(AllMembers = true)]
    public class MapCustomRenderer : MapRenderer
    {
        public MapCustomRenderer(Context context) : base(context)
        {
        }

        protected override void OnAttachedToWindow()
        {
            base.OnAttachedToWindow();

            if (FindViewWithTag("GoogleMapMyLocationButton") is not AView myLocation)
                return;
            if (FindViewWithTag("GoogleMapCompass") is not AView compass)
                return;

            var rlp = new RL.LayoutParams(LayoutParams.WrapContent, LayoutParams.WrapContent);
            rlp.AddRule(LayoutRules.Below, myLocation.Id);
            rlp.AddRule(LayoutRules.AlignParentRight);

            var topMargin = (int)(14 * Resources.System.DisplayMetrics.Density);
            var rightMargin = (int)(10 * Resources.System.DisplayMetrics.Density);
            rlp.SetMargins(0, topMargin, rightMargin, 0);

            compass.LayoutParameters = rlp;
        }
    }
}
