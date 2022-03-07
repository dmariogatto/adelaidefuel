using Xamarin.Forms;

namespace AdelaideFuel.UI.Effects
{
    public class SearchBarIconEffect : RoutingEffect
    {
        public static readonly BindableProperty TintColorProperty =
          BindableProperty.Create(
              propertyName: "TintColor",
              returnType: typeof(Color),
              declaringType: typeof(SearchBarIconEffect),
              defaultValue: Color.Default);

        public SearchBarIconEffect() : base($"AdelaideFuel.Effects.{nameof(SearchBarIconEffect)}")
        {
        }

        public static Color GetTintColor(BindableObject view)
            => (Color)view.GetValue(TintColorProperty);

        public static void SetTintColor(BindableObject view, Color value)
            => view.SetValue(TintColorProperty, value);
    }
}