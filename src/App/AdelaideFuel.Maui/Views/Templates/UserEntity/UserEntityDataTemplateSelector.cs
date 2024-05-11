using AdelaideFuel.Models;

namespace AdelaideFuel.Maui.Views
{
    public class UserEntityDataTemplateSelector : DataTemplateSelector
    {
        public const double BrandHeight = 60d;
        public const double FuelHeight = 60d;
        public const double RadiusHeight = 60d;

        public DataTemplate BrandTemplate { get; } = new DataTemplate(typeof(UserBrandTemplate));
        public DataTemplate FuelTemplate { get; } = new DataTemplate(typeof(UserFuelTemplate));
        public DataTemplate RadiusTemplate { get; } = new DataTemplate(typeof(UserRadiusTemplate));

        protected override DataTemplate OnSelectTemplate(object item, BindableObject container)
        {
            return item switch
            {
                UserBrand _ => BrandTemplate,
                UserFuel _ => FuelTemplate,
                UserRadius _ => RadiusTemplate,
                _ => throw new NotSupportedException($"{item.GetType()} is not supported"),
            };
        }
    }
}