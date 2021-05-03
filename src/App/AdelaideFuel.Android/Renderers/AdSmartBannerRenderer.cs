using AdelaideFuel.Droid.Renderers;
using AdelaideFuel.UI.Controls;
using Android.Content;
using Android.Gms.Ads;
using Android.Runtime;
using System;
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

        protected override void OnElementChanged(ElementChangedEventArgs<AdSmartBanner> e)
        {
            base.OnElementChanged(e);

            if (_disposed
#if DEBUG
                //|| DeviceInfo.DeviceType == DeviceType.Virtual
#endif
                )
                return;

            var needToRequestAd = false;

            if (_adHolder == null && !string.IsNullOrEmpty(Element?.AdUnitId))
            {
                _adHolder = BannerAdPool.Get(Element.AdUnitId);

                if (_adHolder == null)
                {
                    var widthPixels = DeviceDisplay.MainDisplayInfo.Width;
                    var density = DeviceDisplay.MainDisplayInfo.Density;
                    var adWidth = (int)(widthPixels / density);

                    _adHolder = new BannerAdHolder(new AdView(Context)
                    {
                        AdUnitId = Element.AdUnitId,
                        AdSize = AdSize.GetCurrentOrientationAnchoredAdaptiveBannerAdSize(Context, adWidth)
                    });

                    needToRequestAd = true;
                }

                Element.HeightRequest = _adHolder.AdHeight;

                if (_adHolder.AdStatus == AdLoadStatus.Loaded)
                {
                    AttachBannerAd();
                }
                else
                {
                    _registered = true;
                    _adHolder.AdReceived += AdReceived;
                    _adHolder.ReceiveAdFailed += ReceiveAdFailed;
                }
            }

            if (needToRequestAd)
            {
                _adHolder.LoadAd(new AdRequest.Builder().Build());
            }
        }

        private void AdReceived(object sender, EventArgs e)
        {
            AttachBannerAd();
        }

        private void ReceiveAdFailed(object sender, LoadAdError e)
        {
            if (!_disposed && Element != null)
            {
                Element.HeightRequest = 0;
                Element.IsVisible = false;
                Element.AdStatus = AdLoadStatus.Failed;
            }

            System.Diagnostics.Debug.WriteLine($"Failed to load ad: {e?.Code ?? -1}, '{e?.Message ?? string.Empty}'");
        }

        private void AttachBannerAd()
        {
            if (!_disposed && Element != null && Control == null)
            {
                Element.IsVisible = true;
                SetNativeControl(_adHolder.View);
                Element.AdStatus = AdLoadStatus.Loaded;
            }
        }

        protected override bool ManageNativeControlLifetime => false;

        protected override void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            _disposed = true;

            if (disposing && _adHolder != null)
            {
                if (_registered)
                {
                    _adHolder.AdReceived -= AdReceived;
                    _adHolder.ReceiveAdFailed -= ReceiveAdFailed;
                    _registered = false;
                }

                _adHolder.View.RemoveFromParent();
                BannerAdPool.Add(_adHolder);
                _adHolder = null;
            }

            base.Dispose(disposing);
        }
    }    
}