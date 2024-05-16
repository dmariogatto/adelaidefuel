using AdelaideFuel.Services;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace AdelaideFuel.Storage
{
    public class CacheFileStore<T> : ICacheStore<T> where T : class
    {
        private const string KeyNotEmptyExMsg = "Key can not be null or empty.";
        private static readonly DateTime Y2k = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        private readonly ILogger _logger;

        private readonly DirectoryInfo _directory;
        private readonly string _collectionName;

        private readonly SemaphoreSlim _semaphoreSlim = new SemaphoreSlim(1, 1);

        public CacheFileStore(string baseDirectory, ILogger logger, string collectionName = "")
        {
            _collectionName = collectionName;
            _logger = logger;

            if (string.IsNullOrEmpty(_collectionName))
            {
                _collectionName = GetCollectionNameByType();
            }

            if (string.IsNullOrEmpty(_collectionName))
                throw new ArgumentException($"Collection name cannot be empty (type: '{typeof(T).FullName}')");

            _directory = Directory.CreateDirectory(Path.Combine(baseDirectory, _collectionName));
        }

        public string Name => _collectionName;

        #region Exist and Expiration Methods
        public async Task<bool> ExistsAsync(string key, bool includeExpired, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException(KeyNotEmptyExMsg, nameof(key));

            var exists = false;

            await _semaphoreSlim.WaitAsync().ConfigureAwait(false);

            try
            {
                var fi = GetFile(key, includeExpired);
                exists = fi is not null;
            }
            catch (Exception ex)
            {
                LogError(ex, string.Empty);
            }
            finally
            {
                _semaphoreSlim.Release();
            }

            return exists;
        }
        #endregion

        #region Get Methods
        public async Task<IList<T>> AllAsync(bool includeExpired, CancellationToken cancellationToken)
        {
            var items = new List<T>();

            await _semaphoreSlim.WaitAsync().ConfigureAwait(false);

            try
            {
                var bag = new ConcurrentBag<T>();

                await Parallel.ForEachAsync(_directory.GetFiles(), cancellationToken, async (fi, ct) =>
                {
                    using var fs = fi.OpenRead();
                    bag.Add(await JsonSerializer.DeserializeAsync<T>(fs, cancellationToken: ct).ConfigureAwait(false));
                    fs.Close();
                }).ConfigureAwait(false);

                items.AddRange(bag);
            }
            catch (Exception ex)
            {
                LogError(ex, string.Empty);
            }
            finally
            {
                _semaphoreSlim.Release();
            }

            return items;
        }

        public async Task<T> GetAsync(string key, bool includeExpired, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException(KeyNotEmptyExMsg, nameof(key));

            var result = default(T);

            await _semaphoreSlim.WaitAsync().ConfigureAwait(false);

            try
            {
                var fi = GetFile(key, includeExpired);
                if (fi is not null)
                {
                    using var fs = fi.OpenRead();
                    result = await JsonSerializer.DeserializeAsync<T>(fs, cancellationToken: cancellationToken).ConfigureAwait(false);
                    fs.Close();
                }
            }
            catch (Exception ex)
            {
                LogError(ex, string.Empty);
            }
            finally
            {
                _semaphoreSlim.Release();
            }

            return result;
        }

        public async Task<IList<T>> GetRangeAsync(IEnumerable<string> keys, bool includeExpired, CancellationToken cancellationToken)
        {
            var items = new List<T>();

            await _semaphoreSlim.WaitAsync().ConfigureAwait(false);

            try
            {
                var bag = new ConcurrentBag<T>();
                var files = GetFiles(keys, includeExpired);

                await Parallel.ForEachAsync(files, cancellationToken, async (fi, ct) =>
                {
                    using var fs = fi.OpenRead();
                    bag.Add(await JsonSerializer.DeserializeAsync<T>(fs, cancellationToken: ct).ConfigureAwait(false));
                    fs.Close();
                }).ConfigureAwait(false);

                items.AddRange(bag);
            }
            catch (Exception ex)
            {
                LogError(ex, string.Empty);
            }
            finally
            {
                _semaphoreSlim.Release();
            }

            return items;
        }

        public async Task<IList<(string, ItemCacheState)>> GetKeysAsync(CancellationToken cancellationToken)
        {
            var items = default(IList<(string, ItemCacheState)>);

            await _semaphoreSlim.WaitAsync().ConfigureAwait(false);

            try
            {
                items = _directory
                    .GetFiles()
                    .Select(i => (Path.GetFileNameWithoutExtension(i.Name), !IsExpired(i.FullName) ? ItemCacheState.Active : ItemCacheState.Expired))
                    .ToList();
            }
            catch (Exception ex)
            {
                LogError(ex, string.Empty);
            }
            finally
            {
                _semaphoreSlim.Release();
            }

            return items ?? [];
        }

        public async Task<DateTime?> GetExpirationAsync(string key, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException(KeyNotEmptyExMsg, nameof(key));

            var expires = default(DateTime?);

            await _semaphoreSlim.WaitAsync().ConfigureAwait(false);

            try
            {
                var fi = GetFile(key, true);
                if (fi is not null)
                {
                    expires = GetExpiration(fi.FullName);
                }
            }
            catch (Exception ex)
            {
                LogError(ex, string.Empty);
            }
            finally
            {
                _semaphoreSlim.Release();
            }

            return expires;
        }

        public async Task<bool> AnyAsync(bool includeExpired, CancellationToken cancellationToken)
        {
            var exists = false;

            await _semaphoreSlim.WaitAsync().ConfigureAwait(false);

            try
            {
                exists = _directory
                    .EnumerateFiles()
                    .Where(i => includeExpired || !IsExpired(i.FullName))
                    .Any();
            }
            catch (Exception ex)
            {
                LogError(ex, string.Empty);
            }
            finally
            {
                _semaphoreSlim.Release();
            }

            return exists;
        }

        public async Task<int> CountAsync(bool includeExpired, CancellationToken cancellationToken)
        {
            var count = -1;

            await _semaphoreSlim.WaitAsync().ConfigureAwait(false);

            try
            {
                count = _directory
                    .EnumerateFiles()
                    .Count(i => includeExpired || !IsExpired(i.FullName));
            }
            catch (Exception ex)
            {
                LogError(ex, string.Empty);
            }
            finally
            {
                _semaphoreSlim.Release();
            }

            return count;
        }

        #endregion

        #region Add Methods
        public async Task<bool> UpsertAsync(string key, T data, TimeSpan expireIn, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException(KeyNotEmptyExMsg, nameof(key));

            if (data is null)
                throw new ArgumentNullException(nameof(data));

            var success = false;

            await _semaphoreSlim.WaitAsync().ConfigureAwait(false);

            try
            {
                var fi = new FileInfo(GetFilePath(_directory, key));
                using var fs = fi.OpenWrite();
                await JsonSerializer.SerializeAsync(fs, data, cancellationToken: cancellationToken).ConfigureAwait(false);
                fs.Close();

                File.SetLastWriteTimeUtc(fi.FullName, GetExpiration(expireIn));

                success = true;
            }
            catch (Exception ex)
            {
                LogError(ex, key);
            }
            finally
            {
                _semaphoreSlim.Release();
            }

            return success;
        }

        public async Task<int> UpsertRangeAsync(IEnumerable<(string key, T data)> items, TimeSpan expireIn, CancellationToken cancellationToken)
        {
            var count = 0;

            await _semaphoreSlim.WaitAsync().ConfigureAwait(false);

            try
            {
                var expiresIn = GetExpiration(expireIn);

                await Parallel.ForEachAsync(items, cancellationToken, async (i, ct) =>
                {
                    var fi = new FileInfo(GetFilePath(_directory, i.key));
                    using var fs = fi.OpenWrite();
                    await JsonSerializer.SerializeAsync(fs, i.data, cancellationToken: ct).ConfigureAwait(false);
                    fs.Close();

                    File.SetLastWriteTimeUtc(fi.FullName, expiresIn);

                    Interlocked.Increment(ref count);
                }).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                LogError(ex, string.Empty);
            }
            finally
            {
                _semaphoreSlim.Release();
            }

            return count;
        }

        public async Task<bool> UpdateAsync(string key, T data, TimeSpan expireIn, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException(KeyNotEmptyExMsg, nameof(key));

            if (data is null)
                throw new ArgumentNullException(nameof(data));

            var success = false;

            await _semaphoreSlim.WaitAsync().ConfigureAwait(false);

            try
            {
                var fi = new FileInfo(GetFilePath(_directory, key));
                if (fi.Exists)
                {
                    using var fs = fi.OpenWrite();
                    await JsonSerializer.SerializeAsync(fs, data, cancellationToken: cancellationToken).ConfigureAwait(false);
                    fs.Close();

                    File.SetLastWriteTimeUtc(fi.FullName, GetExpiration(expireIn));

                    success = true;
                }
            }
            catch (Exception ex)
            {
                LogError(ex, key);
            }
            finally
            {
                _semaphoreSlim.Release();
            }

            return success;
        }

        public async Task<bool> RemoveAsync(string key, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException(KeyNotEmptyExMsg, nameof(key));

            var success = false;

            await _semaphoreSlim.WaitAsync().ConfigureAwait(false);

            try
            {
                var fi = new FileInfo(GetFilePath(_directory, key));
                if (fi.Exists)
                {
                    fi.Delete();
                    success = true;
                }
            }
            catch (Exception ex)
            {
                LogError(ex, key);
            }
            finally
            {
                _semaphoreSlim.Release();
            }

            return success;
        }

        public async Task<int> RemoveRangeAsync(IEnumerable<string> keys, CancellationToken cancellationToken)
        {
            var count = 0;

            await _semaphoreSlim.WaitAsync().ConfigureAwait(false);

            try
            {
                await Parallel.ForEachAsync(keys, cancellationToken, (k, ct) =>
                {
                    var fi = new FileInfo(GetFilePath(_directory, k));
                    if (fi.Exists)
                    {
                        fi.Delete();
                        Interlocked.Increment(ref count);
                    }

                    return ValueTask.CompletedTask;
                }).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                LogError(ex, string.Empty);
            }
            finally
            {
                _semaphoreSlim.Release();
            }

            return count;
        }
        #endregion

        #region Empty Methods
        public async Task<int> EmptyExpiredAsync(CancellationToken cancellationToken)
        {
            var count = 0;

            await _semaphoreSlim.WaitAsync().ConfigureAwait(false);

            try
            {
                await Parallel.ForEachAsync(_directory.GetFiles(), cancellationToken, (fi, ct) =>
                {
                    if (IsExpired(fi.FullName))
                    {
                        fi.Delete();
                        Interlocked.Increment(ref count);
                    }

                    return ValueTask.CompletedTask;
                }).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                LogError(ex, string.Empty);
            }
            finally
            {
                _semaphoreSlim.Release();
            }

            return count;
        }

        public async Task<int> EmptyAllAsync(CancellationToken cancellationToken)
        {
            var count = 0;

            await _semaphoreSlim.WaitAsync().ConfigureAwait(false);

            try
            {
                await Parallel.ForEachAsync(_directory.GetFiles(), cancellationToken, (fi, ct) =>
                {
                    fi.Delete();
                    Interlocked.Increment(ref count);

                    return ValueTask.CompletedTask;
                }).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                LogError(ex, string.Empty);
            }
            finally
            {
                _semaphoreSlim.Release();
            }

            return count;
        }
        #endregion

        #region Private Methods
        private FileInfo GetFile(string key, bool includeExpired)
        {
            var filePath = GetFilePath(_directory, key);
            if (File.Exists(filePath) && (includeExpired || !IsExpired(filePath)))
            {
                return new FileInfo(filePath);
            }

            return default;
        }

        private List<FileInfo> GetFiles(IEnumerable<string> keys, bool includeExpired)
        {
            var files = new List<FileInfo>();

            foreach (var k in keys)
            {
                var filePath = GetFilePath(_directory, k);
                if (File.Exists(filePath) && (includeExpired || !IsExpired(filePath)))
                {
                    files.Add(new FileInfo(filePath));
                }
            }

            return files;
        }

        private void LogError(Exception ex, string key)
        {
            var data = new Dictionary<string, string>
            {
                { "name", Name },
                { "path", _directory.FullName }
            };

            if (!string.IsNullOrEmpty(key))
                data.Add("key", key);

            _logger.Error(ex, data);
        }

        private static string GetFilePath(DirectoryInfo di, string key)
            => Path.Combine(di.FullName, $"{key}.json");

        private static DateTime GetExpiration(TimeSpan timeSpan)
        {
            if (timeSpan == TimeSpan.MaxValue)
                return Y2k;

            return DateTime.UtcNow.Add(timeSpan);
        }

        private static DateTime GetExpiration(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                return DateTime.MinValue;

            var modified = File.GetLastWriteTimeUtc(filePath);
            return modified == Y2k ? DateTime.MaxValue : modified;
        }

        private static bool IsExpired(string filePath)
        {
            var modified = File.GetLastWriteTimeUtc(filePath);
            return modified != Y2k && modified < DateTime.UtcNow;
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