using MauiColor = Microsoft.Maui.Graphics.Color;
using SystemColor = System.Drawing.Color;

namespace AdelaideFuel
{
    public static class ColorExtensions
    {
        public static MauiColor ToMaui(this SystemColor color)
            => MauiColor.FromInt(color.ToArgb());

        public static SystemColor ToSystem(this MauiColor color)
            => SystemColor.FromArgb(color?.ToInt() ?? 0);
    }
}