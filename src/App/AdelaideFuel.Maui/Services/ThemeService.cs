using AdelaideFuel.Maui.Extensions;
using AdelaideFuel.Services;
using System.Runtime.CompilerServices;
using MauiColor = Microsoft.Maui.Graphics.Color;
using SystemColor = System.Drawing.Color;

namespace AdelaideFuel.Maui.Services
{
    public class ThemeService : IThemeService
    {
        public Theme Current => ThemeManager.CurrentTheme;
        public void SetTheme(Theme theme) => ThemeManager.ChangeTheme(theme);

        public SystemColor PrimaryColor => GetColor().ToSystem();
        public SystemColor PrimaryDarkColor => GetColor().ToSystem();
        public SystemColor PrimaryAccentColor => GetColor().ToSystem();

        public SystemColor ContrastColor => GetColor().ToSystem();

        private MauiColor GetColor([CallerMemberName] string resourceKey = "")
            => Application.Current.FindResource<MauiColor>(resourceKey, null);
    }
}