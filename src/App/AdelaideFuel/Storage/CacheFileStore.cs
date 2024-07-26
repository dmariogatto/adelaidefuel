using AdelaideFuel.Services;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
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

        private readonly object _lock = new object();

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
        public bool Exists(string key, bool includeExpired)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException(KeyNotEmptyExMsg, nameof(key));

            var exists = false;

            lock (_lock)
            {
                try
                {
                    var fi = GetFile(key, includeExpired);
                    exists = fi is not null;
                }
                catch (Exception ex)
                {
                    LogError(ex, string.Empty);
                }
            }

            return exists;
        }
        #endregion

        #region Get Methods
        public IReadOnlyList<T> All(bool includeExpired)
        {
            var items = new ConcurrentBag<T>();

            lock (_lock)
            {
                try
                {
                    Parallel.ForEach(_directory.GetFiles(), (fi) =>
                    {
                        using var fs = fi.OpenRead();
                        items.Add(JsonSerializer.Deserialize<T>(fs));
                        fs.Close();
                    });
                }
                catch (Exception ex)
                {
                    LogError(ex, string.Empty);
                    items.Clear();
                }
            }

            return [.. items];
        }

        public T Get(string key, bool includeExpired)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException(KeyNotEmptyExMsg, nameof(key));

            var result = default(T);

            lock (_lock)
            {
                try
                {
                    var fi = GetFile(key, includeExpired);
                    if (fi is not null)
                    {
                        using var fs = fi.OpenRead();
                        result = JsonSerializer.Deserialize<T>(fs);
                        fs.Close();
                    }
                }
                catch (Exception ex)
                {
                    LogError(ex, key);
                }
            }

            return result;
        }

        public IReadOnlyList<T> GetRange(IEnumerable<string> keys, bool includeExpired)
        {
            var items = new ConcurrentBag<T>();

            lock (_lock)
            {
                try
                {
                    var files = GetFiles(keys, includeExpired);

                    Parallel.ForEach(files, (fi) =>
                    {
                        using var fs = fi.OpenRead();
                        items.Add(JsonSerializer.Deserialize<T>(fs));
                        fs.Close();
                    });
                }
                catch (Exception ex)
                {
                    LogError(ex, string.Empty);
                    items.Clear();
                }
            }

            return [.. items];
        }

        public IReadOnlyCollection<(string, ItemCacheState)> GetKeys()
        {
            var items = default(List<(string, ItemCacheState)>);

            lock (_lock)
            {
                try
                {
                    items = _directory
                        .GetFiles()
                        .Select(i => (Path.GetFileNameWithoutExtension(i.Name), !IsFileExpired(i.FullName) ? ItemCacheState.Active : ItemCacheState.Expired))
                        .ToList();
                }
                catch (Exception ex)
                {
                    LogError(ex, string.Empty);
                }
            }

            return items ?? [];
        }

        public DateTime? GetExpiration(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException(KeyNotEmptyExMsg, nameof(key));

            var expires = default(DateTime?);

            lock (_lock)
            {
                try
                {
                    var fi = GetFile(key, true);
                    if (fi is not null)
                    {
                        expires = GetFileExpiration(fi.FullName);
                    }
                }
                catch (Exception ex)
                {
                    LogError(ex, key);
                }
            }

            return expires;
        }

        public bool Any(bool includeExpired)
        {
            var exists = false;

            lock (_lock)
            {
                try
                {
                    exists = _directory
                        .EnumerateFiles()
                        .Where(i => includeExpired || !IsFileExpired(i.FullName))
                        .Any();
                }
                catch (Exception ex)
                {
                    LogError(ex, string.Empty);
                }

            }

            return exists;
        }

        public int Count(bool includeExpired)
        {
            var count = -1;

            lock (_lock)
            {
                try
                {
                    count = _directory
                        .EnumerateFiles()
                        .Count(i => includeExpired || !IsFileExpired(i.FullName));
                }
                catch (Exception ex)
                {
                    LogError(ex, string.Empty);
                }
            }

            return count;
        }

        #endregion

        #region Add Methods
        public bool Upsert(string key, T data, TimeSpan expireIn)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException(KeyNotEmptyExMsg, nameof(key));

            if (data is null)
                throw new ArgumentNullException(nameof(data));

            var success = false;

            lock (_lock)
            {
                try
                {
                    var fi = new FileInfo(GetFilePath(_directory, key));
                    using var fs = fi.Open(FileMode.Create, FileAccess.Write);
                    JsonSerializer.Serialize(fs, data);
                    fs.Close();

                    File.SetLastWriteTimeUtc(fi.FullName, GetExpectedExpiration(expireIn));

                    success = true;
                }
                catch (Exception ex)
                {
                    LogError(ex, key);
                }
            }

            return success;
        }

        public int UpsertRange(IEnumerable<(string key, T data)> items, TimeSpan expireIn)
        {
            var count = 0;

            lock (_lock)
            {
                try
                {
                    var expiresIn = GetExpectedExpiration(expireIn);

                    Parallel.ForEach(items, (i) =>
                    {
                        var fi = new FileInfo(GetFilePath(_directory, i.key));
                        using var fs = fi.Open(FileMode.Create, FileAccess.Write);
                        JsonSerializer.Serialize(fs, i.data);
                        fs.Close();

                        File.SetLastWriteTimeUtc(fi.FullName, expiresIn);

                        Interlocked.Increment(ref count);
                    });
                }
                catch (Exception ex)
                {
                    LogError(ex, string.Empty);
                }
            }

            return count;
        }

        public bool Update(string key, T data, TimeSpan expireIn)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException(KeyNotEmptyExMsg, nameof(key));

            if (data is null)
                throw new ArgumentNullException(nameof(data));

            var success = false;

            lock (_lock)
            {
                try
                {
                    var fi = new FileInfo(GetFilePath(_directory, key));
                    if (fi.Exists)
                    {
                        using var fs = fi.Open(FileMode.Create, FileAccess.Write);
                        JsonSerializer.Serialize(fs, data);
                        fs.Close();

                        File.SetLastWriteTimeUtc(fi.FullName, GetExpectedExpiration(expireIn));

                        success = true;
                    }
                }
                catch (Exception ex)
                {
                    LogError(ex, key);
                }
            }

            return success;
        }

        public bool Remove(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException(KeyNotEmptyExMsg, nameof(key));

            var success = false;

            lock (_lock)
            {
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
            }

            return success;
        }

        public int RemoveRange(IEnumerable<string> keys)
        {
            var count = 0;

            lock (_lock)
            {
                try
                {
                    Parallel.ForEach(keys, (k) =>
                    {
                        var fi = new FileInfo(GetFilePath(_directory, k));
                        if (fi.Exists)
                        {
                            fi.Delete();
                            Interlocked.Increment(ref count);
                        }
                    });
                }
                catch (Exception ex)
                {
                    LogError(ex, string.Empty);
                }
            }

            return count;
        }
        #endregion

        #region Empty Methods
        public int EmptyExpired()
        {
            var count = 0;

            lock (_lock)
            {
                try
                {
                    Parallel.ForEach(_directory.GetFiles(), (fi) =>
                    {
                        if (IsFileExpired(fi.FullName))
                        {
                            fi.Delete();
                            Interlocked.Increment(ref count);
                        }
                    });
                }
                catch (Exception ex)
                {
                    LogError(ex, string.Empty);
                }
            }

            return count;
        }

        public int EmptyAll()
        {
            var count = 0;

            lock (_lock)
            {
                try
                {
                    Parallel.ForEach(_directory.GetFiles(), (fi) =>
                    {
                        fi.Delete();
                        Interlocked.Increment(ref count);
                    });
                }
                catch (Exception ex)
                {
                    LogError(ex, string.Empty);
                }
            }

            return count;
        }
        #endregion

        #region Private Methods
        private FileInfo GetFile(string key, bool includeExpired)
        {
            var filePath = GetFilePath(_directory, key);
            if (File.Exists(filePath) && (includeExpired || !IsFileExpired(filePath)))
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
                if (File.Exists(filePath) && (includeExpired || !IsFileExpired(filePath)))
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

        private static DateTime GetExpectedExpiration(TimeSpan timeSpan)
        {
            if (timeSpan == TimeSpan.MaxValue)
                return Y2k;

            return DateTime.UtcNow.Add(timeSpan);
        }

        private static DateTime GetFileExpiration(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                return DateTime.MinValue;

            var modified = File.GetLastWriteTimeUtc(filePath);
            return modified == Y2k ? DateTime.MaxValue : modified;
        }

        private static bool IsFileExpired(string filePath)
        {
            var modified = File.GetLastWriteTimeUtc(filePath);
            return modified != Y2k && modified < DateTime.UtcNow;
        }

        private static string GetCollectionNameByType()
        {
            var sb = new StringBuilder();

            void AppendTypeName(Type t)
            {
                if (t.IsGenericType)
                {
                    var genericTypeName = t.GetGenericTypeDefinition().Name;
                    var unmangledName = genericTypeName[..genericTypeName.IndexOf('`')];
                    sb.Append(unmangledName);
                    sb.Append("__");
                    var genericArguments = t.GetGenericArguments();
                    for (var i = 0; i < genericArguments.Length; i++)
                    {
                        if (i > 0)
                            sb.Append('_');
                        AppendTypeName(genericArguments[i]);
                    }
                    sb.Append("__");
                }
                else
                {
                    sb.Append(t.Name);
                }
            }

            AppendTypeName(typeof(T));

            return sb.ToString();
        }
        #endregion
    }
}