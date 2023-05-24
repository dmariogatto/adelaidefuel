using AdelaideFuel.iOS.Renderers;
using AdelaideFuel.UI.Controls;
using Foundation;
using System.ComponentModel;
using UIKit;
using Xamarin.Forms;
using Xamarin.Forms.Platform.iOS;

[assembly: ExportRenderer(typeof(TintImage), typeof(TintImageRenderer))]
namespace AdelaideFuel.iOS.Renderers
{
    [Preserve(AllMembers = true)]
    public class TintImageRenderer : ImageRenderer
    {
        protected override void OnElementChanged(ElementChangedEventArgs<Image> e)
        {
            base.OnElementChanged(e);

            ApplyTintColor();
        }

        protected override void OnElementPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            base.OnElementPropertyChanged(sender, e);

            if (e.PropertyName == TintImage.SourceProperty.PropertyName ||
                e.PropertyName == TintImage.TintColorProperty.PropertyName)
            {
                ApplyTintColor();
            }
        }

        private void ApplyTintColor()
        {
            if (Control?.Image is not null && Element is TintImage tintImage)
            {
                if (tintImage.TintColor == Color.Transparent)
                {
                    Control.Image = Control.Image.ImageWithRenderingMode(UIImageRenderingMode.Automatic);
                    Control.TintColor = null;
                }
                else
                {
                    Control.Image = Control.Image.ImageWithRenderingMode(UIImageRenderingMode.AlwaysTemplate);
                    Control.TintColor = tintImage.TintColor.ToUIColor();
                }
            }
        }
    }
}