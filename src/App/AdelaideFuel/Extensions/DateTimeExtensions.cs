using NodaTime;
using System;

namespace AdelaideFuel
{
    public static class DateTimeExtensions
    {
        public static DateTime LocaliseUtc(this DateTime dateTime)
        {
            if (dateTime.Kind == DateTimeKind.Local)
                return dateTime;

            var tz = DateTimeZoneProviders.Tzdb.GetSystemDefault();
            var instant = Instant.FromDateTimeUtc(DateTime.SpecifyKind(dateTime, DateTimeKind.Utc));
            var result = instant.InZone(tz).ToDateTimeUnspecified();
            return DateTime.SpecifyKind(result, DateTimeKind.Local);
        }
    }
}