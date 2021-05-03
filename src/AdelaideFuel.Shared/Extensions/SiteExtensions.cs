using System;
using System.Collections.Generic;

namespace AdelaideFuel.Shared
{
    public static class SiteExtensions
    {
        public static IDictionary<DayOfWeek, OpeningHour> GetOpeningHours(this SiteDto site)
            => new Dictionary<DayOfWeek, OpeningHour>()
                {
                    { DayOfWeek.Monday, new OpeningHour(DayOfWeek.Monday, site.MondayOpen, site.MondayClose) },
                    { DayOfWeek.Tuesday, new OpeningHour(DayOfWeek.Tuesday, site.TuesdayOpen, site.TuesdayClose) },
                    { DayOfWeek.Wednesday, new OpeningHour(DayOfWeek.Wednesday, site.WednesdayOpen, site.WednesdayClose) },
                    { DayOfWeek.Thursday, new OpeningHour(DayOfWeek.Thursday, site.ThursdayOpen, site.ThursdayClose) },
                    { DayOfWeek.Friday, new OpeningHour(DayOfWeek.Friday, site.FridayOpen, site.FridayClose) },
                    { DayOfWeek.Saturday, new OpeningHour(DayOfWeek.Saturday, site.SaturdayOpen, site.SaturdayClose) },
                    { DayOfWeek.Sunday, new OpeningHour(DayOfWeek.Sunday, site.SundayOpen, site.SundayClose) },
                };
    }
}
