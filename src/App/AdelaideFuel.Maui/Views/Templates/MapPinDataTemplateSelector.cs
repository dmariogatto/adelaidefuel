using AdelaideFuel.Maui.Converters;
using AdelaideFuel.Maui.Extensions;
using AdelaideFuel.Models;
using BetterMaps.Maui;
using IMapPin = AdelaideFuel.Models.IMapPin;

namespace AdelaideFuel.Maui.Views
{
    public class MapPinDataTemplateSelector : DataTemplateSelector
    {
        private static readonly CoordsToPositionConverter PositionConverter = new CoordsToPositionConverter();
        private static readonly PriceCategoryToColorConverter PriceCategoryConverter = new PriceCategoryToColorConverter();

        private static Pin CreatePinTemplate(Type t)
        {
            if (t != typeof(Site))
                throw new ArgumentOutOfRangeException(nameof(t));

            var pin = new Pin();

            pin.SetBinding(Pin.AddressProperty, new Binding(nameof(IMapPin.Description)));
            pin.SetBinding(Pin.LabelProperty, new Binding(nameof(IMapPin.Label)));
            pin.SetBinding(Pin.ZIndexProperty, new Binding(nameof(IMapPin.ZIndex)));
            pin.SetBinding(Pin.PositionProperty, new Binding(nameof(IMapPin.Position), converter: PositionConverter));
            pin.SetBinding(Pin.TintColorProperty, new Binding(nameof(Site.PriceCategory), converter: PriceCategoryConverter));

            pin.Anchor = new Point(0.5, 0.5);
            pin.ImageSource = Application.Current.FindResource<string>(Styles.Keys.TwoToneCircleImg);

            return pin;
        }

        public DataTemplate SiteTemplate { get; set; } = new DataTemplate(() => CreatePinTemplate(typeof(Site)));

        protected override DataTemplate OnSelectTemplate(object item, BindableObject container)
        {
            return item switch
            {
                Site _ => SiteTemplate,
                _ => throw new NotSupportedException($"{item.GetType()} is not supported"),
            };
        }
    }
}