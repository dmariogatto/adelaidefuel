using AdelaideFuel.Models;
using AdelaideFuel.Storage;
using LiteDB;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Storage;
using Polly;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace AdelaideFuel.Services
{
    public class StoreFactory : IStoreFactory
    {
        private const string CacheDbName = "fuel_cache";
        private const string UserDbName = "fuel_user";

        private const int UserDbVersion = 0;

        private readonly object _dbLock = new object();

        private readonly IVersionTracking _versionTracking;
        private readonly IUserNativeReadOnlyService _userReadService;
        private readonly IAppPreferences _appPrefs;
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
            IUserNativeReadOnlyService userReadService,
            IAppPreferences appPrefs,
            ILogger logger)
        {
            _versionTracking = versionTracking;
            _userReadService = userReadService;
            _appPrefs = appPrefs;
            _logger = logger;

            _userPath = Path.Combine(fileSystem.AppDataDirectory, $"{UserDbName}.db");
            _cachePath = Path.Combine(fileSystem.CacheDirectory, $"{CacheDbName}.db");

#if DEBUG
            var rand = new Random();
            if (rand.Next(0, 9) == 4)
                File.WriteAllText(_userPath, "CORRUPT USER");
            if (rand.Next(0, 4) == 2)
                File.WriteAllText(_cachePath, "CORRUPT CACHE");
#endif

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
            if (_userDb is not null)
                return;

            lock (_dbLock)
            {
                if (_userDb is null)
                {
                    var connectionString = $"Filename={_userPath};Upgrade=true;";
                    _userDb = GetLiteDatabase(connectionString);

                    if (_userDb is null)
                    {
                        // Oh boy... user database is corrupt
                        // try to recreate
                        RecreateUserDb(connectionString);
                    }
                }
            }
        }

        private void InitCacheDb()
        {
            if (_cacheDb is not null)
                return;

            lock (_dbLock)
            {
                if (_cacheDb is null)
                {
                    if (_versionTracking.IsFirstLaunchForCurrentBuild && File.Exists(_cachePath))
                    {
                        // Sometimes the cache corrupts, this will hopefully help
                        DeleteLiteDatabase(_cachePath);
                    }

                    var connectionString = $"Filename={_cachePath};";
                    _cacheDb = GetLiteDatabase(connectionString);

                    if (_cacheDb is null)
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

                          var collections = db.GetCollectionNames();
                          foreach (var name in collections)
                          {
                              // Checking for a "LiteDB ENSURE" exception (i.e. data corruption)
                              db.GetCollection(name).EnsureIndex(nameof(IStoreItem.DateExpires));
                          }
                      });
            }
            catch (Exception ex)
            {
                _logger.Error(ex, new Dictionary<string, string>()
                {
                    { "connection_string", connectionString }
                });

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

                var files = di.GetFiles().Where(f => f.Name.StartsWith(fileName, StringComparison.Ordinal)).ToList();
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

        private void RecreateUserDb(string connectionString)
        {
            _appPrefs.LastDateSynced = DateTime.MinValue;

            DeleteLiteDatabase(_userPath);
            _userDb = GetLiteDatabase(connectionString);

            if (_userDb is not null)
            {
                try
                {
                    var userBrands = _userReadService.GetUserBrands();
                    var userFuels = _userReadService.GetUserFuels();
                    var userRadii = _userReadService.GetUserRadii();

                    if (userBrands?.Any() == true)
                    {
                        var brandStore = GetUserStore<UserBrand>();
                        brandStore.UpsertRangeAsync(userBrands.Select(i => (i.Id.ToString(CultureInfo.InvariantCulture), i)), TimeSpan.MaxValue, default).Wait();
                    }

                    if (userFuels?.Any() == true)
                    {
                        var fuelStore = GetUserStore<UserFuel>();
                        fuelStore.UpsertRangeAsync(userFuels.Select(i => (i.Id.ToString(CultureInfo.InvariantCulture), i)), TimeSpan.MaxValue, default).Wait();
                    }

                    if (userRadii?.Any() == true)
                    {
                        var radiusStore = GetUserStore<UserRadius>();
                        radiusStore.UpsertRangeAsync(userRadii.Select(i => (i.Id.ToString(CultureInfo.InvariantCulture), i)), TimeSpan.MaxValue, default).Wait();
                    }

                    _userDb.UserVersion = UserDbVersion;

                    _logger.Event(Events.Data.UserDbReconstruction, new Dictionary<string, string>()
                    {
                        { "brands", (userBrands?.Count ?? -1).ToString() },
                        { "fuels", (userFuels?.Count ?? -1).ToString() },
                        { "radii", (userRadii?.Count ?? -1).ToString() }
                    });
                }
                catch (Exception ex)
                {
                    _logger.Error(ex);
                }
            }
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