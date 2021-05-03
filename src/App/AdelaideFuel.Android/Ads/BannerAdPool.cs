using AdelaideFuel.UI.Controls;
using System;
using System.Collections.Generic;

namespace AdelaideFuel.Droid
{
    public static class BannerAdPool
    {
        private readonly static TimeSpan PooledTime = Constants.AdPoolTime;
        private readonly static Dictionary<string, Queue<BannerAdHolder>> Pool = new Dictionary<string, Queue<BannerAdHolder>>();

        public static bool Add(BannerAdHolder adHolder)
        {
            if (string.IsNullOrEmpty(adHolder?.AdUnitId) || adHolder.AdStatus == AdLoadStatus.Failed)
                return false;

            lock (Pool)
            {
                var queue = default(Queue<BannerAdHolder>);
                if (!Pool.TryGetValue(adHolder.AdUnitId, out queue))
                {
                    queue = new Queue<BannerAdHolder>();
                    Pool[adHolder.AdUnitId] = queue;
                }

                queue.Enqueue(adHolder);
                return true;
            }
        }

        public static BannerAdHolder Get(string adUnitId)
        {
            if (string.IsNullOrEmpty(adUnitId))
                return null;

            lock (Pool)
            {
                if (Pool.TryGetValue(adUnitId, out var queue) && queue.Count > 0)
                {
                    do
                    {
                        if (queue.TryDequeue(out var adHolder))
                        {
                            if (adHolder.AdLoadedDateUtc >= DateTime.UtcNow.Subtract(PooledTime) &&
                                adHolder.AdStatus != AdLoadStatus.Failed)
                            {
                                return adHolder;
                            }
                            else
                            {
                                adHolder.Dispose();
                                adHolder = null;
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
                    while (queue.TryDequeue(out var adHolder))
                    {
                        adHolder.Dispose();
                        adHolder = null;
                    }
                }

                Pool.Clear();
            }
        }
    }
}
