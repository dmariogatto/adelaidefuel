using AdelaideFuel.iOS.Renderers;
using CoreGraphics;
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
        public override void Draw(CGRect rect)
        {
            if (Element is not null && Element.HasShadow)
            {
                Layer.ShadowRadius = 1.0f;
                Layer.ShadowOpacity = 0.4f;
                Layer.ShadowOffset = new SizeF(1.0f, 1.0f);
            }

            base.Draw(rect);
        }
    }
}
