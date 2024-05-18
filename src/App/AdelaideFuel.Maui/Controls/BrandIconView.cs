using AdelaideFuel.Maui.Converters;
using AdelaideFuel.Maui.Extensions;
using FFImageLoading.Maui;

namespace AdelaideFuel.Maui.Controls
{
    public class BrandIconView : Frame
    {
        private static readonly BrandIdToIconConverter IconConverter = new BrandIdToIconConverter();

        public static readonly BindableProperty BrandIdProperty =
          BindableProperty.Create(
              propertyName: nameof(BrandId),
              returnType: typeof(int),
              declaringType: typeof(BrandIconView),
              defaultValue: 0);

        public BrandIconView()
        {
            HasShadow = false;

            BackgroundColor = Colors.Transparent;

            HorizontalOptions = LayoutOptions.Center;
            VerticalOptions = LayoutOptions.Center;

            var ffImg = new CachedImage()
            {
                LoadingDelay = 200,
                LoadingPlaceholder = App.Current.FindResource<string>(Styles.Keys.FuelImg),
            };

            ffImg.SetBinding(
                CachedImage.SourceProperty,
                new Binding(nameof(BrandId),
                    converter: IconConverter,
                    source: this));

            Content = ffImg;

            Size = 44d;
        }

        private double _size = -1d;
        public double Size
        {
            get => _size;
            set
            {
                HeightRequest = value;
                WidthRequest = value;
                CornerRadius = (float)value / 2f;

                Content.HeightRequest = value;
                Content.WidthRequest = value;

                _size = value;
            }
        }

        public int BrandId
        {
            get => (int)GetValue(BrandIdProperty);
            set => SetValue(BrandIdProperty, value);
        }
    }
}