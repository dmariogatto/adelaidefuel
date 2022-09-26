using AdelaideFuel.iOS.Renderers;
using Foundation;
using System.ComponentModel;
using System.Drawing;
using System.Threading.Tasks;
using UIKit;
using Xamarin.Forms;
using Xamarin.Forms.Platform.iOS;

[assembly: ExportRenderer(typeof(Frame), typeof(CustomFrameRenderer))]
namespace AdelaideFuel.iOS.Renderers
{
    [Preserve(AllMembers = true)]
    public class CustomFrameRenderer : FrameRenderer
    {
        protected override void OnElementChanged(ElementChangedEventArgs<Frame> e)
        {
            base.OnElementChanged(e);

            ApplyShadow();
        }

        protected override void OnElementPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == Xamarin.Forms.Frame.HasShadowProperty.PropertyName)
            {
                ApplyShadow();
            }
            else
            {
                base.OnElementPropertyChanged(sender, e);
            }
        }

        private void ApplyShadow()
        {
            Layer.ShadowOpacity = 0;

            if (Element is not null && Element.HasShadow)
            {
                Layer.ShadowColor = UIColor.Black.CGColor;
                Layer.ShadowOpacity = 0.4f;
                Layer.ShadowRadius = 2.0f;
                Layer.ShadowOffset = new SizeF(1.0f, 1.0f);
            }

            Layer.RasterizationScale = UIScreen.MainScreen.Scale;
            Layer.ShouldRasterize = true;
        }
    }
}
