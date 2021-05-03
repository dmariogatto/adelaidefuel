using Microsoft.Extensions.Caching.Memory;
using System;

namespace AdelaideFuel.Services
{
    public class MemoryCacheProvider : ICacheService
    {
        private readonly MemoryCache _cache = new MemoryCache(new MemoryCacheOptions());

        public MemoryCacheProvider()
        {
        }

        public bool TryGetValue<T>(object key, out T value)
        {
            return _cache.TryGetValue(key, out value);
        }

        public void SetAbsolute<T>(object key, T value, TimeSpan expires)
        {
            _cache.Set(key, value, expires);
        }

        public void SetSliding<T>(object key, T value, TimeSpan sliding)
        {
            var options = new MemoryCacheEntryOptions() { SlidingExpiration = sliding };
            _cache.Set(key, value, options);
        }
    }
}