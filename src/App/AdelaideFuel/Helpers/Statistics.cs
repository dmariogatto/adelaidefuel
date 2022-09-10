using System;
using System.Collections.Generic;
using System.Linq;

namespace AdelaideFuel
{
    public static class Statistics
    {
        public const int MinLengthForFns = 2;

        private static readonly double[] Percentages = new[] { 0.00d, 0.25d, 0.50d, 0.75d, 1.00d };

        public static double[] FiveNumberSummary(IList<double> values, bool sorted = true)
        {
            var fns = new[] { 0d, 0d, 0d, 0d, 0d };

            if (values.Count < MinLengthForFns)
                return fns;

            if (!sorted)
                values = values.OrderBy(i => i).ToList();

            fns[0] = values.First();
            fns[4] = values.Last();            

            for (var i = 1; i < Percentages.Length - 1; i++)
            {
                var position = (values.Count + 1) * Percentages[i] - 1;
                var idx = Math.Min(values.Count - 1, Math.Max(0, position));

                var leftNum = values[(int)Math.Floor(idx)];
                var rightNum = values[(int)Math.Ceiling(idx)];

                if (leftNum == rightNum)
                {
                    fns[i] = leftNum;
                }
                else
                {
                    fns[i] = (leftNum + rightNum) / 2d;
                }
            }

            return fns;
        }
    }
}