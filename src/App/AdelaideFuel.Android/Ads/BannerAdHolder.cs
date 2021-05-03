using AdelaideFuel.UI.Controls;
using Android.Gms.Ads;
using System;

namespace AdelaideFuel.Droid
{
    public class BannerAdHolder : IDisposable
    {
        private AdView _adView;
        private BannerAdListener _bannerAdListener;

        private bool _disposed;

        public BannerAdHolder(AdView adView)
        {
            _adView = adView ?? throw new ArgumentNullException(nameof(adView));
            _bannerAdListener = new BannerAdListener();
            _adView.AdListener = _bannerAdListener;

            AdReceived += BannerAdReceived;
            ReceiveAdFailed += BannerAdFailed;
        }

        public event EventHandler AdReceived
        {
            add => _bannerAdListener.AdReceived += value;
            remove => _bannerAdListener.AdReceived -= value;
        }

        public event EventHandler<LoadAdError> ReceiveAdFailed
        {
            add => _bannerAdListener.ReceiveAdFailed += value;
            remove => _bannerAdListener.ReceiveAdFailed -= value;
        }

        public AdView View => _adView;

        public string AdUnitId => _adView?.AdUnitId;
        public int AdHeight => _adView?.AdSize?.Height ?? -1;
        public int AdWidth => _adView?.AdSize?.Width ?? -1;

        public AdLoadStatus AdStatus { get; private set; }

        public DateTime AdLoadedDateUtc { get; private set; } = DateTime.MaxValue;

        public void LoadAd(AdRequest adRequest)
        {
            if (_disposed) return;

            AdStatus = AdLoadStatus.Loading;
            _adView.LoadAd(adRequest);
        }

        private void BannerAdReceived(object sender, EventArgs e)
        {
            AdStatus = AdLoadStatus.Loaded;
            AdLoadedDateUtc = DateTime.UtcNow;
        }

        private void BannerAdFailed(object sender, LoadAdError e)
        {
            AdStatus = AdLoadStatus.Failed;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            _disposed = true;

            if (disposing)
            {
                AdReceived -= BannerAdReceived;
                ReceiveAdFailed -= BannerAdFailed;

                AdStatus = AdLoadStatus.None;
                AdLoadedDateUtc = DateTime.MinValue;

                _adView.AdListener = null;

                _bannerAdListener.Dispose();
                _bannerAdListener = null;

                _adView.Dispose();
                _adView = null;
            }
        }

        public void Dispose() => Dispose(true);
    }
}
