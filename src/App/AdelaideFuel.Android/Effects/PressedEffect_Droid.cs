using AdelaideFuel.Droid.Effects;
using AdelaideFuel.UI.Effects;
using Android.Runtime;
using Xamarin.Forms;
using Xamarin.Forms.Platform.Android;

[assembly: ExportEffect(typeof(PressedEffect_Droid), nameof(PressedEffect))]
namespace AdelaideFuel.Droid.Effects
{
    [Preserve(AllMembers = true)]
    public class PressedEffect_Droid : PlatformEffect
    {
        private bool _attached;

        public PressedEffect_Droid()
        {
        }

        protected override void OnAttached()
        {
            // because an effect can be detached immediately after attached (happens in listview),
            // only attach the handler one time.
            if (!_attached)
            {
                if (Control != null)
                {
                    Control.Clickable = true;
                    Control.Click += Control_Click;

                    Control.LongClickable = true;
                    Control.LongClick += Control_LongClick;
                }
                else
                {
                    Container.Clickable = true;
                    Container.Click += Control_Click;

                    Container.LongClickable = true;
                    Container.LongClick += Control_LongClick;
                }
                _attached = true;
            }
        }

        private void Control_Click(object sender, System.EventArgs e)
        {
            var command = PressedEffect.GetCommand(Element);
            command?.Execute(PressedEffect.GetCommandParameter(Element));
        }

        private void Control_LongClick(object sender, Android.Views.View.LongClickEventArgs e)
        {
            var command = PressedEffect.GetCommandLong(Element);
            command?.Execute(PressedEffect.GetCommandLongParameter(Element));
        }

        protected override void OnDetached()
        {
            if (_attached)
            {
                if (Control != null)
                {
                    Control.Clickable = false;
                    Control.Click -= Control_Click;

                    Control.LongClickable = false;
                    Control.LongClick -= Control_LongClick;
                }
                else
                {
                    Container.Clickable = false;
                    Container.Click -= Control_Click;

                    Container.LongClickable = true;
                    Container.LongClick -= Control_LongClick;
                }
                _attached = false;
            }
        }
    }
}