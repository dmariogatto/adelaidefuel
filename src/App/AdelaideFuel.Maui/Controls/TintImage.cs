namespace AdelaideFuel.Maui.Controls
{
    public class TintImage : Image
    {
        public static readonly BindableProperty TintColorProperty =
            BindableProperty.Create(
                propertyName: nameof(TintColor),
                returnType: typeof(Color),
                declaringType: typeof(TintImage),
                defaultValue: null);

        public TintImage() : base()
        {
            var tintBehavior = new IconTintColorBehavior();
            tintBehavior.SetBinding(IconTintColorBehavior.TintColorProperty, static (TintImage i) => i.TintColor, mode: BindingMode.OneWay, source: this);
            this.Behaviors.Add(tintBehavior);
        }

        public Color TintColor
        {
            get => (Color)GetValue(TintColorProperty);
            set => SetValue(TintColorProperty, value);
        }
    }
}