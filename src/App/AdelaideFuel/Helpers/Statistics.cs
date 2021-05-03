using System;

namespace AdelaideFuel
{
    public static class Statistics
    {
        public static double[] FiveNumberSummary(double[] array, bool sorted = true)
        {
            if (array.Length < 2) return new[] { 0d, 0d, 0d, 0d, 0d };
            if (!sorted) Array.Sort(array);

            var percentages = new[] { 0d, 25d, 50d, 75d, 100d };
            var quatiles = new double[percentages.Length];

            for (var i = 0; i < percentages.Length; i++)
            {
                var p = percentages[i];

                if (p >= 100)
                {
                    quatiles[i] = array[array.Length - 1];
                    continue;
                }

                var position = (array.Length + 1) * p / 100d;
                var leftNum = 0d;
                var rightNum = 0d;

                var n = p / 100d * (array.Length - 1) + 1d;

                if (position >= 1)
                {
                    leftNum = array[(int)Math.Floor(n) - 1];
                    rightNum = array[(int)Math.Floor(n)];
                }
                else
                {
                    leftNum = array[0];
                    rightNum = array[1];
                }

                if (leftNum == rightNum)
                    quatiles[i] = leftNum;
                else
                {
                    var part = n - Math.Floor(n);
                    quatiles[i] = leftNum + part * (rightNum - leftNum);
                }
            }

            return quatiles;
        }
    }
}