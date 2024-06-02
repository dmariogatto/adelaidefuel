using Microsoft.Maui.Controls.Shapes;
using System.Globalization;

namespace AdelaideFuel.Maui.Converters
{
    public class HeightToRoundRectangleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var height = System.Convert.ToDouble(value);
            return new RoundRectangle()
            {
                CornerRadius = new CornerRadius(height / 2d),
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is RoundRectangle rect
                ? rect.CornerRadius.TopLeft * 2d
                : 0d;
        }
    }
}