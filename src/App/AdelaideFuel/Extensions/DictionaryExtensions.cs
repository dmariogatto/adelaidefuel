using System.Collections.Generic;

namespace AdelaideFuel
{
    public static class DictionaryExtensions
    {
        public static bool DictionaryEqual<K, V>(this IDictionary<K, V> first, IDictionary<K, V> second)
            => first.DictionaryEqual(second, null);

        public static bool DictionaryEqual<K, V>(this IDictionary<K, V> first, IDictionary<K, V> second, IEqualityComparer<V> valueComparer)
        {
            if (first == second)
                return true;
            if (first == null || second == null)
                return false;
            if (first.Count != second.Count)
                return false;

            valueComparer ??= EqualityComparer<V>.Default;

            foreach (var kv in first)
            {
                if (!second.TryGetValue(kv.Key, out var secondValue)) return false;
                if (!valueComparer.Equals(kv.Value, secondValue)) return false;
            }

            return true;
        }
    }
}