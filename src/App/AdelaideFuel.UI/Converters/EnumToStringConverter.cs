using System;
using System.Globalization;
using Xamarin.Forms;

namespace AdelaideFuel.UI.Converters
{
    public class EnumToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Enum e &&
                parameter is Type t &&
                t.IsEnum &&
                t.IsEnumDefined(e))
            {
                return e.ToString();
            }

            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string val &&
                parameter is Type t &&
                t.IsEnum)
            {
                try
                {
                    return Enum.Parse(t, val, true);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(ex);
                }
            }

            return null;
        }
    }
}