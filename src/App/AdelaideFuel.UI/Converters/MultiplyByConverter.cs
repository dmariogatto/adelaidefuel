using System;
using System.Globalization;
using Xamarin.Forms;

namespace AdelaideFuel.UI.Converters
{
    public class MultiplyByConverter : IValueConverter
    {
        public double MinValue { get; }
        public double MaxValue { get; }

        public MultiplyByConverter() : this(double.MinValue, double.MaxValue)
        {
        }

        public MultiplyByConverter(double minValue, double maxValue)
        {
            MinValue = minValue;
            MaxValue = maxValue;
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var result = System.Convert.ToDouble(value) * System.Convert.ToDouble(parameter);
            result = Math.Max(MinValue, Math.Min(MaxValue, result));

            return System.Convert.ChangeType(result, targetType);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return System.Convert.ChangeType(
                System.Convert.ToDouble(value) / System.Convert.ToDouble(parameter),
                targetType); 
        }
    }
}