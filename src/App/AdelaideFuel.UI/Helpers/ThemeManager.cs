using AdelaideFuel.Services;
using AdelaideFuel.UI.Styles;
using System;
using System.Collections.Generic;
using System.Linq;
using Xamarin.Forms;
using WeakEventManager = MvvmHelpers.WeakEventManager;

namespace AdelaideFuel.UI
{
    public class ThemeEventArgs : EventArgs
    {
        public Theme New { get; private set; }
        public Theme Old { get; private set; }

        public ThemeEventArgs(Theme newTheme, Theme oldTheme)
        {
            New = newTheme;
            Old = oldTheme;
        }
    }

    public static class ThemeManager
    {
        private readonly static Lazy<IAppPreferences> AppPreferences = new Lazy<IAppPreferences>(() => IoC.Resolve<IAppPreferences>());
        private readonly static Lazy<IEnvironmentService> EnvironmentService = new Lazy<IEnvironmentService>(() => IoC.Resolve<IEnvironmentService>());

        private readonly static IDictionary<Theme, ResourceDictionary> Resources = new Dictionary<Theme, ResourceDictionary>();
        private readonly static WeakEventManager WeakEventManager = new WeakEventManager();

        public static event EventHandler<ThemeEventArgs> CurrentThemeChanged
        {
            add => WeakEventManager.AddEventHandler(value, nameof(CurrentThemeChanged));
            remove => WeakEventManager.RemoveEventHandler(value, nameof(CurrentThemeChanged));
        }

        public static event EventHandler<ThemeEventArgs> ResolvedThemeChanged
        {
            add => WeakEventManager.AddEventHandler(value, nameof(ResolvedThemeChanged));
            remove => WeakEventManager.RemoveEventHandler(value, nameof(ResolvedThemeChanged));
        }

        public static Theme CurrentTheme => AppPreferences.Value.AppTheme;
        public static Theme ResolvedTheme { get; private set; } = Theme.Light;

        public static void LoadTheme()
        {
            var theme = CurrentTheme == Theme.System
                ? EnvironmentService.Value.GetOperatingSystemTheme()
                : CurrentTheme;

            var mergedDictionaries = Application.Current?.Resources?.MergedDictionaries;
            if (mergedDictionaries != null &&
                (theme != ResolvedTheme || !mergedDictionaries.Any()))
            {
                mergedDictionaries.Clear();
                mergedDictionaries.Add(GetDictionary(theme));

                var oldTheme = ResolvedTheme;
                ResolvedTheme = theme;

                WeakEventManager.HandleEvent(Application.Current, new ThemeEventArgs(theme, oldTheme), nameof(ResolvedThemeChanged));
            }
        }

        public static void ChangeTheme(Theme theme)
        {
            var oldTheme = AppPreferences.Value.AppTheme;
            AppPreferences.Value.AppTheme = theme;

            if (oldTheme != CurrentTheme)
                WeakEventManager.HandleEvent(Application.Current, new ThemeEventArgs(CurrentTheme, oldTheme), nameof(CurrentThemeChanged));

            LoadTheme();
        }

        public static void OsThemeChanged()
        {
            if (CurrentTheme == Theme.System)
                LoadTheme();
        }

        private static ResourceDictionary GetDictionary(Theme theme)
        {
            lock (Resources)
            {
                if (!Resources.ContainsKey(theme))
                {
                    switch (theme)
                    {
                        case Theme.Dark:
                            Resources.Add(Theme.Dark, new DarkTheme());
                            break;
                        default:
                            Resources.Add(Theme.Light, new LightTheme());
                            break;
                    }
                }

                return Resources[theme];
            }
        }
    }
}