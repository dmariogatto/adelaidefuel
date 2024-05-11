using System.Globalization;

namespace AdelaideFuel.Maui.Converters
{
    public class BytesToKbConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is long bytes
                ? $"{bytes / 1024f:#,##0.##} KB"
                : "0 KB";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}