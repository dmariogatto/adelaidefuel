using AdelaideFuel.Models;
using AdelaideFuel.Services;
using Microsoft.Maui.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace AdelaideFuel.Storage
{
    public class UserPrefStore<T> : IUserStore<T> where T : class, IUserEntity
    {
        private readonly string _key;
        private readonly string _sharedName;

        private readonly IPreferences _preferences;
        private readonly ILogger _logger;

        private readonly object _lock = new();

        public UserPrefStore(
            string key,
            string sharedName,
            IPreferences preferences,
            ILogger logger)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(key);

            _key = key;
            _sharedName = sharedName;

            _preferences = preferences;
            _logger = logger;
        }

        public string Name => typeof(T).Name;

        public IReadOnlyList<T> All()
        {
            lock (_lock)
            {
                return GetAllItems();
            }
        }

        public T Get(int id)
        {
            lock (_lock)
            {
                var items = GetAllItems();
                return items.FirstOrDefault(i => i.Id == id);
            }
        }

        public bool Upsert(T item)
        {
            lock (_lock)
            {
                var success = false;
                var items = GetAllItems();

                for (var i = 0; i <= items.Count && !success; i++)
                {
                    if (i == items.Count)
                    {
                        items.Add(item);
                        success = true;
                    }
                    else if (items[i].Id == item.Id)
                    {
                        items[i] = item;
                        success = true;
                    }
                }

                _preferences.Set(_key, JsonSerializer.Serialize(items), _sharedName);
                return success;
            }
        }

        public bool Update(T item)
        {
            lock (_lock)
            {
                var success = false;
                var items = GetAllItems();

                for (var i = 0; i < items.Count && !success; i++)
                {
                    if (items[i].Id == item.Id)
                    {
                        items[i] = item;
                        success = true;
                    }
                }

                _preferences.Set(_key, JsonSerializer.Serialize(items), _sharedName);
                return success;
            }
        }

        public bool Remove(int id)
        {
            lock (_lock)
            {
                var success = false;
                var items = GetAllItems();

                for (var i = 0; i < items.Count && !success; i++)
                {
                    if (items[i].Id == id)
                    {
                        items.RemoveAt(i);
                        success = true;
                    }
                }

                _preferences.Set(_key, JsonSerializer.Serialize(items), _sharedName);
                return success;
            }
        }

        public int UpsertRange(IEnumerable<T> toUpsert)
        {
            lock (_lock)
            {
                var count = 0;

                var success = false;
                var result = GetAllItems();

                foreach (var item in toUpsert)
                {
                    success = false;

                    for (var i = 0; i <= result.Count && !success; i++)
                    {
                        if (i == result.Count)
                        {
                            result.Add(item);
                            success = true;
                        }
                        else if (result[i].Id == item.Id)
                        {
                            result[i] = item;
                            success = true;
                        }
                    }

                    if (success)
                        count++;
                }

                _preferences.Set(_key, JsonSerializer.Serialize(result), _sharedName);
                return count;
            }
        }

        public int RemoveRange(IEnumerable<T> toRemove)
        {
            lock (_lock)
            {
                var count = 0;

                var success = false;
                var result = GetAllItems();

                foreach (var item in toRemove)
                {
                    success = false;

                    for (var i = 0; i < result.Count && !success; i++)
                    {
                        if (result[i].Id == item.Id)
                        {
                            result.RemoveAt(i);
                            count++;
                            success = true;
                        }
                    }
                }

                _preferences.Set(_key, JsonSerializer.Serialize(result), _sharedName);
                return count;
            }
        }

        private List<T> GetAllItems()
        {
            var data = _preferences.Get(_key, string.Empty, _sharedName);
            if (string.IsNullOrEmpty(data))
                return [];

            return JsonSerializer.Deserialize<List<T>>(data);
        }
    }
}