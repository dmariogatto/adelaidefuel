using System;
using System.Globalization;

namespace AdelaideFuel
{
    public static class DtfInfoExtensions
    {
        public static bool Is24Hour(this DateTimeFormatInfo dtf)
         => dtf.LongTimePattern.Contains("H");

        public static void SetTimePatterns(this DateTimeFormatInfo dtf, bool is24Hour)
        {
            var ltp = is24Hour ? "HH:mm:ss" : "h:mm:ss tt";
            var stp = is24Hour ? "HH:mm" : "h:mm tt";

            dtf.LongTimePattern = ltp;
            dtf.ShortTimePattern = stp;

            var hour = is24Hour ? "h" : "H";

            var idx = dtf.FullDateTimePattern.IndexOf(hour, StringComparison.Ordinal);

            if (idx > -1)
            {
                dtf.FullDateTimePattern = dtf.FullDateTimePattern.Substring(0, idx) + dtf.LongTimePattern;
            }
        }
    }
}