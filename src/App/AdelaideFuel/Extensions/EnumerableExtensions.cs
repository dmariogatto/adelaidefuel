using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AdelaideFuel
{
    public static class EnumerableExtensions
    {
        public static void ForEach<T>(this IEnumerable<T> enumerable, Action<T> action)
        {
            foreach (var i in enumerable)
            {
                action.Invoke(i);
            }
        }

        public static void ForEach<T>(this IEnumerable<T> enumerable, Action<int, T> action)
        {
            var idx = 0;

            foreach (var i in enumerable)
            {
                action.Invoke(idx, i);
                idx++;
            }
        }

        public static int FirstIndexOf<T>(this IEnumerable<T> enumerable, Func<T, bool> predicate)
        {
            var items = enumerable.ToList();

            for (var i = 0; i < items.Count; i++)
            {
                if (predicate(items[i]))
                    return i;
            }

            return -1;
        }
    }
}