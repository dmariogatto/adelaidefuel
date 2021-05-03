using AdelaideFuel.Models;
using System;
using System.Globalization;
using Xamarin.Forms;

namespace AdelaideFuel.UI.Converters
{
    public class PriceCategoryToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (PriceCategory)value switch
            {
                PriceCategory.Lowest => Color.FromHex("#00C853"),
                PriceCategory.Low => Color.FromHex("#CDDC39"),
                PriceCategory.Average => Color.FromHex("#FFAB00"),
                PriceCategory.High => Color.FromHex("#FF6D00"),
                PriceCategory.Highest => Color.FromHex("#DD2C00"),
                _ => Color.Blue
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}