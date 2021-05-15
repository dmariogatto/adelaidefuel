using CoreGraphics;
using CoreLocation;
using Foundation;
using MapKit;
using ObjCRuntime;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Linq;
using UIKit;
using Xamarin.Forms.Platform.iOS;
using RectangleF = CoreGraphics.CGRect;

namespace Xamarin.Forms.Maps.iOS
{
    [Preserve(AllMembers = true)]
    public class MapRenderer : ViewRenderer
    {
        private readonly Dictionary<IMKAnnotation, Pin> _pinLookup = new Dictionary<IMKAnnotation, Pin>();
        private readonly Dictionary<IMKOverlay, MapElement> _elementLookup = new Dictionary<IMKOverlay, MapElement>();

        private CLLocationManager _locationManager;
        private bool _shouldUpdateRegion;
        private bool _disposed;
        private bool _init = true;

        private UITapGestureRecognizer _mapClickedGestureRecognizer;
        private MKUserTrackingButton _userTrackingButton;

        protected MKMapView MapNative => Control as MKMapView;
        protected Map MapModel => Element as Map;

        protected bool IsDarkMode => FormsMaps.iOs13OrNewer && TraitCollection?.UserInterfaceStyle == UIUserInterfaceStyle.Dark;

        #region Overrides
        public override SizeRequest GetDesiredSize(double widthConstraint, double heightConstraint)
        {
            return Control.GetSizeRequest(widthConstraint, heightConstraint);
        }

        protected override void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;

            if (disposing)
            {
                if (MapModel != null)
                {
                    CleanUpMapModelElements(MapModel, MapNative);
                }

                if (MapNative != null)
                {
                    CleanUpNativeMap(MapNative);
                }
            }

            base.Dispose(disposing);
        }

        protected override void OnElementChanged(ElementChangedEventArgs<View> e)
        {
            base.OnElementChanged(e);

            var oldMapView = MapNative;

            SetNativeControl(new MKMapView(RectangleF.Empty));

            if (e.OldElement != null)
            {
                CleanUpMapModelElements((Map)e.OldElement, oldMapView);
            }

            if (oldMapView != null)
            {
                CleanUpNativeMap(oldMapView);
                oldMapView.Dispose();
                oldMapView = null;
            }

            if (e.NewElement != null)
            {
                var mapModel = (Map)e.NewElement;

                MapNative.GetViewForAnnotation = GetViewForAnnotation;
                MapNative.OverlayRenderer = GetViewForOverlay;
                MapNative.DidSelectAnnotationView += MkMapViewOnAnnotationViewSelected;
                MapNative.DidDeselectAnnotationView += MkMapViewOnAnnotationViewDeselected;
                MapNative.RegionChanged += MkMapViewOnRegionChanged;
                MapNative.AddGestureRecognizer(_mapClickedGestureRecognizer = new UITapGestureRecognizer(OnMapClicked));

                MessagingCenter.Subscribe<Map, MapSpan>(this, Map.MoveToRegionMessageName, (s, a) => MoveToRegion(a), mapModel);

                UpdateTrafficEnabled();
                UpdateMapTheme();
                UpdateMapType();
                UpdateShowUserLocation();
                UpdateShowUserLocationButton();
                UpdateShowCompass();
                UpdateHasScrollEnabled();
                UpdateHasZoomEnabled();

                mapModel.Pins.CollectionChanged += OnPinCollectionChanged;
                OnPinCollectionChanged(mapModel.Pins, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));

                mapModel.MapElements.CollectionChanged += OnMapElementCollectionChanged;
                OnMapElementCollectionChanged(mapModel.MapElements, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));

                UpdateSelectedPin();
            }
        }

        public override void TraitCollectionDidChange(UITraitCollection previousTraitCollection)
        {
            base.TraitCollectionDidChange(previousTraitCollection);

            if (FormsMaps.iOs13OrNewer &&
                _userTrackingButton != null &&
                TraitCollection?.UserInterfaceStyle != previousTraitCollection?.UserInterfaceStyle)
            {
                UpdateUserTrackingButtonTheme();
            }
        }

        protected override void OnElementPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            base.OnElementPropertyChanged(sender, e);

            if (e.PropertyName == Map.MapThemeProperty.PropertyName)
                UpdateMapTheme();
            else if (e.PropertyName == Map.MapTypeProperty.PropertyName)
                UpdateMapType();
            else if (e.PropertyName == Map.ShowUserLocationProperty.PropertyName)
                UpdateShowUserLocation();
            else if (e.PropertyName == Map.ShowUserLocationButtonProperty.PropertyName)
                UpdateShowUserLocationButton();
            else if (e.PropertyName == Map.ShowCompassProperty.PropertyName)
                UpdateShowCompass();
            else if (e.PropertyName == Map.SelectedPinProperty.PropertyName)
                UpdateSelectedPin();
            else if (e.PropertyName == Map.HasScrollEnabledProperty.PropertyName)
                UpdateHasScrollEnabled();
            else if (e.PropertyName == Map.HasZoomEnabledProperty.PropertyName)
                UpdateHasZoomEnabled();
            else if (e.PropertyName == Map.TrafficEnabledProperty.PropertyName)
                UpdateTrafficEnabled();
            else if (e.PropertyName == VisualElement.HeightProperty.PropertyName && MapModel.LastMoveToRegion != null)
                _shouldUpdateRegion = MapModel.MoveToLastRegionOnLayoutChange;
        }

        public override void LayoutSubviews()
        {
            base.LayoutSubviews();

            // need to have frame define for this to work
            if (_init && MapNative.Frame.Height > 1)
            {
                // initial region
                _init = false;
                if (MapModel.LastMoveToRegion != null)
                    MoveToRegion(MapModel.LastMoveToRegion, false);
            }

            UpdateRegion();
        }
        #endregion

        #region Annotations
        protected virtual IMKAnnotation CreateAnnotation(Pin pin)
        {
            return new FormsMKPointAnnotation
            {
                TintColor = pin.TintColor.ToUIColor(),
                Title = pin.Label,
                Subtitle = pin.Address ?? string.Empty,
                Coordinate = new CLLocationCoordinate2D(pin.Position.Latitude, pin.Position.Longitude),
                Anchor = new CGPoint((float)pin.Anchor.X, (float)pin.Anchor.Y),
                ZIndex = pin.ZIndex,
                FileImage = pin.FileImage
            };
        }

        protected virtual MKAnnotationView GetViewForAnnotation(MKMapView mapView, IMKAnnotation annotation)
        {
            var mapPin = default(MKAnnotationView);

            // https://bugzilla.xamarin.com/show_bug.cgi?id=26416
            var userLocationAnnotation = Runtime.GetNSObject(annotation.Handle) as MKUserLocation;
            if (userLocationAnnotation != null)
                return null;

            const string defaultPinAnnotationId = nameof(defaultPinAnnotationId);
            const string customImgAnnotationId = nameof(customImgAnnotationId);

            var fAnnotation = (FormsMKPointAnnotation)annotation;

            var pinImage = GetMarkerImage(fAnnotation.FileImage, fAnnotation.TintColor);
            if (pinImage != null)
            {
                mapPin = mapView.DequeueReusableAnnotation(customImgAnnotationId);

                if (mapPin == null)
                {
                    mapPin = new MKAnnotationView(annotation, customImgAnnotationId);
                    mapPin.CanShowCallout = true;
                }

                mapPin.Annotation = annotation;
                mapPin.Layer.AnchorPoint = fAnnotation.Anchor;
                mapPin.Image = pinImage;

                if (FormsMaps.iOs14OrNewer)
                    mapPin.ZPriority = fAnnotation.ZIndex;
            }
            else
            {
                mapPin = mapView.DequeueReusableAnnotation(defaultPinAnnotationId);

                if (mapPin == null)
                {
                    mapPin = new MKPinAnnotationView(annotation, defaultPinAnnotationId);
                    mapPin.CanShowCallout = true;
                }

                mapPin.Annotation = annotation;
                ((MKPinAnnotationView)mapPin).PinTintColor = fAnnotation.TintColor;

                if (FormsMaps.iOs14OrNewer)
                    mapPin.ZPriority = fAnnotation.ZIndex;
            }

            return mapPin;
        }

        private void MkMapViewOnAnnotationViewSelected(object sender, MKAnnotationViewEventArgs e)
        {
            var annotation = e.View.Annotation;
            var pin = GetPinForAnnotation(annotation);

            if (e.View.GestureRecognizers?.Length > 0)
                foreach (var r in e.View.GestureRecognizers.ToList())
                {
                    e.View.RemoveGestureRecognizer(r);
                    r.Dispose();
                }

            var calloutTapRecognizer = new UITapGestureRecognizer(g => OnCalloutClicked(annotation));
            var calloutLongRecognizer = new UILongPressGestureRecognizer(g =>
            {
                if (g.State == UIGestureRecognizerState.Began)
                    OnCalloutAltClicked(annotation);
            });

            e.View.AddGestureRecognizer(calloutTapRecognizer);
            e.View.AddGestureRecognizer(calloutLongRecognizer);

            if (pin != null)
            {
                if (!ReferenceEquals(pin, MapModel.SelectedPin))
                {
                    MapModel.SelectedPin = pin;
                }

                // SendMarkerClick() returns the value of PinClickedEventArgs.HideInfoWindow
                // Hide the info window by deselecting the annotation
                var deselect = MapModel.SendMarkerClick(pin);
                if (deselect) MapNative.DeselectAnnotation(annotation, false);
            }
        }

        private void MkMapViewOnAnnotationViewDeselected(object sender, MKAnnotationViewEventArgs e)
        {
            if (e.View.GestureRecognizers?.Length > 0)
                foreach (var r in e.View.GestureRecognizers.ToList())
                {
                    e.View.RemoveGestureRecognizer(r);
                    r.Dispose();
                }

            if (GetPinForAnnotation(e.View.Annotation) is Pin pin &&
                ReferenceEquals(MapModel.SelectedPin, pin))
            {
                MapModel.SelectedPin = null;
            }
        }

        private void OnCalloutClicked(IMKAnnotation annotation)
        {
            // lookup pin
            var targetPin = GetPinForAnnotation(annotation);

            // pin not found. Must have been activated outside of forms
            if (targetPin == null) return;

            // SendInfoWindowClick() returns the value of PinClickedEventArgs.HideInfoWindow
            // Hide the info window by deselecting the annotation
            var deselect = MapModel.SendInfoWindowClick(targetPin);
            if (deselect) MapNative.DeselectAnnotation(annotation, true);
        }

        private void OnCalloutAltClicked(IMKAnnotation annotation)
        {
            // lookup pin
            var targetPin = GetPinForAnnotation(annotation);

            // pin not found. Must have been activated outside of forms
            if (targetPin == null) return;

            var deselect = MapModel.SendInfoWindowLongClick(targetPin);
            if (deselect) MapNative.DeselectAnnotation(annotation, true);
        }
        #endregion

        #region Map
        private void OnMapClicked(UITapGestureRecognizer recognizer)
        {
            if (Element == null) return;

            var tapPoint = recognizer.LocationInView(Control);
            var tapGPS = MapNative.ConvertPoint(tapPoint, Control);
            MapModel.SendMapClicked(new Position(tapGPS.Latitude, tapGPS.Longitude));
        }

        private void UpdateRegion()
        {
            if (_shouldUpdateRegion)
            {
                MoveToRegion(MapModel.LastMoveToRegion, false);
                _shouldUpdateRegion = false;
            }
        }

        private void MkMapViewOnRegionChanged(object sender, MKMapViewChangeEventArgs e)
        {
            if (MapModel == null) return;

            var pos = new Position(MapNative.Region.Center.Latitude, MapNative.Region.Center.Longitude);
            MapModel.SetVisibleRegion(new MapSpan(pos, MapNative.Region.Span.LatitudeDelta, MapNative.Region.Span.LongitudeDelta, MapNative.Camera.Heading));
        }

        private void MoveToRegion(MapSpan mapSpan, bool animated = true)
            => MapNative.SetRegion(MapSpanToMKCoordinateRegion(mapSpan), animated);

        private MKCoordinateRegion MapSpanToMKCoordinateRegion(MapSpan mapSpan)
            => new MKCoordinateRegion(new CLLocationCoordinate2D(mapSpan.Center.Latitude, mapSpan.Center.Longitude), new MKCoordinateSpan(mapSpan.LatitudeDegrees, mapSpan.LongitudeDegrees));

        private void UpdateHasScrollEnabled()
        {
            MapNative.ScrollEnabled = MapModel.HasScrollEnabled;
        }

        private void UpdateTrafficEnabled()
        {
            MapNative.ShowsTraffic = MapModel.TrafficEnabled;
        }

        private void UpdateHasZoomEnabled()
        {
            MapNative.ZoomEnabled = MapModel.HasZoomEnabled;
        }

        private void UpdateShowUserLocation()
        {
            if (MapModel.ShowUserLocation && _locationManager == null)
            {
                _locationManager = new CLLocationManager();
                _locationManager.RequestWhenInUseAuthorization();
            }
            else if (!MapModel.ShowUserLocation && _locationManager != null)
            {
                _locationManager.Dispose();
                _locationManager = null;
            }

            MapNative.ShowsUserLocation = MapModel.ShowUserLocation;
        }

        private void UpdateShowUserLocationButton()
        {
            if (MapModel.ShowUserLocationButton && _userTrackingButton == null)
            {
                const float utSize = 38f;

                _userTrackingButton = MKUserTrackingButton.FromMapView(MapNative);
                _userTrackingButton.Layer.CornerRadius = 4f;
                _userTrackingButton.Layer.BorderWidth = 0.25f;
                UpdateUserTrackingButtonTheme();

                var circleMask = new CoreAnimation.CAShapeLayer();
                var circlePath = UIBezierPath.FromRoundedRect(new CGRect(0, 0, utSize, utSize), utSize / 2);
                circleMask.Path = circlePath.CGPath;
                _userTrackingButton.Layer.Mask = circleMask;

                _userTrackingButton.TranslatesAutoresizingMaskIntoConstraints = false;

                MapNative.AddSubview(_userTrackingButton);

                var margins = MapNative.LayoutMarginsGuide;
                NSLayoutConstraint.ActivateConstraints(new[]
                {
                    _userTrackingButton.TopAnchor.ConstraintEqualTo(margins.TopAnchor, 48),
                    _userTrackingButton.TrailingAnchor.ConstraintEqualTo(margins.TrailingAnchor, -3),
                    _userTrackingButton.WidthAnchor.ConstraintEqualTo(utSize),
                    _userTrackingButton.HeightAnchor.ConstraintEqualTo(_userTrackingButton.WidthAnchor),
                });
            }
            else if (!MapModel.ShowUserLocationButton && _userTrackingButton != null)
            {
                _userTrackingButton.RemoveFromSuperview();
                _userTrackingButton.Dispose();
                _userTrackingButton = null;
            }
        }

        private void UpdateShowCompass()
        {
            MapNative.ShowsCompass = MapModel.ShowCompass;
        }

        private void UpdateSelectedPin()
        {
            var pin = MapModel.SelectedPin;

            if (pin == null)
            {
                foreach (var a in MapNative.SelectedAnnotations)
                    MapNative.DeselectAnnotation(a, false);
            }
            else if (pin.MarkerId is IMKAnnotation annotation)
            {
                MapNative.SelectAnnotation(annotation, false);
            }
        }

        private void UpdateMapTheme()
        {
            if (FormsMaps.iOs13OrNewer)
            {
                var mapTheme = MapModel.MapTheme;

                MapNative.OverrideUserInterfaceStyle = mapTheme switch
                {
                    MapTheme.System => UIUserInterfaceStyle.Unspecified,
                    MapTheme.Light => UIUserInterfaceStyle.Light,
                    MapTheme.Dark => UIUserInterfaceStyle.Dark,
                    _ => throw new NotSupportedException($"Unknown map theme '{mapTheme}'")
                };
            }
        }

        private void UpdateMapType()
        {
            var mapType = MapModel.MapType;
            MapNative.MapType = mapType switch
            {
                MapType.Street => MKMapType.MutedStandard,
                MapType.Satellite => MKMapType.Satellite,
                MapType.Hybrid => MKMapType.Hybrid,
                _ => throw new NotSupportedException($"Unknown map type '{mapType}'")
            };

            if (FormsMaps.iOs13OrNewer)
            {
                MapNative.PointOfInterestFilter = new MKPointOfInterestFilter(Array.Empty<MKPointOfInterestCategory>());
            }
            else
            {
                MapNative.ShowsPointsOfInterest = false;
            }
        }

        private void UpdateUserTrackingButtonTheme()
        {
            if (_userTrackingButton != null)
            {
                _userTrackingButton.Layer.BackgroundColor = (IsDarkMode ? UIColor.FromRGBA(49, 49, 51, 230) : UIColor.FromRGBA(255, 255, 255, 230)).CGColor;
                _userTrackingButton.Layer.BorderColor = (IsDarkMode ? UIColor.FromRGBA(0, 0, 0, 230) : UIColor.FromRGBA(191, 191, 191, 230)).CGColor;
            }
        }
        #endregion

        #region Pins
        private void OnPinCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (Device.IsInvokeRequired)
                Device.BeginInvokeOnMainThread(() => PinCollectionChanged(e));
            else
                PinCollectionChanged(e);
        }

        private void PinCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            var itemsToAdd = e.NewItems?.Cast<Pin>()?.ToList() ?? new List<Pin>(0);
            var itemsToRemove = e.OldItems?.Cast<Pin>()?.Where(p => p.MarkerId != null)?.ToList() ?? new List<Pin>(0);

            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    AddPins(itemsToAdd);
                    break;
                case NotifyCollectionChangedAction.Remove:
                    RemovePins(itemsToRemove);
                    break;
                case NotifyCollectionChangedAction.Replace:
                    RemovePins(itemsToRemove);
                    AddPins(itemsToAdd);
                    break;
                case NotifyCollectionChangedAction.Reset:
                    RemovePins(_pinLookup.Values.ToList());

                    AddPins((Element as Map).Pins);
                    break;
                case NotifyCollectionChangedAction.Move:
                    //do nothing
                    break;
            }
        }

        private void RemovePins(IList<Pin> pins)
        {
            var annotations = pins.Select(p =>
            {
                p.PropertyChanged -= PinOnPropertyChanged;

                var annotation = (IMKAnnotation)p.MarkerId;
                _pinLookup.Remove(annotation);
                p.MarkerId = null;

                return annotation;
            }).ToArray();

            var selectedToRemove =
                (from sa in MapNative.SelectedAnnotations ?? Array.Empty<IMKAnnotation>()
                 join a in annotations on sa equals a
                 select sa).ToList();

            foreach (var a in selectedToRemove)
                MapNative.DeselectAnnotation(a, false);

            MapNative.RemoveAnnotations(annotations);
        }

        private void AddPins(IList<Pin> pins)
        {
            var selectedAnnotation = default(IMKAnnotation);

            var annotations = pins.Select(p =>
            {
                p.PropertyChanged += PinOnPropertyChanged;
                var annotation = CreateAnnotation(p);
                p.MarkerId = annotation;

                if (ReferenceEquals(p, MapModel.SelectedPin))
                    selectedAnnotation = annotation;

                _pinLookup.Add(annotation, p);

                return annotation;
            }).ToArray();

            MapNative.AddAnnotations(annotations);

            if (selectedAnnotation != null)
                MapNative.SelectAnnotation(selectedAnnotation, true);
        }

        private void PinOnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (sender is Pin pin && pin.MarkerId is FormsMKPointAnnotation annotation)
            {
                if (e.PropertyName == Pin.LabelProperty.PropertyName)
                {
                    annotation.SetValueForKey(new NSString(pin.Label), new NSString(nameof(annotation.Title)));
                }
                else if (e.PropertyName == Pin.AddressProperty.PropertyName)
                {
                    annotation.SetValueForKey(new NSString(pin.Address), new NSString(nameof(annotation.Subtitle)));
                }
                else if (e.PropertyName == Pin.PositionProperty.PropertyName)
                {
                    var coord = new CLLocationCoordinate2D(pin.Position.Latitude, pin.Position.Longitude);
                    ((IMKAnnotation)annotation).SetCoordinate(coord);
                }
                else if (e.PropertyName == Pin.AnchorProperty.PropertyName)
                {
                    annotation.Anchor = new CGPoint((float)pin.Anchor.X, (float)pin.Anchor.Y);
                    if (MapNative.ViewForAnnotation(annotation) is MKAnnotationView view) view.Layer.AnchorPoint = annotation.Anchor;
                }
                else if (e.PropertyName == Pin.ZIndexProperty.PropertyName)
                {
                    annotation.ZIndex = pin.ZIndex;
                    if (FormsMaps.iOs14OrNewer && MapNative.ViewForAnnotation(annotation) is MKAnnotationView view)
                        view.SetValueForKey(new NSNumber((float)annotation.ZIndex), new NSString(nameof(view.ZPriority)));
                }
                else if (e.PropertyName == Pin.FileImageSourceProperty.PropertyName ||
                         e.PropertyName == Pin.TintColorProperty.PropertyName)
                {
                    annotation.TintColor = pin.TintColor.ToUIColor();
                    annotation.FileImage = pin.FileImage;

                    switch (MapNative.ViewForAnnotation(annotation))
                    {
                        case MKPinAnnotationView pinView:
                            pinView.SetValueForKey(annotation.TintColor, new NSString(nameof(pinView.PinTintColor)));
                            break;
                        case MKAnnotationView view:
                            var pinImage = GetMarkerImage(annotation.FileImage, annotation.TintColor);
                            if (pinImage != null)
                                view.SetValueForKey(pinImage, new NSString(nameof(view.Image)));
                            break;
                    }
                }
            }
        }

        private UIImage GetMarkerImage(string fileImage, UIColor tint)
        {
            var image = default(UIImage);

            if (!string.IsNullOrEmpty(fileImage))
            {
                if (!tint.IsEqual(UIColor.Clear))
                {
                    var cacheKey = $"TintedImage_{HashCode.Combine(fileImage, tint)}";

                    if (FormsMaps.Cache == null || !FormsMaps.Cache.TryGetValue(cacheKey, out image))
                    {
                        image = File.Exists(fileImage)
                            ? new UIImage(fileImage)
                            : UIImage.FromBundle(fileImage);
                        image.ImageWithRenderingMode(UIImageRenderingMode.AlwaysTemplate);

                        UIGraphics.BeginImageContextWithOptions(image.Size, false, image.CurrentScale);
                        var context = UIGraphics.GetCurrentContext();
                        tint.SetFill();
                        context.TranslateCTM(0, image.Size.Height);
                        context.ScaleCTM(1, -1);
                        var rect = new CGRect(0, 0, image.Size.Width, image.Size.Height);
                        context.ClipToMask(new CGRect(0, 0, image.Size.Width, image.Size.Height), image.CGImage);
                        context.FillRect(rect);
                        var tintedImage = UIGraphics.GetImageFromCurrentImageContext();
                        UIGraphics.EndImageContext();

                        image = tintedImage;

                        FormsMaps.Cache?.SetSliding(cacheKey, image, TimeSpan.FromMinutes(2));
                    }
                }

                image ??= File.Exists(fileImage)
                    ? new UIImage(fileImage)
                    : UIImage.FromBundle(fileImage);
            }

            return image;
        }

        protected Pin GetPinForAnnotation(IMKAnnotation annotation)
            => annotation != null && _pinLookup.TryGetValue(annotation, out var p) ? p : null;
        #endregion

        #region MapElements
        private void OnMapElementCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (Device.IsInvokeRequired)
                Device.BeginInvokeOnMainThread(() => MapElementCollectionChanged(e));
            else
                MapElementCollectionChanged(e);
        }

        private void MapElementCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            var itemsToAdd = e.NewItems?.Cast<MapElement>()?.ToList() ?? new List<MapElement>(0);
            var itemsToRemove = e.OldItems?.Cast<MapElement>()?.ToList() ?? new List<MapElement>(0);

            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    AddMapElements(itemsToAdd);
                    break;
                case NotifyCollectionChangedAction.Remove:
                    RemoveMapElements(itemsToRemove);
                    break;
                case NotifyCollectionChangedAction.Replace:
                    RemoveMapElements(itemsToRemove);
                    AddMapElements(itemsToAdd);
                    break;
                case NotifyCollectionChangedAction.Reset:
                    RemoveMapElements(_elementLookup.Values.ToList());

                    AddMapElements(MapModel.MapElements);
                    break;
            }
        }

        private void AddMapElements(IEnumerable<MapElement> mapElements)
        {
            var overlays = mapElements.Select(e =>
            {
                e.PropertyChanged += MapElementPropertyChanged;

                IMKOverlay overlay = e switch
                {
                    Polyline polyline => MKPolyline.FromCoordinates(polyline.Geopath
                            .Select(position => new CLLocationCoordinate2D(position.Latitude, position.Longitude))
                            .ToArray()),
                    Polygon polygon => MKPolygon.FromCoordinates(polygon.Geopath
                            .Select(position => new CLLocationCoordinate2D(position.Latitude, position.Longitude))
                            .ToArray()),
                    Circle circle => MKCircle.Circle(
                            new CLLocationCoordinate2D(circle.Center.Latitude, circle.Center.Longitude),
                            circle.Radius.Meters),
                    _ => throw new NotSupportedException("Element not supported")

                };

                e.MapElementId = overlay;
                _elementLookup.Add(overlay, e);

                return overlay;
            }).ToArray();

            MapNative.AddOverlays(overlays);
        }

        private void RemoveMapElements(IEnumerable<MapElement> mapElements)
        {
            var overlays = mapElements.Select(e =>
            {
                e.PropertyChanged -= MapElementPropertyChanged;

                var overlay = (IMKOverlay)e.MapElementId;
                _elementLookup.Remove(overlay);
                e.MapElementId = null;

                return overlay;
            }).ToArray();

            MapNative.RemoveOverlays(overlays);
        }

        private void MapElementPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            var element = (MapElement)sender;

            RemoveMapElements(new[] { element });
            AddMapElements(new[] { element });
        }

        protected virtual MKOverlayRenderer GetViewForOverlay(MKMapView mapview, IMKOverlay overlay)
            => overlay switch
            {
                MKPolyline polyline => GetViewForPolyline(polyline),
                MKPolygon polygon => GetViewForPolygon(polygon),
                MKCircle circle => GetViewForCircle(circle),
                _ => null
            };

        protected virtual MKPolylineRenderer GetViewForPolyline(MKPolyline mkPolyline)
            => _elementLookup.TryGetValue(mkPolyline, out var e) && e is Polyline pl
                ? new MKPolylineRenderer(mkPolyline)
                {
                    StrokeColor = pl.StrokeColor.ToUIColor(Color.Black),
                    LineWidth = pl.StrokeWidth
                }
                : null;

        protected virtual MKPolygonRenderer GetViewForPolygon(MKPolygon mkPolygon)
            => _elementLookup.TryGetValue(mkPolygon, out var e) && e is Polygon pg
                ? new MKPolygonRenderer(mkPolygon)
                {
                    StrokeColor = pg.StrokeColor.ToUIColor(Color.Black),
                    FillColor = pg.FillColor.ToUIColor(),
                    LineWidth = pg.StrokeWidth
                }
                : null;

        protected virtual MKCircleRenderer GetViewForCircle(MKCircle mkCircle)
            => _elementLookup.TryGetValue(mkCircle, out var e) && e is Circle c
                ? new MKCircleRenderer(mkCircle)
                {
                    StrokeColor = c.StrokeColor.ToUIColor(Color.Black),
                    FillColor = c.FillColor.ToUIColor(),
                    LineWidth = c.StrokeWidth
                }
                : null;
        #endregion

        private void CleanUpMapModelElements(Map mapModel, MKMapView mapNative)
        {
            MessagingCenter.Unsubscribe<Map, MapSpan>(this, Map.MoveToRegionMessageName);
            mapModel.Pins.CollectionChanged -= OnPinCollectionChanged;
            mapModel.MapElements.CollectionChanged -= OnMapElementCollectionChanged;

            foreach (var kv in _pinLookup)
            {
                kv.Value.PropertyChanged -= PinOnPropertyChanged;
                kv.Value.MarkerId = null;
            }

            foreach (var kv in _elementLookup)
            {
                kv.Value.PropertyChanged -= MapElementPropertyChanged;
                kv.Value.MapElementId = null;
            }

            if (mapNative?.SelectedAnnotations?.Length > 0)
                foreach (var sa in mapNative.SelectedAnnotations.ToList())
                    mapNative.DeselectAnnotation(sa, false);


            mapNative?.RemoveAnnotations(_pinLookup.Keys.ToArray());
            mapNative?.RemoveOverlays(_elementLookup.Keys.ToArray());

            _pinLookup.Clear();
            _elementLookup.Clear();
        }

        private void CleanUpNativeMap(MKMapView mapNative)
        {
            _userTrackingButton?.RemoveFromSuperview();
            _userTrackingButton?.Dispose();
            _userTrackingButton = null;

            _locationManager?.Dispose();
            _locationManager = null;

            mapNative.GetViewForAnnotation = null;
            mapNative.OverlayRenderer = null;
            mapNative.DidSelectAnnotationView -= MkMapViewOnAnnotationViewSelected;
            mapNative.DidDeselectAnnotationView -= MkMapViewOnAnnotationViewDeselected;
            mapNative.RegionChanged -= MkMapViewOnRegionChanged;

            mapNative.Delegate?.Dispose();
            mapNative.Delegate = null;

            mapNative.RemoveFromSuperview();

            if (_mapClickedGestureRecognizer != null)
            {
                mapNative.RemoveGestureRecognizer(_mapClickedGestureRecognizer);
                _mapClickedGestureRecognizer.Dispose();
                _mapClickedGestureRecognizer = null;
            }

            if (mapNative.Annotations?.Length > 0)
                mapNative.RemoveAnnotations(mapNative.Annotations.ToArray());

            if (mapNative.Overlays?.Length > 0)
                mapNative.RemoveOverlays(mapNative.Overlays.ToArray());
        }
    }
}
