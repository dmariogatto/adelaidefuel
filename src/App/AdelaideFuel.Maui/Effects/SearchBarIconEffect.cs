namespace AdelaideFuel.Maui.Effects
{
    public class SearchBarIconEffect : RoutingEffect
    {
        public static readonly BindableProperty TintColorProperty =
          BindableProperty.Create(
              propertyName: "TintColor",
              returnType: typeof(Color),
              declaringType: typeof(SearchBarIconEffect),
              defaultValue: default);

        public static Color GetTintColor(BindableObject view)
            => (Color)view.GetValue(TintColorProperty);

        public static void SetTintColor(BindableObject view, Color value)
            => view.SetValue(TintColorProperty, value);
    }
}