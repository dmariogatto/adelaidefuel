using AdelaideFuel.UI.Controls;
using Google.MobileAds;
using System;

namespace AdelaideFuel.iOS
{
    public class BannerAdView : BannerView
    {
        private bool _disposed;

        public BannerAdView(AdSize size) : base(size)
        {
            AdReceived += BannerAdReceived;
            ReceiveAdFailed += BannerAdFailed;
        }

        public nfloat AdHeight => AdSize.Size.Height;
        public nfloat AdWidth => AdSize.Size.Width;

        public AdLoadStatus AdStatus { get; private set; }

        public DateTime AdLoadedDateUtc { get; private set; } = DateTime.MaxValue;

        public override void LoadRequest(Request request)
        {
            if (_disposed) return;

            AdStatus = AdLoadStatus.Loading;
            base.LoadRequest(request);
        }

        private void BannerAdReceived(object sender, EventArgs e)
        {
            AdStatus = AdLoadStatus.Loaded;
            AdLoadedDateUtc = DateTime.UtcNow;
        }

        private void BannerAdFailed(object sender, BannerViewErrorEventArgs e)
        {
            AdStatus = AdLoadStatus.Failed;
        }

        protected override void Dispose(bool disposing)
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
            }

            base.Dispose(disposing);
        }
    }
}
