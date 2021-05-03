using System;
using System.Runtime.CompilerServices;

namespace AdelaideFuel.Services
{
    public class BaseService
    {
        protected readonly ICacheService MemoryCache;
        protected readonly ILogger Logger;

        public BaseService(ICacheService cacheService, ILogger logger)
        {
            MemoryCache = cacheService;
            Logger = logger;
        }

        protected string CacheKey([CallerMemberName] string caller = "")
            => !string.IsNullOrEmpty(caller)
               ? $"{GetType().Name}_{caller}"
               : throw new ArgumentException($"{GetType().Name}.{nameof(CacheKey)}() {nameof(caller)} cannot be empty");

        protected string CacheKey(long id, [CallerMemberName] string caller = "")
            => $"{CacheKey(caller)}_{id}";

        protected string CacheKey(string id, [CallerMemberName] string caller = "")
            => $"{CacheKey(caller)}_{id}";

        protected string CacheKey(string caller, params object[] keys)
            => $"{CacheKey(caller)}_{string.Join("_", keys)}";
    }
}