using AdelaideFuel.Maui.Extensions;
using AdelaideFuel.Services;
using System.Globalization;

namespace AdelaideFuel.Maui.Converters
{
    public class BrandIdToIconConverter : IValueConverter
    {
        private readonly Lazy<IBrandService> _brandService = new Lazy<IBrandService>(IoC.Resolve<IBrandService>);

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var path = value is int id && id > 0
                ? _brandService.Value.GetBrandImagePath(id)
                : string.Empty;

            return new FileImageSource()
            {
                File = !string.IsNullOrEmpty(path) ? path : App.Current.FindResource<string>(Styles.Keys.FuelImg)
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}