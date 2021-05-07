using System;
using System.Globalization;
using System.IO;
using Xamarin.Essentials.Interfaces;
using Xamarin.Forms;

namespace AdelaideFuel.UI.Converters
{
    public class BrandIdToIconConverter : IValueConverter
    {
        private readonly Lazy<bool> _2x = new Lazy<bool>(() => IoC.Resolve<IDeviceDisplay>().MainDisplayInfo.Density < 3);
        private readonly string _iconUrlFormat = Path.Combine(Constants.ApiUrlBase, "Brand/Img/{0}%40{1}.png?code=" + Constants.ApiKeyBrandImg);

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int id && id > 0)
            {
                var size = _2x.Value ? "2x" : "3x";
                return new UriImageSource()
                {
                    Uri = new Uri(string.Format(_iconUrlFormat, id, size)),
                    CachingEnabled = true,
                    CacheValidity = TimeSpan.FromDays(7)
                };
            }

            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}