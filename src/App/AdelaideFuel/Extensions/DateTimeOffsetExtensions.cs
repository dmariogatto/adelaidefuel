using NodaTime;
using System;

namespace AdelaideFuel
{
    public static class DateTimeOffsetExtensions
    {
        public static DateTimeOffset Localise(this DateTimeOffset dateTimeOffset)
        {
            var tz = DateTimeZoneProviders.Tzdb.GetSystemDefault();
            var instant = Instant.FromDateTimeOffset(dateTimeOffset);
            var result = instant.InZone(tz).ToDateTimeOffset();
            return result;
        }
    }
}