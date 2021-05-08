using System;

namespace AdelaideFuel.Shared
{
    public struct OpeningHour
    {
        private readonly static TimeSpan FullDay = TimeSpan.FromHours(24);
        private readonly static TimeSpan EndOfDay = new TimeSpan(23, 59, 00);

        public OpeningHour(DayOfWeek day, string open, string close)
        {
            Day = day;

            TimeSpan.TryParse(open ?? "00:00", out var openTime);
            TimeSpan.TryParse(close ?? "00:00", out var closeTime);

            Open = openTime;
            Close = closeTime;

            if (Close == EndOfDay)
                Length = FullDay - Open;
            else if (Close < Open)
                Length = Close + FullDay - Open;
            else
                Length = Close - Open;
        }

        public DayOfWeek Day { get; }
        public TimeSpan Open { get; }
        public TimeSpan Close { get; }

        public TimeSpan Length { get; }

        public bool IsOpen(TimeSpan timeOfDay)
        {
            if (OpenAllDay()) return true;
            if (ClosedAllDay()) return false;
            if (timeOfDay >= Open && (timeOfDay < Close || Close == TimeSpan.Zero)) return true;
            return false;
        }

        public bool OpenAllDay() => Length == FullDay;
        public bool ClosedAllDay() => Length == TimeSpan.Zero;
    }
}