using BetterMaps.Maui.Handlers;
using BetterMaps.Maui.iOS;
using CoreGraphics;
using UIKit;
using IMap = BetterMaps.Maui.IMap;

namespace AdelaideFuel.Maui.Handlers
{
    public static class MapCustomHandler
    {
        public static void MapShowUserLocationButton(IMapHandler handler, IMap map)
        {
            if (handler?.PlatformView is not MauiMapView mapView)
                return;
            if (mapView.UserTrackingButton is null)
                return;

            var showUserLocationButton = map?.ShowUserLocationButton ?? false;
            if (!showUserLocationButton && mapView.UserTrackingButton.Superview is not null)
            {
                mapView.UserTrackingButton.RemoveFromSuperview();
                return;
            }

            if (showUserLocationButton && mapView.UserTrackingButton.Superview is null)
            {
                const float utSize = 38f;

                var circleMask = new CoreAnimation.CAShapeLayer();
                var circlePath = UIBezierPath.FromRoundedRect(new CGRect(0, 0, utSize, utSize), utSize / 2);
                circleMask.Path = circlePath.CGPath;
                mapView.UserTrackingButton.Layer.Mask = circleMask;

                mapView.UserTrackingButton.TranslatesAutoresizingMaskIntoConstraints = false;

                mapView.AddSubview(mapView.UserTrackingButton);

                var margins = mapView.LayoutMarginsGuide;
                var insets = WindowStateManager.Default.GetCurrentUIWindow().SafeAreaInsets;

                NSLayoutConstraint.ActivateConstraints(new[]
                {
                    mapView.UserTrackingButton.TopAnchor.ConstraintEqualTo(margins.TopAnchor, insets.Top + 40),
                    mapView.UserTrackingButton.TrailingAnchor.ConstraintEqualTo(margins.TrailingAnchor, -8),
                    mapView.UserTrackingButton.WidthAnchor.ConstraintEqualTo(utSize),
                    mapView.UserTrackingButton.HeightAnchor.ConstraintEqualTo(mapView.UserTrackingButton.WidthAnchor),
                });
            }
        }
    }
}
