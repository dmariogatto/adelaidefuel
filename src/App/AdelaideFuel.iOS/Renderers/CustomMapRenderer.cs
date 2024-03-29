﻿using AdelaideFuel.iOS.Renderers;
using AdelaideFuel.UI.Controls;
using CoreGraphics;
using MapKit;
using UIKit;
using Xamarin.Forms;
using Xamarin.Forms.BetterMaps.iOS;
using Xamarin.Forms.Internals;

[assembly: ExportRenderer(typeof(FuelMap), typeof(CustomMapRenderer))]
namespace AdelaideFuel.iOS.Renderers
{
    [Preserve(AllMembers = true)]
    public class CustomMapRenderer : MapRenderer
    {
        protected override void UpdateShowUserLocationButton()
        {
            if (MapModel.ShowUserLocationButton && UserTrackingButton is null)
            {
                const float utSize = 38f;

                UserTrackingButton = MKUserTrackingButton.FromMapView(MapNative);
                UserTrackingButton.Layer.CornerRadius = 4f;
                UserTrackingButton.Layer.BorderWidth = 0.25f;
                UpdateUserTrackingButtonTheme();

                var circleMask = new CoreAnimation.CAShapeLayer();
                var circlePath = UIBezierPath.FromRoundedRect(new CGRect(0, 0, utSize, utSize), utSize / 2);
                circleMask.Path = circlePath.CGPath;
                UserTrackingButton.Layer.Mask = circleMask;

                UserTrackingButton.TranslatesAutoresizingMaskIntoConstraints = false;

                MapNative.AddSubview(UserTrackingButton);

                var margins = MapNative.LayoutMarginsGuide;
                NSLayoutConstraint.ActivateConstraints(new[]
                {
                    UserTrackingButton.TopAnchor.ConstraintEqualTo(margins.TopAnchor, 60),
                    UserTrackingButton.TrailingAnchor.ConstraintEqualTo(margins.TrailingAnchor, -8),
                    UserTrackingButton.WidthAnchor.ConstraintEqualTo(utSize),
                    UserTrackingButton.HeightAnchor.ConstraintEqualTo(UserTrackingButton.WidthAnchor),
                });
            }
            else if (!MapModel.ShowUserLocationButton && UserTrackingButton is not null)
            {
                UserTrackingButton.RemoveFromSuperview();
                UserTrackingButton.Dispose();
                UserTrackingButton = null;
            }
        }
    }
}
