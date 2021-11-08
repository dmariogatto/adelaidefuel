using System;
using System.Collections.Generic;
using System.Linq;

namespace AdelaideFuel
{
    public static class Statistics
    {
        public const int MinLengthForFns = 2;

        public static double[] FiveNumberSummary(IList<double> values, bool sorted = true)
        {
            if (values.Count < MinLengthForFns) return new[] { 0d, 0d, 0d, 0d, 0d };
            if (!sorted)
            {
                switch (values)
                {
                    case double[] array:
                        Array.Sort(array);
                        break;
                    case List<double> list:
                        list.Sort();
                        break;
                    default:
                        values = values.OrderBy(i => i).ToList();
                        break;
                }
            }

            var percentages = new[] { 0d, 25d, 50d, 75d, 100d };
            var fns = new double[percentages.Length];

            for (var i = 0; i < percentages.Length; i++)
            {
                var p = percentages[i];

                if (p >= 100)
                {
                    fns[i] = values[values.Count - 1];
                    continue;
                }

                var position = (values.Count + 1) * p / 100d;
                var leftNum = 0d;
                var rightNum = 0d;

                var n = p / 100d * (values.Count - 1) + 1d;

                if (position >= 1)
                {
                    leftNum = values[(int)Math.Floor(n) - 1];
                    rightNum = values[(int)Math.Floor(n)];
                }
                else
                {
                    leftNum = values[0];
                    rightNum = values[1];
                }

                if (leftNum == rightNum)
                    fns[i] = leftNum;
                else
                {
                    var part = n - Math.Floor(n);
                    fns[i] = leftNum + part * (rightNum - leftNum);
                }
            }

            return fns;
        }
    }
}