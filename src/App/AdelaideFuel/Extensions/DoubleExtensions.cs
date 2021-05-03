using System;

namespace AdelaideFuel
{
    public static class DoubleExtensions
    {
        public static string KmToString(this double km)
        {
            var result = string.Empty;

            if (km > 0)
            {
                var metres = km * 1000;
                var fuzzy = (int)(Math.Round(metres / 50, MidpointRounding.AwayFromZero) * 50);

                if (fuzzy < 1000)
                {
                    result = string.Format("{0}m", fuzzy);
                }
                else if (fuzzy < 100000)
                {
                    result = string.Format("{0:0.#}km", fuzzy / 1000d);
                }
                else
                {
                    result = string.Format("{0}km", (fuzzy / 1000));
                }
            }

            return result;
        }
    }
}