namespace AdelaideFuel.Maui.Controls
{
    public class TintImageButton : ImageButton
    {
        public static readonly BindableProperty TintColorProperty =
            BindableProperty.Create(
                propertyName: nameof(TintColor),
                returnType: typeof(Color),
                declaringType: typeof(TintImageButton),
                defaultValue: null);

        public TintImageButton() : base()
        {
            var tintBehavior = new IconTintColorBehavior();
            tintBehavior.SetBinding(IconTintColorBehavior.TintColorProperty, static (TintImageButton i) => i.TintColor, mode: BindingMode.OneWay, source: this);
            this.Behaviors.Add(tintBehavior);
        }

        public Color TintColor
        {
            get => (Color)GetValue(TintColorProperty);
            set => SetValue(TintColorProperty, value);
        }
    }
}