using Microsoft.Extensions.Caching.Memory;
using System;

namespace Xamarin.Forms.Maps
{
    public class MapCache : IMapCache
    {
        private readonly MemoryCache _cache = new MemoryCache(new MemoryCacheOptions());

        public MapCache()
        {
        }

        public bool TryGetValue<T>(object key, out T value)
        {
            return _cache.TryGetValue(key, out value);
        }

        public void SetAbsolute<T>(object key, T value, TimeSpan expires)
        {
            var options = new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(expires)
                .RegisterPostEvictionCallback(EvictedCallback);

            _cache.Set(key, value, expires);
        }

        public void SetSliding<T>(object key, T value, TimeSpan sliding)
        {
            var options = new MemoryCacheEntryOptions()
               .SetSlidingExpiration(sliding)
               .RegisterPostEvictionCallback(EvictedCallback);

            _cache.Set(key, value, options);
        }

        private static void EvictedCallback(object key, object item, EvictionReason reason, object state)
        {
            System.Diagnostics.Debug.WriteLine($"MapCache: Evicted '{key}'");
        }
    }
}