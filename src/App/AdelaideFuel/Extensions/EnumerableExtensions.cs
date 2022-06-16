using System;
using System.Collections.Generic;

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
            var idx = 0;

            foreach (var i in enumerable)
            {
                if (predicate(i))
                    return idx;

                idx++;
            }

            return -1;
        }
    }
}