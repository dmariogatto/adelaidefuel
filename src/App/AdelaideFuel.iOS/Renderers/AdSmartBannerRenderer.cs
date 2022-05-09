using AdelaideFuel.iOS.Renderers;
using AdelaideFuel.UI.Controls;
using Foundation;
using Google.MobileAds;
using System;
using System.ComponentModel;
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

        protected override bool ManageNativeControlLifetime => false;

        protected override void OnElementChanged(ElementChangedEventArgs<AdSmartBanner> e)
        {
            base.OnElementChanged(e);

            if (e.NewElement is not null)
            {
                AdUnitIdChanged();
            }
        }

        protected override void OnElementPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            base.OnElementPropertyChanged(sender, e);

            if (e.PropertyName == AdSmartBanner.AdUnitIdProperty.PropertyName)
                AdUnitIdChanged();
        }

        protected override void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            _disposed = true;

            if (disposing)
            {
                CleanUpBannerAd();
            }

            base.Dispose(disposing);
        }

        private void AdUnitIdChanged()
        {
            if (_disposed)
                return;

            CleanUpBannerAd();

            if (_bannerView is null &&
                !string.IsNullOrEmpty(Element?.AdUnitId) &&
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
                        BannerAdLoaded();
                        break;
                    case AdLoadStatus.Failed:
                        BannerAdFailed();
                        break;
                    default:
                        break;
                }

                if (!_registered)
                {
                    _registered = true;
                    _bannerView.AdReceived += AdReceived;
                    _bannerView.ReceiveAdFailed += ReceiveAdFailed;
                }

                if (!_bannerView.AutoloadEnabled)
                    _bannerView.AutoloadEnabled = true;
            }
        }

        private void AdReceived(object sender, EventArgs e)
        {
            BannerAdLoaded();
        }

        private void ReceiveAdFailed(object sender, BannerViewErrorEventArgs e)
        {
            BannerAdFailed();
            System.Diagnostics.Debug.WriteLine($"Failed to load ad: {e.Error}");
        }

        private void BannerAdLoaded()
        {
            if (!_disposed && Element is not null && _bannerView is not null)
            {
                if (Control != _bannerView)
                    SetNativeControl(_bannerView);
                Element.IsVisible = true;
                Element.HeightRequest = _bannerView.AdHeight;
                Element.AdStatus = AdLoadStatus.Loaded;
            }
        }

        private void BannerAdFailed()
        {
            if (!_disposed && Element is not null)
            {
                Element.HeightRequest = 0;
                Element.IsVisible = false;
                Element.AdStatus = AdLoadStatus.Failed;
            }
        }

        private void CleanUpBannerAd()
        {
            if (_bannerView is not null)
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
        }

        private static UIViewController GetRootViewController()
            => UIApplication.SharedApplication.Windows
                .Where(w => w.RootViewController is not null)
                .Select(w => w.RootViewController)
                .FirstOrDefault();
    }
}