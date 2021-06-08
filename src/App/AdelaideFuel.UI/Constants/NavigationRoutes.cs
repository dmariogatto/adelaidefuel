using System;

namespace AdelaideFuel.UI
{
    public static class NavigationRoutes
    {
        public const string AbsoluteFormat = "//{0}";

        public const string Map = "map";
        public const string Sites = "sites";
        public const string Brands = "brands";
        public const string Fuels = "fuels";
        public const string Radii = "radii";
        public const string Settings = "settings";
        public const string About = "settings/about";

        public static string ToAbsolute(string uri) => string.Format(AbsoluteFormat, uri);
        public static string ToAbsolute(string uri, params string[] queryProperties) => ToAbsolute(AppendQueryProperties(uri, queryProperties));
        public static string AppendQueryProperties(string uri, params string[] queryProperties) => $"{uri}?{string.Join("&", queryProperties)}";
        public static string ToQueryProperty(string prop, object value) => $"{prop}={value}";
    }
}