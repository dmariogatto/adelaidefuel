using System;

namespace AdelaideFuel
{
    public static class AppCenterEvents
    {
        public static class PageView
        {
            public const string HomeView = "home_view";
            public const string MapView = "map_view";
            public const string BrandsView = "brands_view";
            public const string FuelsView = "fuels_view";
            public const string SettingsView = "settings_view";
        }

        public static class Setting
        {
            public const string AppTheme = "app_theme";
        }

        public static class Action
        {
            public const string FuelSetup = "fuel_setup";
            public const string ReviewRequested = "review_requested";
        }

        public static class Property
        {
            public const string Value = "value";
            public const string New = "new";
            public const string Old = "old";
        }
    }
}