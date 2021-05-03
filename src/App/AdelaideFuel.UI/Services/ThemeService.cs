using AdelaideFuel.Services;
using System.Runtime.CompilerServices;
using Xamarin.Essentials;
using Xamarin.Forms;
using SysColor = System.Drawing.Color;
using XamColor = Xamarin.Forms.Color;

namespace AdelaideFuel.UI.Services
{
    public class ThemeService : IThemeService
    {
        public Theme Current => ThemeManager.CurrentTheme;
        public void SetTheme(Theme theme) => ThemeManager.ChangeTheme(theme);

        public SysColor PrimaryColor => GetColor();
        public SysColor PrimaryDarkColor => GetColor();
        public SysColor PrimaryAccentColor => GetColor();

        private XamColor GetColor([CallerMemberName] string resourceKey = "") => Application.Current.Resources.ContainsKey(resourceKey)
            ? (XamColor)Application.Current.Resources[resourceKey]
            : XamColor.Default;
    }
}