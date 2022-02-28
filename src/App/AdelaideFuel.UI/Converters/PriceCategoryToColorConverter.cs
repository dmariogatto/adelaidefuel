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
                PriceCategory.Lowest => (Color)App.Current.Resources[Styles.Keys.LowestColor],
                PriceCategory.Low => (Color)App.Current.Resources[Styles.Keys.LowColor],
                PriceCategory.Average => (Color)App.Current.Resources[Styles.Keys.AverageColor],
                PriceCategory.High => (Color)App.Current.Resources[Styles.Keys.HighColor],
                PriceCategory.Highest => (Color)App.Current.Resources[Styles.Keys.HighestColor],
                _ => (Color)App.Current.Resources[Styles.Keys.UnavailableColor],
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}