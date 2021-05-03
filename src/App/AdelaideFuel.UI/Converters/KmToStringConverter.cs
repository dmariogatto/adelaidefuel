using System;
using System.Globalization;
using Xamarin.Forms;

namespace AdelaideFuel.UI.Converters
{
    public class KmToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var result = string.Empty;

            if (value is double km && km > 0)
            {
                result = km.KmToString();
            }

            return result;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}