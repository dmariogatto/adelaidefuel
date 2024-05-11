using AdelaideFuel.Maui.Extensions;
using AdelaideFuel.Models;
using System.Globalization;

namespace AdelaideFuel.Maui.Converters
{
    public class PriceCategoryToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (PriceCategory)value switch
            {
                PriceCategory.Lowest => App.Current.FindResource<Color>(Styles.Keys.LowestColor),
                PriceCategory.Low => App.Current.FindResource<Color>(Styles.Keys.LowColor),
                PriceCategory.Average => App.Current.FindResource<Color>(Styles.Keys.AverageColor),
                PriceCategory.High => App.Current.FindResource<Color>(Styles.Keys.HighColor),
                PriceCategory.Highest => App.Current.FindResource<Color>(Styles.Keys.HighestColor),
                _ => App.Current.FindResource<Color>(Styles.Keys.UnavailableColor),
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}