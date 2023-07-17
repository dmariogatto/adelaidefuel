using System;

namespace AdelaideFuel.Services
{
    public interface IAppClock
    {
        DateTime AdelaideNow { get; }
        TimeSpan AdelaideUtcOffset { get; }

        DateTime Today { get; }
        TimeSpan TimeOfDay { get; }

        DateTime UtcNow { get; }
        DateTime LocalNow { get; }

        DateTimeOffset UtcNowOffset { get; }
        DateTimeOffset LocalNowOffset { get; }

        DateTime ToUniversal(DateTime dateTime);

        DateTime ToLocal(DateTime dateTime);
        DateTimeOffset ToLocal(DateTimeOffset dateTimeOffset);
    }
}
