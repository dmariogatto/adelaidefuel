using AdelaideFuel.Models;
using AdelaideFuel.Storage;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Storage;
using System;
using System.Collections.Concurrent;
using System.IO;

namespace AdelaideFuel.Services
{
    public class StoreFactory : IStoreFactory
    {
        private readonly ILogger _logger;

        private readonly string _cachePath;

        private readonly ConcurrentDictionary<Type, IUserStore> _userStores = new ConcurrentDictionary<Type, IUserStore>();
        private readonly ConcurrentDictionary<Type, ICacheStore> _cacheStores = new ConcurrentDictionary<Type, ICacheStore>();

        public StoreFactory(
            IFileSystem fileSystem,
            IPreferences preferences,
            IVersionTracking versionTracking,
            ILogger logger)
        {
            _logger = logger;
            _cachePath = Path.Combine(fileSystem.CacheDirectory, "user_cache");

            _userStores.TryAdd(
                typeof(UserBrand),
                new UserPrefStore<UserBrand>(nameof(UserBrand), Constants.SharedGroupName, preferences, logger));
            _userStores.TryAdd(
                typeof(UserFuel),
                new UserPrefStore<UserFuel>(nameof(UserFuel), Constants.SharedGroupName, preferences, logger));
            _userStores.TryAdd(
                typeof(UserRadius),
                new UserPrefStore<UserRadius>(nameof(UserRadius), Constants.SharedGroupName, preferences, logger));

            ClearCache(fileSystem, versionTracking);
        }

        public IUserStore<T> GetUserStore<T>() where T : class
            => _userStores.TryGetValue(typeof(T), out var result)
                ? result as IUserStore<T>
                : throw new InvalidOperationException($"UserStore for {typeof(T).Name} is not setup!");

        public ICacheStore<T> GetCacheStore<T>() where T : class
            => _cacheStores.GetOrAdd(typeof(T), (k) => new CacheFileStore<T>(_cachePath, _logger)) as ICacheStore<T>;

        public void CacheEmptyExpired() => _cacheStores.Values.ForEach(v => v.EmptyExpired());
        public void CacheEmptyAll() => _cacheStores.Values.ForEach(v => v.EmptyAll());

        public long CacheSizeInBytes() => SizeInBytes(_cachePath);

        private long SizeInBytes(string path)
        {
            var size = 0L;

            try
            {
                var di = new DirectoryInfo(path);
                foreach (var fi in di.GetFiles("*.*", SearchOption.AllDirectories))
                {
                    size += fi.Length;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
            }

            return size;
        }

        private void ClearCache(IFileSystem fileSystem, IVersionTracking versionTracking)
        {
            try
            {
                static void clearOldLiteDbs(string path)
                {
                    var di = new DirectoryInfo(path);
                    foreach (var fi in di.GetFiles("*.db"))
                        fi.Delete();
                }
                clearOldLiteDbs(fileSystem.AppDataDirectory);
                clearOldLiteDbs(fileSystem.CacheDirectory);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
            }

            if (versionTracking.IsFirstLaunchForCurrentBuild)
            {
                try
                {
                    var di = new DirectoryInfo(_cachePath);
                    di.Delete(true);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(ex);
                }
            }
        }
    }
}