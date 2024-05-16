using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace AdelaideFuel.Storage
{
    public enum ItemCacheState
    {
        None = 0,
        Expired = 1,
        Active = 2
    }

    public interface ICacheStore
    {
        string Name { get; }

        Task<bool> RemoveAsync(string key, CancellationToken cancellationToken);
        Task<int> RemoveRangeAsync(IEnumerable<string> keys, CancellationToken cancellationToken);

        Task<int> EmptyAllAsync(CancellationToken cancellationToken);
        Task<int> EmptyExpiredAsync(CancellationToken cancellationToken);

        Task<bool> ExistsAsync(string key, bool includeExpired, CancellationToken cancellationToken);
        Task<IList<(string, ItemCacheState)>> GetKeysAsync(CancellationToken cancellationToken);

        Task<DateTime?> GetExpirationAsync(string key, CancellationToken cancellationToken);

        Task<bool> AnyAsync(bool includeExpired, CancellationToken cancellationToken);
        Task<int> CountAsync(bool includeExpired, CancellationToken cancellationToken);
    }

    public interface ICacheStore<T> : ICacheStore where T : class
    {
        Task<bool> UpsertAsync(string key, T data, TimeSpan expireIn, CancellationToken cancellationToken);
        Task<int> UpsertRangeAsync(IEnumerable<(string key, T data)> items, TimeSpan expireIn, CancellationToken cancellationToken);
        Task<bool> UpdateAsync(string key, T data, TimeSpan expireIn, CancellationToken cancellationToken);
        Task<IList<T>> AllAsync(bool includeExpired, CancellationToken cancellationToken);
        Task<T> GetAsync(string key, bool includeExpired, CancellationToken cancellationToken);
        Task<IList<T>> GetRangeAsync(IEnumerable<string> keys, bool includeExpired, CancellationToken cancellationToken);
    }
}