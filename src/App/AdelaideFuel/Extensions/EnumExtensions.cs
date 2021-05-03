using System;
using System.ComponentModel;
using System.Linq;

namespace AdelaideFuel
{
    public static class EnumExtensions
    {
        public static string GetDescription(this Enum enumValue)
        {
            var fi = enumValue.GetType().GetField(enumValue.ToString());

            var attributes = fi.GetCustomAttributes(
                typeof(DescriptionAttribute),
                false).OfType<DescriptionAttribute>()
                      .ToList();

            return attributes.Any()
                    ? attributes.First().Description
                    : enumValue.ToString();
        }
    }
}