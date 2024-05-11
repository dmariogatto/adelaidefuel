using System.Globalization;

namespace AdelaideFuel.Maui.Converters
{
    public class DoubleToGridLengthConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var number = value is double dv ? dv : 1d;
            var gridUnit = parameter is GridUnitType gut ? gut : GridUnitType.Absolute;
            return number >= 0 ? new GridLength(number, gridUnit) : GridLength.Auto;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}