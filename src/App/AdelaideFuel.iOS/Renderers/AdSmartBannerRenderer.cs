using AdelaideFuel.iOS.Renderers;
using AdelaideFuel.UI.Controls;
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

            if (_bannerView is null &&
                !string.IsNullOrEmpty(e.NewElement?.AdUnitId) &&
                GetRootViewController() is UIViewController rvc)
            {
                _bannerView = BannerAdPool.Get(Element.AdUnitId);

                if (_bannerView is null)
                {
                    var size = rvc.View?.Frame.Size ?? UIScreen.MainScreen.Bounds.Size;
                    var adSize = AdSizeCons.GetCurrentOrientationAnchoredAdaptiveBannerAdSize(size.Width);

                    _bannerView = new BannerAdView(adSize)
                    {
                        AdUnitId = Element.AdUnitId,
                        RootViewController = rvc
                    };
                }

                Element.HeightRequest = _bannerView.AdHeight;

                switch (_bannerView.AdStatus)
                {
                    case AdLoadStatus.Loaded:
                        AttachBannerAd();
                        break;
                    case AdLoadStatus.Failed:
                        DetachBannerAd();
                        break;
                    default:
                        if (!_registered)
                        {
                            _registered = true;
                            _bannerView.AdReceived += AdReceived;
                            _bannerView.ReceiveAdFailed += ReceiveAdFailed;
                        }
                        break;
                }
            }

            if (_bannerView?.AutoloadEnabled == false)
                _bannerView.AutoloadEnabled = true;
        }

        private void AdReceived(object sender, EventArgs e)
        {
            AttachBannerAd();
        }

        private void ReceiveAdFailed(object sender, BannerViewErrorEventArgs e)
        {
            DetachBannerAd();
            System.Diagnostics.Debug.WriteLine($"Failed to load ad: {e.Error}");
        }

        private void AttachBannerAd()
        {
            if (!_disposed && Element is not null && Control is null)
            {
                Element.IsVisible = true;
                Element.HeightRequest = _bannerView.AdHeight;
                SetNativeControl(_bannerView);
                Element.AdStatus = AdLoadStatus.Loaded;
            }
        }

        private void DetachBannerAd()
        {
            if (!_disposed && Element is not null)
            {
                Element.HeightRequest = 0;
                Element.IsVisible = false;
                Element.AdStatus = AdLoadStatus.Failed;
            }
        }

        protected override bool ManageNativeControlLifetime => false;

        protected override void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            _disposed = true;

            if (disposing && _bannerView is not null)
            {
                if (_registered)
                {
                    _bannerView.AdReceived -= AdReceived;
                    _bannerView.ReceiveAdFailed -= ReceiveAdFailed;
                    _registered = false;
                }

                _bannerView.RemoveFromSuperview();

                if (!BannerAdPool.Add(_bannerView))
                    _bannerView.Dispose();

                _bannerView = null;
            }

            base.Dispose(disposing);
        }

        private static UIViewController GetRootViewController()
            => UIApplication.SharedApplication.Windows
                .Where(w => w.RootViewController is not null)
                .Select(w => w.RootViewController)
                .FirstOrDefault();
    }
}