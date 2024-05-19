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
            const float utSize = 48f;

            if (handler?.PlatformView is not MauiMapView mapView)
                return;
            if (mapView.UserTrackingButton is null)
                return;

            var showUserLocationButton = map?.ShowUserLocationButton ?? false;

            if (!showUserLocationButton && mapView.UserTrackingButton.Superview is not null)
            {
                NSLayoutConstraint.DeactivateConstraints(getUserButtonConstraints(mapView));
                mapView.UserTrackingButton.RemoveFromSuperview();
                return;
            }

            if (showUserLocationButton && mapView.UserTrackingButton.Superview is null)
            {
                if (mapView.UserTrackingButton.Layer.Mask is null)
                {
                    mapView.UserTrackingButton.Layer.CornerRadius = utSize / 2;
                    mapView.UserTrackingButton.Layer.BorderWidth = 0.25f;

                    var circleMask = new CoreAnimation.CAShapeLayer();
                    var circlePath = UIBezierPath.FromRoundedRect(new CGRect(0, 0, utSize, utSize), utSize / 2);
                    circleMask.Path = circlePath.CGPath;
                    mapView.UserTrackingButton.Layer.Mask = circleMask;
                }

                mapView.AddSubview(mapView.UserTrackingButton);
                mapView.UserTrackingButton.TranslatesAutoresizingMaskIntoConstraints = false;

                NSLayoutConstraint.ActivateConstraints(getUserButtonConstraints(mapView));
            }

            static NSLayoutConstraint[] getUserButtonConstraints(MauiMapView map)
            {
                var margins = map.LayoutMarginsGuide;
                var insets = WindowStateManager.Default.GetCurrentUIWindow().SafeAreaInsets;
                return new[]
                {
                    map.UserTrackingButton.TopAnchor.ConstraintEqualTo(margins.TopAnchor, insets.Top + 40),
                    map.UserTrackingButton.TrailingAnchor.ConstraintEqualTo(margins.TrailingAnchor, -8),
                    map.UserTrackingButton.WidthAnchor.ConstraintEqualTo(utSize),
                    map.UserTrackingButton.HeightAnchor.ConstraintEqualTo(map.UserTrackingButton.WidthAnchor),
                };
            }
        }
    }
}