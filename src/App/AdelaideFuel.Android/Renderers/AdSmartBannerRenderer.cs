using AdelaideFuel.Droid.Renderers;
using AdelaideFuel.UI.Controls;
using Android.Content;
using Android.Gms.Ads;
using Android.Runtime;
using System;
using System.ComponentModel;
using Xamarin.Essentials;
using Xamarin.Forms;
using Xamarin.Forms.Platform.Android;

[assembly: ExportRenderer(typeof(AdSmartBanner), typeof(AdSmartBannerRenderer))]
namespace AdelaideFuel.Droid.Renderers
{
    [Preserve(AllMembers = true)]
    public class AdSmartBannerRenderer : ViewRenderer<AdSmartBanner, AdView>
    {
        private bool _disposed;
        private bool _registered;
        private BannerAdHolder _adHolder;

        public AdSmartBannerRenderer(Context context) : base(context)
        {
        }

        protected override bool ManageNativeControlLifetime => false;

        protected override void OnElementChanged(ElementChangedEventArgs<AdSmartBanner> e)
        {
            base.OnElementChanged(e);

            AdUnitIdChanged();
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

            if (_adHolder is null &&
                !string.IsNullOrEmpty(Element?.AdUnitId))
            {
                _adHolder = BannerAdPool.Get(Element.AdUnitId);
                var needToRequestAd = false;

                if (_adHolder is null)
                {
                    needToRequestAd = true;

                    var widthPixels = DeviceDisplay.MainDisplayInfo.Width;
                    var density = DeviceDisplay.MainDisplayInfo.Density;
                    var adWidth = (int)(widthPixels / density);

                    _adHolder = new BannerAdHolder(new AdView(Context)
                    {
                        AdUnitId = Element.AdUnitId,
                        AdSize = AdSize.GetCurrentOrientationAnchoredAdaptiveBannerAdSize(Context, adWidth)
                    });
                }

                Element.HeightRequest = _adHolder.AdHeight;

                switch (_adHolder.AdStatus)
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
                    _adHolder.AdReceived += AdReceived;
                    _adHolder.ReceiveAdFailed += ReceiveAdFailed;
                }

                if (needToRequestAd)
                    _adHolder.LoadAd(new AdRequest.Builder().Build());
            }
        }

        private void AdReceived(object sender, EventArgs e)
        {
            BannerAdLoaded();
        }

        private void ReceiveAdFailed(object sender, LoadAdError e)
        {
            BannerAdFailed();
            System.Diagnostics.Debug.WriteLine($"Failed to load ad: {e?.Code ?? -1}, '{e?.Message ?? string.Empty}'");
        }

        private void BannerAdLoaded()
        {
            if (!_disposed && Element is not null && _adHolder is not null)
            {
                if (Control != _adHolder.View)
                    SetNativeControl(_adHolder.View);
                Element.IsVisible = true;
                Element.HeightRequest = _adHolder.AdHeight;
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
            if (_adHolder is not null)
            {
                if (_registered)
                {
                    _adHolder.AdReceived -= AdReceived;
                    _adHolder.ReceiveAdFailed -= ReceiveAdFailed;
                    _registered = false;
                }

                _adHolder.View.RemoveFromParent();

                if (!BannerAdPool.Add(_adHolder))
                    _adHolder.Dispose();

                _adHolder = null;
            }
        }
    }
}