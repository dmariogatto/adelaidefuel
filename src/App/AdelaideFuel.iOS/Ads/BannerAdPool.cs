using AdelaideFuel.UI.Controls;
using System;
using System.Collections.Generic;

namespace AdelaideFuel.iOS
{
    public static class BannerAdPool
    {
        private readonly static TimeSpan PooledTime = Constants.AdPoolTime;
        private readonly static Dictionary<string, Queue<BannerAdView>> Pool = new Dictionary<string, Queue<BannerAdView>>();

        public static bool Add(BannerAdView bannerView)
        {
            if (string.IsNullOrEmpty(bannerView?.AdUnitId) || bannerView.AdStatus == AdLoadStatus.Failed)
                return false;

            lock (Pool)
            {
                var queue = default(Queue<BannerAdView>);
                if (!Pool.TryGetValue(bannerView.AdUnitId, out queue))
                {
                    queue = new Queue<BannerAdView>();
                    Pool[bannerView.AdUnitId] = queue;
                }

                queue.Enqueue(bannerView);
                return true;
            }
        }

        public static BannerAdView Get(string adUnitId)
        {
            if (string.IsNullOrEmpty(adUnitId))
                return null;

            lock(Pool)
            {
                if (Pool.TryGetValue(adUnitId, out var queue) && queue.Count > 0)
                {
                    do
                    {
                        if (queue.TryDequeue(out var bannerView))
                        {
                            if (bannerView.AdLoadedDateUtc >= DateTime.UtcNow.Subtract(PooledTime) &&
                                bannerView.AdStatus != AdLoadStatus.Failed)
                            {
                                return bannerView;
                            }
                            else
                            {
                                bannerView.Dispose();
                                bannerView = null;
                            }
                        }
                    } while (queue.Count > 0);
                }

                return null;
            }
        }

        public static void Clear()
        {
            lock (Pool)
            {
                foreach (var kv in Pool)
                {
                    var queue = kv.Value;
                    while (queue.TryDequeue(out var bannerView))
                    {
                        bannerView.Dispose();
                        bannerView = null;
                    }
                }

                Pool.Clear();
            }
        }
    }
}
