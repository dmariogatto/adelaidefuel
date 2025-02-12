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
            if (value is int id && id > 0)
            {
                return ImageSource.FromStream(async (ct) =>
                {
                    var path = await _brandService.Value.GetBrandImagePathAsync(id, ct);
                    return File.Exists(path)
                        ? File.OpenRead(path)
                        : Stream.Null;
                });
            }

            return ImageSource.FromFile(App.Current.FindResource<string>(Styles.Keys.FuelImg));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}