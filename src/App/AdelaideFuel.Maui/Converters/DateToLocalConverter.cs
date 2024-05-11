using AdelaideFuel.Services;
using System.Globalization;

namespace AdelaideFuel.Maui.Converters
{
    public class DateToLocalConverter : IValueConverter
    {
        private static readonly Lazy<IAppClock> Clock = new Lazy<IAppClock>(IoC.Resolve<IAppClock>);

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Clock.Value.ToLocal((DateTime)value);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}