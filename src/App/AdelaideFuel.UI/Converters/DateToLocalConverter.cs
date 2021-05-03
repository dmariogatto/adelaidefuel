using System;
using System.Globalization;
using Xamarin.Forms;

namespace AdelaideFuel.UI.Converters
{
    public class DateToLocalConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return ((DateTime)value).ToLocalTime();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}