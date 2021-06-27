using AdelaideFuel.Droid.Renderers;
using AdelaideFuel.UI.Controls;
using Android.Content;
using Android.Content.Res;
using Xamarin.Forms;
using Xamarin.Forms.BetterMaps.Android;
using Xamarin.Forms.Internals;
using AndroidViews = Android.Views;
using AndroidWidget = Android.Widget;

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

            if (FindViewById(1)?.Parent is AndroidViews.View parent)
            {
                if (parent.FindViewById(2) is AndroidViews.View myLocation &&
                   parent.FindViewById(5) is AndroidViews.View compass)
                {
                    var rlp = new AndroidWidget.RelativeLayout.LayoutParams(LayoutParams.WrapContent, LayoutParams.WrapContent);
                    rlp.AddRule(AndroidWidget.LayoutRules.Below, myLocation.Id);
                    rlp.AddRule(AndroidWidget.LayoutRules.AlignParentRight);

                    var topMargin = (int)(14 * Resources.System.DisplayMetrics.Density);
                    var rightMargin = (int)(10 * Resources.System.DisplayMetrics.Density);
                    rlp.SetMargins(0, topMargin, rightMargin, 0);

                    compass.LayoutParameters = rlp;
                }
            }
        }
    }
}
