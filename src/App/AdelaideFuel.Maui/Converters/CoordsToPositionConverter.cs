using AdelaideFuel.Models;
using BetterMaps.Maui;
using System.Globalization;

namespace AdelaideFuel.Maui.Converters
{
    public class CoordsToPositionConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is Coords mapPosition
                ? new Position(mapPosition.Latitude, mapPosition.Longitude)
                : new Position();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}