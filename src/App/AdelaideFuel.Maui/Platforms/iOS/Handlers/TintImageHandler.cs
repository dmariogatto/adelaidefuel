using AdelaideFuel.Maui.Controls;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Platform;
using UIKit;
using IImage = Microsoft.Maui.IImage;

namespace AdelaideFuel.Maui.Handlers
{
    public static class TintImageHandler
    {
        public static void MapTintColor(IImageHandler handler, IImage image)
        {
            if (image is not TintImage tintImage)
                return;
            if (handler.PlatformView is not UIImageView imageView)
                return;

            if (tintImage.TintColor is null)
            {
                imageView.Image = imageView.Image.ImageWithRenderingMode(UIImageRenderingMode.Automatic);
                imageView.TintColor = null;
            }
            else
            {
                imageView.Image = imageView.Image.ImageWithRenderingMode(UIImageRenderingMode.AlwaysTemplate);
                imageView.TintColor = tintImage.TintColor.ToPlatform();
            }
        }
    }
}
