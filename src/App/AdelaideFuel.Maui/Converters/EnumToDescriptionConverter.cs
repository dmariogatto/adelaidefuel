using System.Globalization;

namespace AdelaideFuel.Maui.Converters
{
    public class EnumToDescriptionConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Enum e)
            {
                return e.GetDescription();
            }

            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (targetType is not null)
            {
                var enumType = Nullable.GetUnderlyingType(targetType) ?? targetType;

                if (value is string val && enumType?.IsEnum == true)
                {
                    try
                    {
                        foreach (Enum e in Enum.GetValues(enumType))
                        {
                            if (e.GetDescription() == val)
                                return e;
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine(ex);
                    }
                }
            }

            return null;
        }
    }
}