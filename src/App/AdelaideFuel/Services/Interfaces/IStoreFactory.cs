using AdelaideFuel.Storage;

namespace AdelaideFuel.Services
{
    public interface IStoreFactory
    {
        int UserDbCurrentVersion { get; }

        (bool success, int count) EnsureUserPropertyDataTypes<T>(IStore<T> store) where T : class;

        IStore<T> GetUserStore<T>() where T : class;
        IStore<T> GetCacheStore<T>() where T : class;

        void CacheEmptyExpired();
        void CacheEmptyAll();

        long UserSizeInBytes();
        long CacheSizeInBytes();

        bool UserCheckpoint();
        bool CacheCheckpoint();

        bool UserRebuild();
        bool CacheRebuild();
    }
}