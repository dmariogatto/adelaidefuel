using AdelaideFuel.Storage;
using LiteDB;
using Polly;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xamarin.Essentials.Interfaces;

namespace AdelaideFuel.Services
{
    public class StoreFactory : IStoreFactory
    {
        private const string CacheDbName = "fuel_cache";
        private const string UserDbName = "fuel_user";

        private const int UserDbVersion = 0;

        private readonly object _dbLock = new object();

        private readonly IVersionTracking _versionTracking;
        private readonly ILogger _logger;

        private readonly string _userPath;
        private readonly string _cachePath;

        private readonly ConcurrentDictionary<Type, IStore> _userStores = new ConcurrentDictionary<Type, IStore>();
        private readonly ConcurrentDictionary<Type, IStore> _cacheStores = new ConcurrentDictionary<Type, IStore>();

        private LiteDatabase _userDb;
        private LiteDatabase _cacheDb;

        public StoreFactory(
            IFileSystem fileSystem,
            IVersionTracking versionTracking,
            ILogger logger)
        {
            _versionTracking = versionTracking;
            _logger = logger;

            _userPath = Path.Combine(fileSystem.AppDataDirectory, $"{UserDbName}.db");
            _cachePath = Path.Combine(fileSystem.CacheDirectory, $"{CacheDbName}.db");

            InitUserDb();
            InitCacheDb();
        }

        public int UserDbCurrentVersion => _userDb?.UserVersion ?? -1;

        public (bool success, int count) EnsureUserPropertyDataTypes<T>(IStore<T> store) where T : class
        {
            if (_userStores.ContainsKey(typeof(T)))
            {
                return EnsurePropertyDataTypes<T>(_userDb, store.Name);
            }

            return (false, 0);
        }

        public IStore<T> GetUserStore<T>() where T : class
            => _userStores.GetOrAdd(typeof(T), (k) => new LiteStore<T>(_userDb, false, _logger)) as IStore<T>;

        public IStore<T> GetCacheStore<T>() where T : class
            => _cacheStores.GetOrAdd(typeof(T), (k) => new LiteStore<T>(_cacheDb, true, _logger)) as IStore<T>;

        public void CacheEmptyExpired() => TryExecute(() => _cacheStores.Values.ForEach(v => v.EmptyExpiredAsync(default).Wait()), CacheDbName);
        public void CacheEmptyAll() => TryExecute(() => _cacheStores.Values.ForEach(v => v.EmptyAllAsync(default).Wait()), CacheDbName);

        public long UserSizeInBytes() => SizeInBytes(_userPath);
        public long CacheSizeInBytes() => SizeInBytes(_cachePath);

        public bool UserCheckpoint() => TryExecute(() => _userDb.Checkpoint(), UserDbName);
        public bool CacheCheckpoint() => TryExecute(() => _cacheDb.Checkpoint(), CacheDbName);

        public bool UserRebuild() => TryExecute(() => _userDb.Rebuild(), UserDbName);
        public bool CacheRebuild() => TryExecute(() => _cacheDb.Rebuild(), CacheDbName);

        private long SizeInBytes(string path)
        {
            var size = 0L;

            if (File.Exists(path))
            {
                try
                {
                    var fileInfo = new FileInfo(path);
                    size = fileInfo.Length;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(ex);
                }
            }

            return size;
        }

        private bool TryExecute(Action action, string dbName)
        {
            var success = false;

            try
            {
                action();
                success = true;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, new Dictionary<string, string>()
                {
                    { "db", dbName }
                });
            }

            return success;
        }

        #region Database Init
        private void InitUserDb()
        {
            if (_userDb != null)
                return;

            lock (_dbLock)
            {
                if (_userDb == null)
                {
                    var connectionString = $"Filename={_userPath};Upgrade=true;";
                    _userDb = GetLiteDatabase(connectionString);

                    if (_userDb == null)
                    {
                        DeleteLiteDatabase(_userPath);
                        _userDb = GetLiteDatabase(connectionString);
                    }
                }
            }
        }

        private void InitCacheDb()
        {
            if (_cacheDb != null)
                return;

            lock (_dbLock)
            {
                if (_cacheDb == null)
                {
                    if (_versionTracking.IsFirstLaunchForCurrentBuild && File.Exists(_cachePath))
                    {
                        // Sometime the cache corrupts, this will hopefully help
                        DeleteLiteDatabase(_cachePath);
                    }

                    var connectionString = $"Filename={_cachePath};";
                    _cacheDb = GetLiteDatabase(connectionString);

                    if (_cacheDb == null)
                    {
                        DeleteLiteDatabase(_cachePath);
                        _cacheDb = GetLiteDatabase(connectionString);
                    }
                }
            }
        }

        private LiteDatabase GetLiteDatabase(string connectionString)
        {
            var db = default(LiteDatabase);

            try
            {
                Policy.Handle<LiteException>()
                      .Or<InvalidOperationException>()
                      .WaitAndRetry
                        (
                            retryCount: 3,
                            sleepDurationProvider: retryAttempt => TimeSpan.FromMilliseconds(6 * Math.Pow(2, retryAttempt))
                        )
                      .Execute(() =>
                      {
                          db = new LiteDatabase(connectionString);
                          db.Pragma("UTC_DATE", true);
                      });
            }
            catch (Exception ex)
            {
                _logger.Error(ex);

                db?.Dispose();
                db = null;
            }

            return db;
        }

        private bool DeleteLiteDatabase(string dbPath)
        {
            var success = false;

            try
            {
                var di = new DirectoryInfo(Path.GetDirectoryName(dbPath));
                var fileName = Path.GetFileNameWithoutExtension(dbPath);

                var files = di.GetFiles().Where(f => f.Name.StartsWith(fileName)).ToList();
                foreach (var f in files)
                    File.Delete(f.FullName);

                success = true;
            }
            catch (Exception ex)
            {
                _logger.Error(ex);
            }

            return success;
        }
        #endregion

        #region Migration
        private (bool success, int count) EnsurePropertyDataTypes<T>(LiteDatabase db, string collectionName)
        {
            var success = true;
            var count = 0;

            try
            {
                if (db?.CollectionExists(collectionName) == true)
                {
                    var dataType = typeof(T);

                    var properties = dataType.GetProperties()
                        .Where(p => p.CanRead &&
                                    p.CanWrite &&
                                    !p.GetCustomAttributes(false)
                                      .Any(i => i is BsonIdAttribute || i is BsonIgnoreAttribute))
                        .ToList();

                    var collection = db.GetCollection(collectionName);
                    var items = collection.FindAll();

                    foreach (var i in items)
                    {
                        var bDoc = i[nameof(StoreItem<T>.Contents)].AsDocument;
                        var hasChanged = false;

                        foreach (var p in properties.Where(p => bDoc.ContainsKey(p.Name)))
                        {
                            var bProp = bDoc[p.Name];
                            var rawVal = bProp.RawValue;

                            if (rawVal?.GetType() is Type rawType &&
                                rawVal.GetType() != p.PropertyType &&
                                (rawType.IsNumeric() || rawType.IsString()) &&
                                (p.PropertyType.IsNumeric() || p.PropertyType.IsString()))
                            {
                                var type = p.PropertyType.IsGenericType &&
                                           p.PropertyType.GetGenericTypeDefinition() == typeof(Nullable<>)
                                           ? Nullable.GetUnderlyingType(p.PropertyType)
                                           : p.PropertyType;
                                var val = Convert.ChangeType(rawVal, type);
                                bDoc[p.Name] = new BsonValue(val);

                                hasChanged = true;
                            }
                        }

                        if (hasChanged)
                        {
                            collection.Update(i);
                            count++;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, new Dictionary<string, string>()
                    {
                        { "collection_name", collectionName }
                    });

                success = false;
            }

            return (success, count);
        }
        #endregion
    }
}