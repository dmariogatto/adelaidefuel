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

        private static readonly ImageSource TwoToneImg = ImageSource.FromFile("twotone_circle.png");

        private static Pin CreatePin(Type t)
        {
            if (t != typeof(Site))
                throw new ArgumentOutOfRangeException(nameof(t));

            var pin = new Pin();

            pin.SetBinding(Pin.AddressProperty, static (IMapPin i) => i.Description, mode: BindingMode.OneWay);
            pin.SetBinding(Pin.LabelProperty, static (IMapPin i) => i.Label, mode: BindingMode.OneWay);
            pin.SetBinding(Pin.ZIndexProperty, static (IMapPin i) => i.ZIndex, mode: BindingMode.OneWay);
            pin.SetBinding(Pin.PositionProperty, static (IMapPin i) => i.Position, converter: PositionConverter, mode: BindingMode.OneWay);
            pin.SetBinding(Pin.TintColorProperty, static (Site i) => i.PriceCategory, converter: PriceCategoryConverter, mode: BindingMode.OneWay);

            pin.Anchor = new Point(0.5, 0.5);
            pin.ImageSource = TwoToneImg;

            return pin;
        }

        public static readonly DataTemplate SiteTemplate = new DataTemplate(() => CreatePin(typeof(Site)));

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