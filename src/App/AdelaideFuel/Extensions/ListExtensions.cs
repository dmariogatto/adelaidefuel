using System.Collections.Generic;

namespace AdelaideFuel
{
    public static class ListExtensions
    {
        public static void RemoveRange<T>(this IList<T> list, IEnumerable<T> range)
        {
            foreach (var i in range)
            {
                list.Remove(i);
            }
        }
    }
}