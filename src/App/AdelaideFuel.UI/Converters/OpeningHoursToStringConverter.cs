using AdelaideFuel.Localisation;
using AdelaideFuel.Shared;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Xamarin.Forms;

namespace AdelaideFuel.UI.Converters
{
    public class OpeningHoursToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var result = Resources.Closed;

            if (value is IDictionary<DayOfWeek, OpeningHour> openingHours)
            {
                var adlNow = Constants.AdelaideNow;

                if (openingHours.All(kv => kv.Value.OpenAllDay()))
                    result = Resources.TwentyFourHours;
                else if (openingHours.TryGetValue(adlNow.DayOfWeek, out var oh) && oh.IsOpen(adlNow.TimeOfDay))
                    result = Resources.Open;
                else if (openingHours.TryGetValue(adlNow.DayOfWeek, out var poh) && adlNow.TimeOfDay < poh.Close)
                    result = Resources.Open;
            }

            return result;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        private static DayOfWeek PreviousDay(DayOfWeek day)
        {
            var dayIdx = (int)day;
            var previousDayIdx = dayIdx == 0 ? 6 : dayIdx - 1;
            return (DayOfWeek)previousDayIdx;
        }
    }
}