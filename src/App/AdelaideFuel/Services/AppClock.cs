using NodaTime;
using System;

namespace AdelaideFuel.Services
{
    public class AppClock : IAppClock
    {
        private const string AdelaideTimeZoneId = "Australia/Adelaide";

        private readonly DateTimeZone _adelaideTimeZone;

        public AppClock()
        {
            _adelaideTimeZone = DateTimeZoneProviders.Tzdb.GetZoneOrNull(AdelaideTimeZoneId) ?? throw new ArgumentNullException($"Cannot find timezone '{AdelaideTimeZoneId}'");
        }

        public DateTime AdelaideNow =>
            SystemClock
                .Instance
                .GetCurrentInstant()
                .InZone(_adelaideTimeZone)
                .ToDateTimeUnspecified();

        public TimeSpan AdelaideUtcOffset =>
            _adelaideTimeZone
                .GetUtcOffset(SystemClock.Instance.GetCurrentInstant())
                .ToTimeSpan();

        public DateTime Today => LocalNow.Date;
        public TimeSpan TimeOfDay => LocalNow.TimeOfDay;

        public DateTime UtcNow => DateTime.UtcNow;
        public DateTime LocalNow => ToLocal(DateTime.UtcNow);

        public DateTimeOffset UtcNowOffset => DateTimeOffset.UtcNow;
        public DateTimeOffset LocalNowOffset => ToLocal(DateTimeOffset.UtcNow);

        public TimeSpan LocalUtcOffset =>
            DateTimeZoneProviders
                .Tzdb
                .GetSystemDefault()
                .GetUtcOffset(SystemClock.Instance.GetCurrentInstant())
                .ToTimeSpan();

        public DateTime ToUniversal(DateTime dateTime)
        {
            if (dateTime.Kind == DateTimeKind.Utc)
                return dateTime;

            var ldt = LocalDateTime.FromDateTime(dateTime);
            var zdt = ldt.InZoneLeniently(DateTimeZoneProviders.Tzdb.GetSystemDefault());
            var result = zdt.ToDateTimeUtc();
            return result;
        }

        public DateTime ToLocal(DateTime dateTime)
        {
            if (dateTime.Kind == DateTimeKind.Local)
                return dateTime;

            var tz = DateTimeZoneProviders.Tzdb.GetSystemDefault();
            var instant = Instant.FromDateTimeUtc(DateTime.SpecifyKind(dateTime, DateTimeKind.Utc));
            var result = instant.InZone(tz).ToDateTimeUnspecified();
            return DateTime.SpecifyKind(result, DateTimeKind.Local);
        }

        public DateTimeOffset ToLocal(DateTimeOffset dateTimeOffset)
        {
            var tz = DateTimeZoneProviders.Tzdb.GetSystemDefault();
            var instant = Instant.FromDateTimeOffset(dateTimeOffset);
            var result = instant.InZone(tz).ToDateTimeOffset();
            return result;
        }
    }
}
