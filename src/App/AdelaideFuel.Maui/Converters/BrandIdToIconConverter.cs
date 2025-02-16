using AdelaideFuel.Maui.Extensions;
using AdelaideFuel.Maui.ImageSources;
using AdelaideFuel.Services;
using System.Globalization;

namespace AdelaideFuel.Maui.Converters
{
    public class BrandIdToIconConverter : IValueConverter
    {
        private readonly Lazy<IBrandService> _brandService = new Lazy<IBrandService>(IoC.Resolve<IBrandService>);

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var fallbackImg = App.Current.FindResource<string>(Styles.Keys.FuelImg);

            if (value is int id && id > 0)
            {
                return new FileAsyncImageSource()
                {
                    File = async (ct) =>
                    {
                        var path = await _brandService.Value.GetBrandImagePathAsync(id, ct);
                        return File.Exists(path) ? path : fallbackImg;

                    }
                };
            }

            return ImageSource.FromFile(fallbackImg);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}