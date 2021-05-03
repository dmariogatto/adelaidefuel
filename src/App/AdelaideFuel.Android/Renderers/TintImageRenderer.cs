using AdelaideFuel.Droid.Renderers;
using AdelaideFuel.UI.Controls;
using Android.Content;
using Android.Graphics;
using Android.Runtime;
using System.ComponentModel;
using Xamarin.Forms;
using Xamarin.Forms.Platform.Android;
using Color = Xamarin.Forms.Color;

[assembly: ExportRenderer(typeof(TintImage), typeof(TintImageRenderer))]
namespace AdelaideFuel.Droid.Renderers
{
    [Preserve(AllMembers = true)]
    public class TintImageRenderer : ImageRenderer
    {
        public TintImageRenderer(Context context) : base(context)
        {
        }

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
            if (Control != null && Element is TintImage tintImage)
            {
                if (tintImage.TintColor == Color.Transparent)
                {
                    Control.ClearColorFilter();
                }
                else
                {
                    var filter = new PorterDuffColorFilter(tintImage.TintColor.ToAndroid(), PorterDuff.Mode.SrcIn);
                    Control.SetColorFilter(filter);
                }
            }
        }
    }
}