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
        private const string DateExpiresColumn = nameof(StoreItem<int>.DateExpires);

        private const string KeyNotEmptyExMsg = "Key can not be null or empty.";

        private readonly ILogger _logger;

        private readonly LiteDatabase _db;
        private readonly ILiteCollection<StoreItem<T>> _col;
        private readonly bool _isCache;

        private readonly RetryPolicy _retryPolicy =
               Policy.Handle<LiteException>()
                     .WaitAndRetry
                       (
                           retryCount: 3,
                           sleepDurationProvider: retryAttempt => TimeSpan.FromMilliseconds(6 * Math.Pow(2, retryAttempt))
                       );

        private readonly AsyncRetryPolicy _retryPolicyAsync =
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

            try
            {
                _retryPolicy.Execute(() => _col.EnsureIndex(i => i.DateExpires));
            }
            catch (LiteException ex)
            {
                LogError(ex, string.Empty);
            }
        }

        public string Name { get => _col?.Name ?? string.Empty; }

        #region Exist and Expiration Methods
        public async Task<bool> ExistsAsync(string key, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException(KeyNotEmptyExMsg, nameof(key));

            var item = await GetAsync(key, true, cancellationToken).ConfigureAwait(false);
            return item != null;
        }

        public async Task<bool> IsExpiredAsync(string key, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException(KeyNotEmptyExMsg, nameof(key));

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
                items = await _retryPolicyAsync.ExecuteAsync(
                    async (ct) => await Task.Run(() => (includeExpired
                                                        ? _col.FindAll()
                                                        : _col.Find(Query.GT(DateExpiresColumn, DateTime.UtcNow)))
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
                                   .Select(k => new BsonValue(k))
                                   .ToList();

                var query = Query.In(IdColumn, bsonKeys);
                if (!includeExpired)
                    query = Query.And(query, Query.GT(DateExpiresColumn, DateTime.UtcNow));

                items = await _retryPolicyAsync.ExecuteAsync(
                    async (ct) => await Task.Run(() => _col.Find(query)
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
                items = await _retryPolicyAsync.ExecuteAsync(
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
                throw new ArgumentException(KeyNotEmptyExMsg, nameof(key));

            var item = await GetEntity(key, cancellationToken).ConfigureAwait(false);
            return item?.DateExpires.ToUniversalTime();
        }

        public async Task<int> CountAsync(bool includeExpired, CancellationToken cancellationToken)
        {
            var count = -1;

            try
            {
                count = await _retryPolicyAsync.ExecuteAsync(
                    async (ct) => await Task.Run(() => includeExpired
                                                       ? _col.Count()
                                                       : _col.Count(Query.GT(DateExpiresColumn, DateTime.UtcNow))).ConfigureAwait(false),
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
                throw new ArgumentException(KeyNotEmptyExMsg, nameof(key));

            if (data == null)
                throw new ArgumentNullException(nameof(data));

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

                await _retryPolicyAsync.ExecuteAsync(
                    async (ct) => await Task.Run(() => _col.Upsert(item)).ConfigureAwait(false),
                    cancellationToken).ConfigureAwait(false);

                success = true;
            }
            catch (LiteException ex)
            {
                LogError(ex, key);
            }

            return success;
        }

        public async Task<int> UpsertRangeAsync(IEnumerable<(string key, T data)> items, TimeSpan expireIn, CancellationToken cancellationToken)
        {
            var count = -1;

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

                count = await _retryPolicyAsync.ExecuteAsync(
                    async (ct) => await Task.Run(() => _col.Upsert(storeItems)).ConfigureAwait(false),
                    cancellationToken).ConfigureAwait(false);
            }
            catch (LiteException ex)
            {
                LogError(ex, string.Empty);
            }

            return count;
        }

        public async Task<bool> UpdateAsync(string key, T data, TimeSpan expireIn, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException(KeyNotEmptyExMsg, nameof(key));

            if (data == null)
                throw new ArgumentNullException(nameof(data));

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

                await _retryPolicyAsync.ExecuteAsync(
                    async (ct) => await Task.Run(() => _col.Update(item)).ConfigureAwait(false),
                    cancellationToken).ConfigureAwait(false);

                success = true;
            }
            catch (LiteException ex)
            {
                LogError(ex, key);
            }

            return success;
        }

        public async Task<bool> RemoveAsync(string key, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException(KeyNotEmptyExMsg, nameof(key));

            var success = false;

            try
            {
                await _retryPolicyAsync.ExecuteAsync(
                    async (ct) => await Task.Run(() => _col.Delete(key)).ConfigureAwait(false),
                    cancellationToken).ConfigureAwait(false);

                success = true;
            }
            catch (LiteException ex)
            {
                LogError(ex, key);
            }

            return success;
        }

        public async Task<int> RemoveRangeAsync(IEnumerable<string> keys, CancellationToken cancellationToken)
        {
            var count = -1;

            try
            {
                var bsonKeys = keys.Where(k => !string.IsNullOrEmpty(k))
                                   .Select(k => new BsonValue(k)).ToList();
                var query = Query.In(IdColumn, bsonKeys);
                count = await _retryPolicyAsync.ExecuteAsync(
                    async (ct) => await Task.Run(() => _col.DeleteMany(query)).ConfigureAwait(false),
                    cancellationToken).ConfigureAwait(false);
            }
            catch (LiteException ex)
            {
                LogError(ex, string.Empty);
            }

            return count;
        }
        #endregion

        #region Empty Methods
        public async Task<int> EmptyExpiredAsync(CancellationToken cancellationToken)
        {
            var count = -1;

            try
            {
                count = await _retryPolicyAsync.ExecuteAsync(
                    async (ct) => await Task.Run(() => _col.DeleteMany(i => i.DateExpires < DateTime.UtcNow)).ConfigureAwait(false),
                    cancellationToken).ConfigureAwait(false);
            }
            catch (LiteException ex)
            {
                LogError(ex, string.Empty);
            }

            return count;
        }

        public async Task<int> EmptyAllAsync(CancellationToken cancellationToken)
        {
            var count = -1;

            try
            {
                count = await _retryPolicyAsync.ExecuteAsync(
                    async (ct) => await Task.Run(() => _col.DeleteAll()).ConfigureAwait(false),
                    cancellationToken).ConfigureAwait(false);
            }
            catch (LiteException ex)
            {
                LogError(ex, string.Empty);
            }

            return count;
        }
        #endregion

        #region Private Methods
        private async Task<StoreItem<T>> GetEntity(string key, CancellationToken cancellationToken)
        {
            var item = default(StoreItem<T>);

            try
            {
                item = await _retryPolicyAsync.ExecuteAsync(
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
            if (timeSpan == TimeSpan.MaxValue)
                return DateTime.MaxValue;

            return DateTime.UtcNow.Add(timeSpan);
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