using System;
using System.Globalization;
using Xamarin.Forms;

namespace AdelaideFuel.UI.Converters
{
    public class UrlToUriImageSourceConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return new UriImageSource()
            {
                Uri = new Uri((string)value),
                CacheValidity = TimeSpan.FromDays(3),
                CachingEnabled = true
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}