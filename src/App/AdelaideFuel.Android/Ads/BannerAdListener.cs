using Android.Gms.Ads;
using System;

namespace AdelaideFuel.Droid
{
    internal class BannerAdListener : AdListener
    {
        public BannerAdListener()
        {
        }

        public event EventHandler AdReceived;
        public event EventHandler<LoadAdError> ReceiveAdFailed;

        public override void OnAdLoaded()
        {
            base.OnAdLoaded();
            AdReceived?.Invoke(this, new EventArgs());
        }

        public override void OnAdFailedToLoad(LoadAdError p0)
        {
            base.OnAdFailedToLoad(p0);
            ReceiveAdFailed?.Invoke(this, p0);
        }
    }
}
