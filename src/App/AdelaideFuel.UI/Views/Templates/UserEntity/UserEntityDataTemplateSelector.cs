using AdelaideFuel.Models;
using System;
using Xamarin.Forms;

namespace AdelaideFuel.UI.Views
{
    public class UserEntityDataTemplateSelector : DataTemplateSelector
    {
        public const double BrandHeight = 60d;
        public const double FuelHeight = 60d;

        public DataTemplate BrandTemplate { get; } = new DataTemplate(typeof(UserBrandTemplate));
        public DataTemplate FuelTemplate { get; } = new DataTemplate(typeof(UserFuelTemplate));

        protected override DataTemplate OnSelectTemplate(object item, BindableObject container)
        {
            return item switch
            {
                UserBrand _ => BrandTemplate,
                UserFuel _ => FuelTemplate,
                _ => throw new NotSupportedException($"{item.GetType()} is not supported"),
            };
        }
    }
}