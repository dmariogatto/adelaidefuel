using System.Collections;
using System.Globalization;

namespace AdelaideFuel.Maui.Converters
{
    public class CollectionToCountConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is null)
                return 0;
            if (value is ICollection collection)
                return collection.Count;

            throw new NotSupportedException("Value must implement ICollection!");
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
