namespace AdelaideFuel.Maui.Controls
{
    public class TintImage : Image
    {
        public static readonly BindableProperty TintColorProperty =
          BindableProperty.Create(
              propertyName: nameof(TintColor),
              returnType: typeof(Color),
              declaringType: typeof(TintImage),
              defaultValue: default);

        public Color TintColor
        {
            get => (Color)GetValue(TintColorProperty);
            set => SetValue(TintColorProperty, value);
        }
    }
}