using AdelaideFuel.Droid.Renderers;
using Android.Content;
using Android.Runtime;
using AndroidX.Core.View;
using System.ComponentModel;
using Xamarin.Forms;
using Xamarin.Forms.Platform.Android;

[assembly: ExportRenderer(typeof(Frame), typeof(CustomFrameRenderer))]
namespace AdelaideFuel.Droid.Renderers
{
    [Preserve(AllMembers = true)]
    public class CustomFrameRenderer : Xamarin.Forms.Platform.Android.FastRenderers.FrameRenderer
    {
        public CustomFrameRenderer(Context context) : base(context)
        {
        }

        protected override void OnElementChanged(ElementChangedEventArgs<Frame> e)
        {
            base.OnElementChanged(e);

            ApplyShadow();
        }

        protected override void OnElementPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == Frame.HasShadowProperty.PropertyName)
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
            Elevation = 0;
            TranslationZ = 0;

            if (Element is not null && Element.HasShadow)
            {
                ViewCompat.SetElevation(this, Context.ToPixels(2));
            }
        }
    }
}
