using AdelaideFuel.Storage;

namespace AdelaideFuel.Services
{
    public interface IStoreFactory
    {
        IUserStore<T> GetUserStore<T>() where T : class;
        ICacheStore<T> GetCacheStore<T>() where T : class;

        void CacheEmptyExpired();
        void CacheEmptyAll();

        long CacheSizeInBytes();
    }
}