using AdelaideFuel.Localisation;
using AdelaideFuel.Services;
using System.Globalization;

namespace AdelaideFuel.Maui.Converters
{
    public class DateTimeToFriendlyConverter : IValueConverter
    {
        private static readonly Lazy<IAppClock> AppClock = new Lazy<IAppClock>(IoC.Resolve<IAppClock>);

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var timeDiff = value switch
            {
                DateTime dt => AppClock.Value.LocalNow - AppClock.Value.ToLocal(dt),
                DateTimeOffset dto => DateTimeOffset.UtcNow - dto,
                _ => TimeSpan.Zero
            };

            var result = string.Empty;
            if (timeDiff > TimeSpan.Zero)
            {
                var totalDays = (int)timeDiff.TotalDays;
                result = string.Format(totalDays == 1
                    ? Resources.ItemDayAgo
                    : Resources.ItemDaysAgo, totalDays);
            }

            return result;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}