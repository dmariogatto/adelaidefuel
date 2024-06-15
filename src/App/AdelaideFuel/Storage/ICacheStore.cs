using System;
using System.Collections.Generic;

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

        bool Remove(string key);
        int RemoveRange(IEnumerable<string> keys);

        int EmptyAll();
        int EmptyExpired();

        bool Exists(string key, bool includeExpired);
        IReadOnlyCollection<(string, ItemCacheState)> GetKeys();

        DateTime? GetExpiration(string key);

        bool Any(bool includeExpired);
        int Count(bool includeExpired);
    }

    public interface ICacheStore<T> : ICacheStore where T : class
    {
        bool Upsert(string key, T data, TimeSpan expireIn);
        int UpsertRange(IEnumerable<(string key, T data)> items, TimeSpan expireIn);
        bool Update(string key, T data, TimeSpan expireIn);
        IReadOnlyList<T> All(bool includeExpired);
        T Get(string key, bool includeExpired);
        IReadOnlyList<T> GetRange(IEnumerable<string> keys, bool includeExpired);
    }
}