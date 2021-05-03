using System;

namespace AdelaideFuel.Models
{
    [Flags]
    public enum AppAlertShow
    {
        None = 0,
        OnStart = 1 << 0,
        OnResume = 1 << 1
    }

    public class AppAlert
    {
        public Guid Id { get; set; }
        public string CultureName { get; set; }
        public string Title { get; set; }
        public string Message { get; set; }
        public string OkText { get; set; }

        public DateTime? StartDateUtc { get; set; }
        public DateTime? EndDateUtc { get; set; }

        public AppAlertShow ShowWhen { get; set; }

        public bool IsValid()
        {
            var now = DateTime.UtcNow;
            return (!StartDateUtc.HasValue || StartDateUtc <= now) &&
                   (!EndDateUtc.HasValue || now <= EndDateUtc);
        }
    }
}