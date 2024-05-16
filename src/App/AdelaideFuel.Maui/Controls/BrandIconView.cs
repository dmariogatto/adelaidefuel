using AdelaideFuel.Maui.Converters;
using AdelaideFuel.Maui.Extensions;
using FFImageLoading.Maui;

namespace AdelaideFuel.Maui.Controls
{
    public class BrandIconView : Frame
    {
        public static readonly BindableProperty SizeProperty =
          BindableProperty.Create(
              propertyName: nameof(Size),
              returnType: typeof(double),
              declaringType: typeof(BrandIconView),
              defaultValue: 44d);

        public static readonly BindableProperty BrandIdProperty =
          BindableProperty.Create(
              propertyName: nameof(BrandId),
              returnType: typeof(int),
              declaringType: typeof(BrandIconView),
              defaultValue: 0);

        public static readonly BrandIdToIconConverter IconConverter = new BrandIdToIconConverter();
        public static readonly DivideByConverter RadiusConverter = new DivideByConverter();

        public BrandIconView()
        {
            HasShadow = false;

            BackgroundColor = Colors.Transparent;

            HorizontalOptions = LayoutOptions.Center;
            VerticalOptions = LayoutOptions.Center;

            SetBinding(
                CornerRadiusProperty,
                new Binding(nameof(Size),
                    converter: RadiusConverter,
                    converterParameter: 2d,
                    source: this));
            SetBinding(
                HeightRequestProperty,
                new Binding(nameof(Size), source: this));
            SetBinding(
                WidthRequestProperty,
                new Binding(nameof(Size), source: this));

            var ffImg = new CachedImage()
            {
                LoadingDelay = 200,
                LoadingPlaceholder = App.Current.FindResource<string>(Styles.Keys.FuelImg),
            };

            ffImg.SetBinding(
                HeightRequestProperty,
                new Binding(nameof(Size), source: this));
            ffImg.SetBinding(
                WidthRequestProperty,
                new Binding(nameof(Size), source: this));
            ffImg.SetBinding(
                CachedImage.SourceProperty,
                new Binding(nameof(BrandId),
                    converter: IconConverter,
                    source: this));

            Content = ffImg;
        }

        public double Size
        {
            get => (double)GetValue(SizeProperty);
            set => SetValue(SizeProperty, value);
        }

        public int BrandId
        {
            get => (int)GetValue(BrandIdProperty);
            set => SetValue(BrandIdProperty, value);
        }
    }
}