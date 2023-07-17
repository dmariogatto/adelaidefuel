using AdelaideFuel.Localisation;
using AdelaideFuel.Shared;
using System;
using System.Globalization;
using Xamarin.Forms;

namespace AdelaideFuel.UI.Converters
{
    public class OpeningHourToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var result = Resources.Closed;

            if (value is OpeningHour oh && !oh.ClosedAllDay())
            {
                result = oh.OpenAllDay()
                    ? Resources.TwentyFourHours
                    : string.Format(Resources.ItemDashItem,
                        DateTime.MinValue.Date.Add(oh.Open).ToShortTimeString(),
                        DateTime.MinValue.Date.Add(oh.Close).ToShortTimeString());
            }

            return result;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}