using AdelaideFuel.Services;
using LiteDB;
using Polly;
using Polly.Retry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AdelaideFuel.Storage
{
    public class LiteStore<T> : IStore<T> where T : class
    {
        private const string IdColumn = "_id";

        private readonly ILogger _logger;

        private readonly LiteDatabase _db;
        private readonly ILiteCollection<StoreItem<T>> _col;
        private readonly bool _isCache;

        private readonly AsyncRetryPolicy _retryPolicy =
               Policy.Handle<LiteException>()
                     .WaitAndRetryAsync
                       (
                           retryCount: 3,
                           sleepDurationProvider: retryAttempt => TimeSpan.FromMilliseconds(6 * Math.Pow(2, retryAttempt))
                       );

        public LiteStore(LiteDatabase liteDatabase, bool isCache, ILogger logger, string collectionName = "")
        {
            _logger = logger;

            _db = liteDatabase;
            _isCache = isCache;

            var name = collectionName;

            if (string.IsNullOrEmpty(name))
            {
                name = GetCollectionNameByType();
            }

            if (string.IsNullOrEmpty(name))
                throw new ArgumentException($"Collection name cannot be empty (type: '{typeof(T).FullName}')");

            _col = _db.GetCollection<StoreItem<T>>(name);
        }

        public string Name { get => _col?.Name ?? string.Empty; }

        #region Exist and Expiration Methods
        public async Task<bool> ExistsAsync(string key, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Key can not be null or empty.", nameof(key));

            var item = await GetAsync(key, true, cancellationToken).ConfigureAwait(false);
            return item != null;
        }

        public async Task<bool> IsExpiredAsync(string key, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Key can not be null or empty.", nameof(key));

            var item = await GetEntity(key, cancellationToken).ConfigureAwait(false);
            return item == null || item.HasExpired();
        }

        #endregion

        #region Get Methods
        public async Task<IList<T>> AllAsync(bool includeExpired, CancellationToken cancellationToken)
        {
            var items = default(IList<T>);

            try
            {
                items = await _retryPolicy.ExecuteAsync(
                    async (ct) => await Task.Run(() => _col.FindAll()
                                                           .Where(i => includeExpired || !i.HasExpired())
                                                           .Select(i => i.Contents)
                                                           .ToList()).ConfigureAwait(false),
                    cancellationToken).ConfigureAwait(false);
            }
            catch (LiteException ex)
            {
                LogError(ex, string.Empty);
            }

            return items ?? Array.Empty<T>();
        }

        public async Task<T> GetAsync(string key, bool includeExpired, CancellationToken cancellationToken)
        {
            var entity = await GetEntity(key, cancellationToken).ConfigureAwait(false);
            return entity != null && (includeExpired || !entity.HasExpired())
                ? entity.Contents
                : default;
        }

        public async Task<IList<T>> GetRangeAsync(IEnumerable<string> keys, bool includeExpired, CancellationToken cancellationToken)
        {
            var items = default(IList<T>);

            try
            {
                var bsonKeys = keys.Where(k => !string.IsNullOrWhiteSpace(k))
                                   .Select(k => new BsonValue(k)).ToList();
                var query = Query.In(IdColumn, bsonKeys);
                items = await _retryPolicy.ExecuteAsync(
                    async (ct) => await Task.Run(() => _col.Find(query)
                                                           .Where(i => includeExpired || !i.HasExpired())
                                                           .Select(i => i.Contents)
                                                           .ToList()).ConfigureAwait(false),
                    cancellationToken).ConfigureAwait(false);
            }
            catch (LiteException ex)
            {
                LogError(ex, string.Empty);
            }

            return items ?? Array.Empty<T>();
        }

        public async Task<IList<(string, ItemState)>> GetKeysAsync(CancellationToken cancellationToken)
        {
            var items = default(IList<(string, ItemState)>);

            try
            {
                items = await _retryPolicy.ExecuteAsync(
                    async (ct) => await Task.Run(() => _col.FindAll()
                                                           .Select(i => (i.Id, !i.HasExpired() ? ItemState.Active : ItemState.Expired))
                                                           .ToList()).ConfigureAwait(false),
                    cancellationToken).ConfigureAwait(false);
            }
            catch (LiteException ex)
            {
                LogError(ex, string.Empty);
            }

            return items ?? Array.Empty<(string, ItemState)>();
        }

        public async Task<DateTime?> GetExpirationAsync(string key, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Key can not be null or empty.", nameof(key));

            var item = await GetEntity(key, cancellationToken).ConfigureAwait(false);
            return item?.DateExpires.ToUniversalTime();
        }

        public async Task<int> CountAsync(CancellationToken cancellationToken)
        {
            var count = -1;

            try
            {
                count = await _retryPolicy.ExecuteAsync(
                    async (ct) => await Task.Run(() => _col.Count()).ConfigureAwait(false),
                    cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                LogError(ex, string.Empty);
            }

            return count;
        }

        #endregion

        #region Add Methods
        public async Task<bool> UpsertAsync(string key, T data, TimeSpan expireIn, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Key can not be null or empty.", nameof(key));

            if (data == null)
                throw new ArgumentNullException("Data can not be null.", nameof(data));

            var success = false;

            try
            {
                var item = new StoreItem<T>
                {
                    Id = key,
                    DateCreated = DateTime.UtcNow,
                    DateExpires = GetExpiration(expireIn),
                    Contents = data
                };

                success = await _retryPolicy.ExecuteAsync(
                    async (ct) => await Task.Run(() => _col.Upsert(item)).ConfigureAwait(false),
                    cancellationToken).ConfigureAwait(false);
            }
            catch (LiteException ex)
            {
                LogError(ex, key);
            }

            return success;
        }

        public async Task<bool> UpsertRangeAsync(IEnumerable<(string key, T data)> items, TimeSpan expireIn, CancellationToken cancellationToken)
        {
            var success = false;

            try
            {
                var storeItems = items
                    .Where(i => !string.IsNullOrWhiteSpace(i.key) && i.data != null)
                    .Select(i => new StoreItem<T>
                    {
                        Id = i.key,
                        DateCreated = DateTime.UtcNow,
                        DateExpires = GetExpiration(expireIn),
                        Contents = i.data
                    });

                await _retryPolicy.ExecuteAsync(
                    async (ct) => await Task.Run(() => _col.Upsert(storeItems)).ConfigureAwait(false),
                    cancellationToken).ConfigureAwait(false);

                success = true;
            }
            catch (LiteException ex)
            {
                LogError(ex, string.Empty);
            }

            return success;
        }

        public async Task<bool> UpdateAsync(string key, T data, TimeSpan expireIn, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Key can not be null or empty.", nameof(key));

            if (data == null)
                throw new ArgumentNullException("Data can not be null.", nameof(data));

            var updated = false;

            try
            {
                var item = new StoreItem<T>
                {
                    Id = key,
                    DateCreated = DateTime.UtcNow,
                    DateExpires = GetExpiration(expireIn),
                    Contents = data
                };

                updated = await _retryPolicy.ExecuteAsync(
                    async (ct) => await Task.Run(() => _col.Update(item)).ConfigureAwait(false),
                    cancellationToken).ConfigureAwait(false);
            }
            catch (LiteException ex)
            {
                LogError(ex, key);
            }

            return updated;
        }

        public async Task<bool> RemoveAsync(string key, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Key can not be null or empty.", nameof(key));

            var success = false;

            try
            {
                success = await _retryPolicy.ExecuteAsync(
                    async (ct) => await Task.Run(() => _col.Delete(key)).ConfigureAwait(false),
                    cancellationToken).ConfigureAwait(false);
            }
            catch (LiteException ex)
            {
                LogError(ex, key);
            }

            return success;
        }

        public async Task<bool> RemoveRangeAsync(IEnumerable<string> keys, CancellationToken cancellationToken)
        {
            var success = false;

            try
            {
                var bsonKeys = keys.Where(k => !string.IsNullOrEmpty(k))
                                   .Select(k => new BsonValue(k)).ToList();
                var query = Query.In(IdColumn, bsonKeys);
                var count = await _retryPolicy.ExecuteAsync(
                    async (ct) => await Task.Run(() => _col.DeleteMany(query)).ConfigureAwait(false),
                    cancellationToken).ConfigureAwait(false);

                success = true;
            }
            catch (LiteException ex)
            {
                LogError(ex, string.Empty);
            }

            return success;
        }
        #endregion

        #region Empty Methods
        public async Task<bool> EmptyExpiredAsync(CancellationToken cancellationToken)
        {
            var success = false;

            try
            {
                var deleted = await _retryPolicy.ExecuteAsync(
                    async (ct) => await Task.Run(() => _col.DeleteMany(i => i.DateExpires < DateTime.UtcNow)).ConfigureAwait(false),
                    cancellationToken).ConfigureAwait(false);
                success = true;
            }
            catch (LiteException ex)
            {
                LogError(ex, string.Empty);
            }

            return success;
        }

        public async Task<bool> EmptyAllAsync(CancellationToken cancellationToken)
        {
            var success = false;

            try
            {
                var deleted = await _retryPolicy.ExecuteAsync(
                    async (ct) => await Task.Run(() => _col.DeleteAll()).ConfigureAwait(false),
                    cancellationToken).ConfigureAwait(false);
                success = true;
            }
            catch (LiteException ex)
            {
                LogError(ex, string.Empty);
            }

            return success;
        }
        #endregion

        #region Private Methods
        private async Task<StoreItem<T>> GetEntity(string key, CancellationToken cancellationToken)
        {
            var item = default(StoreItem<T>);

            try
            {
                item = await _retryPolicy.ExecuteAsync(
                    async (ct) => await Task.Run(() => _col.FindById(key)).ConfigureAwait(false),
                    cancellationToken).ConfigureAwait(false);
            }
            catch (LiteException ex)
            {
                LogError(ex, key);
            }

            return item;
        }

        private void LogError(Exception ex, string key)
        {
            var data = new Dictionary<string, string>();

            data.Add("name", Name);
            data.Add("cache", _isCache.ToString());

            if (!string.IsNullOrEmpty(key))
                data.Add("key", key);

            _logger.Error(ex, data);
        }

        private static DateTime GetExpiration(TimeSpan timeSpan)
        {
            try
            {
                return DateTime.UtcNow.Add(timeSpan);
            }
            catch
            {
                if (timeSpan.Milliseconds < 0)
                    return DateTime.MinValue;

                return DateTime.MaxValue;
            }
        }

        private static string GetCollectionNameByType()
        {
            var dataType = typeof(T);
            var name = string.Empty;

            if (dataType.IsGenericType)
            {
                if ((dataType.GetGenericTypeDefinition() == typeof(List<>) ||
                     dataType.GetGenericTypeDefinition() == typeof(IList<>)) &&
                    !dataType.GenericTypeArguments[0].IsGenericType)
                {
                    name = $"List_{dataType.GenericTypeArguments[0].Name}";
                }
            }
            else
            {
                name = dataType.Name;
            }

            return name;
        }
        #endregion
    }
}