using Android.Content.Res;
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
        public static void MapOnMapLoaded(IMapHandler handler, IMap map, object arg)
        {
            if (handler.PlatformView is not ViewGroup viewGroup)
                return;

            if (viewGroup.FindViewWithTag("GoogleMapMyLocationButton") is not AView myLocation)
                return;
            if (viewGroup.FindViewWithTag("GoogleMapCompass") is not AView compass)
                return;

            var topInset = 0;

            if (Build.VERSION.SdkInt >= BuildVersionCodes.R)
            {
                var insets = Platform.CurrentActivity?.Window?.DecorView?.RootWindowInsets;
                var systemBars = insets?.GetInsets(WindowInsets.Type.SystemBars());
                topInset = systemBars?.Top ?? 0;
            }

            var locationTopMargin = (int)(72 * Resources.System.DisplayMetrics.Density);
            var locationRlp = myLocation.LayoutParameters as RL.LayoutParams;
            locationRlp?.SetMargins(locationRlp.LeftMargin, locationRlp.TopMargin + topInset + locationTopMargin, locationRlp.RightMargin, locationRlp.BottomMargin);

            var compassRlp = new RL.LayoutParams(LayoutParams.WrapContent, LayoutParams.WrapContent);
            compassRlp.AddRule(LayoutRules.Below, myLocation.Id);
            compassRlp.AddRule(LayoutRules.AlignParentRight);

            var compassTopMargin = (int)(14 * Resources.System.DisplayMetrics.Density);
            compassRlp.SetMargins(0, locationTopMargin, locationRlp.RightMargin, 0);

            compass.LayoutParameters = compassRlp;
        }
    }
}