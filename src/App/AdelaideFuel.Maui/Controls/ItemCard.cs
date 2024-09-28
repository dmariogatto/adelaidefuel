using AdelaideFuel.Maui.Extensions;

namespace AdelaideFuel.Maui.Controls
{
    public class ItemCard : Border
    {
        public ItemCard()
        {
            SetDynamicResource(StyleProperty, Styles.Keys.CardBorderStyle);

            Margin = App.Current.FindResource<Thickness>(Styles.Keys.ItemMargin);
            Padding = App.Current.FindResource<Thickness>(Styles.Keys.SmallThickness);

            if (DeviceInfo.Platform == DevicePlatform.Android)
            {
                Shadow = new Shadow()
                {
                    Brush = Colors.Black,
                    Opacity = 0.35f,
                    Radius = 1.75f,
                    Offset = new Point(2, 2),
                };
            }
        }
    }
}