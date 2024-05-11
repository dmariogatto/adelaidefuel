using AdelaideFuel.Maui.Controls;
using Android.Graphics;
using Android.Widget;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Platform;
using IImage = Microsoft.Maui.IImage;

namespace AdelaideFuel.Maui.Handlers
{
    public static class TintImageHandler
    {
        public static void MapTintColor(IImageHandler handler, IImage image)
        {
            if (image is not TintImage tintImage)
                return;
            if (handler.PlatformView is not ImageView imageView)
                return;

            if (tintImage.TintColor is null)
            {
                imageView.ClearColorFilter();
            }
            else
            {
                var filter = new PorterDuffColorFilter(tintImage.TintColor.ToPlatform(), PorterDuff.Mode.SrcIn);
                imageView.SetColorFilter(filter);
            }
        }
    }
}
