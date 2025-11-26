using Android.Content.Res;
using Android.Gms.Maps;
using Android.OS;
using Android.Views;
using Android.Widget;
using BetterMaps.Maui.Handlers;
using static Android.Views.ViewGroup;
using AView = Android.Views.View;
using IMap = BetterMaps.Maui.IMap;
using RL = Android.Widget.RelativeLayout;

namespace AdelaideFuel.Maui.Handlers
{
    public static class MapCustomHandler
    {
        public static void MapViewAttachedToWindow(IMapHandler handler, IMap map, object arg)
        {            
            if (arg is not MapView mapView)
                return;

            if (mapView.FindViewWithTag("GoogleMapMyLocationButton") is not AView myLocation)
                return;
            if (mapView.FindViewWithTag("GoogleMapCompass") is not AView compass)
                return;
                        
            var topInset = 0;

            if (Build.VERSION.SdkInt >= BuildVersionCodes.R)
            {
                var insets = Platform.CurrentActivity?.Window?.DecorView?.RootWindowInsets;
                var systemBars = insets?.GetInsets(WindowInsets.Type.SystemBars());
                topInset = systemBars?.Top ?? 0;
            }

            var rlp = new RL.LayoutParams(LayoutParams.WrapContent, LayoutParams.WrapContent);
            rlp.AddRule(LayoutRules.Below, myLocation.Id);
            rlp.AddRule(LayoutRules.AlignParentRight);

            var topMargin = (int)(14 * Resources.System.DisplayMetrics.Density) + topInset;
            var rightMargin = (int)(10 * Resources.System.DisplayMetrics.Density);
            rlp.SetMargins(0, topMargin, rightMargin, 0);

            compass.LayoutParameters = rlp;
        }
    }
}