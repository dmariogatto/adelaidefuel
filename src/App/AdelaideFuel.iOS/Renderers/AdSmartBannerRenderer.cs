using AdelaideFuel.iOS.Renderers;
using AdelaideFuel.Services;
using AdelaideFuel.UI.Controls;
using CoreGraphics;
using Foundation;
using Google.MobileAds;
using System;
using System.Linq;
using UIKit;
using Xamarin.Forms;
using Xamarin.Forms.Platform.iOS;

[assembly: ExportRenderer(typeof(AdSmartBanner), typeof(AdSmartBannerRenderer))]
namespace AdelaideFuel.iOS.Renderers
{
    [Preserve(AllMembers = true)]
    public class AdSmartBannerRenderer : ViewRenderer<AdSmartBanner, UIView>
    {
        private bool _disposed;
        private bool _registered;
        private BannerAdView _bannerView;

        protected override void OnElementChanged(ElementChangedEventArgs<AdSmartBanner> e)
        {
            base.OnElementChanged(e);

            if (_disposed)
                return;

            var needToRequestAd = false;

            if (_bannerView == null &&
                !string.IsNullOrEmpty(e.NewElement?.AdUnitId) &&
                GetRootViewController() is UIViewController rvc)
            {
                _bannerView = BannerAdPool.Get(Element.AdUnitId);

                if (_bannerView == null)
                {
                    var size = rvc.View?.Frame.Size ?? UIScreen.MainScreen.Bounds.Size;
                    var adSize = AdSizeCons.GetCurrentOrientationAnchoredAdaptiveBannerAdSize(size);

                    _bannerView = new BannerAdView(adSize)
                    {
                        AdUnitId = Element.AdUnitId,
                        RootViewController = rvc
                    };

                    needToRequestAd = true;
                }

                Element.HeightRequest = _bannerView.AdHeight;

                if (_bannerView.AdStatus == AdLoadStatus.Loaded)
                {
                    AttachBannerAd();
                }
                else
                {
                    _registered = true;
                    _bannerView.AdReceived += AdReceived;
                    _bannerView.ReceiveAdFailed += ReceiveAdFailed;
                }
            }

            if (needToRequestAd)
            {
                var request = Request.GetDefaultRequest();
                _bannerView.LoadRequest(request);
            }
        }

        private void AdReceived(object sender, EventArgs e)
        {
            AttachBannerAd();
        }

        private void ReceiveAdFailed(object sender, BannerViewErrorEventArgs e)
        {
            if (!_disposed && Element != null)
            {
                Element.HeightRequest = 0;
                Element.IsVisible = false;
                Element.AdStatus = AdLoadStatus.Failed;
            }

            System.Diagnostics.Debug.WriteLine($"Failed to load ad: {e.Error}");
        }

        private void AttachBannerAd()
        {
            if (!_disposed && Element != null && Control == null)
            {
                Element.IsVisible = true;
                SetNativeControl(_bannerView);
                Element.AdStatus = AdLoadStatus.Loaded;
            }
        }

        protected override bool ManageNativeControlLifetime => false;

        protected override void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            _disposed = true;

            if (disposing && _bannerView != null)
            {
                if (_registered)
                {
                    _bannerView.AdReceived -= AdReceived;
                    _bannerView.ReceiveAdFailed -= ReceiveAdFailed;
                    _registered = false;
                }

                _bannerView.RemoveFromSuperview();
                BannerAdPool.Add(_bannerView);
                _bannerView = null;
            }

            base.Dispose(disposing);
        }

        private static UIViewController GetRootViewController()
            => UIApplication.SharedApplication.Windows
                .Where(w => w.RootViewController != null)
                .Select(w => w.RootViewController)
                .FirstOrDefault();
    }
}