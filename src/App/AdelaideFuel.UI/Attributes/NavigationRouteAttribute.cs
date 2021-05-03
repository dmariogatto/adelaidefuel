using System;

namespace AdelaideFuel.UI.Attributes
{
    [AttributeUsage(AttributeTargets.Class)]
    public class NavigationRouteAttribute : Attribute
    {
        public string Route { get; }
        public bool Absolute { get; }

        public NavigationRouteAttribute(string route, bool absolute = false)
        {
            if (string.IsNullOrWhiteSpace(route))
                throw new ArgumentOutOfRangeException(nameof(route));

            Route = route;
            Absolute = absolute;
        }
    }
}