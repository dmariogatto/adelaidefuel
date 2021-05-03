using AdelaideFuel.UI.Controls;
using System;
using System.Globalization;
using Xamarin.Forms;

namespace AdelaideFuel.UI.Converters
{
    public class AdNotLoadedConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return ((AdLoadStatus)value) != AdLoadStatus.Loaded;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}