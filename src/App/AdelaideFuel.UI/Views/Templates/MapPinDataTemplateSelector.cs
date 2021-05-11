using AdelaideFuel.Models;
using AdelaideFuel.UI.Converters;
using System;
using Xamarin.Forms;
using Xamarin.Forms.Maps;

namespace AdelaideFuel.UI.Views
{
    public class MapPinDataTemplateSelector : DataTemplateSelector
    {
        private readonly static CoordsToPositionConverter PositionConverter = new CoordsToPositionConverter();
        private readonly static PriceCategoryToColorConverter PriceCategoryConverter = new PriceCategoryToColorConverter();

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
            pin.FileImage = Application.Current.Resources[Styles.Keys.TwoToneCircleImg]?.ToString();

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