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
        private Android.Views.View _view;
        private bool _preClickable, _preLongClickable;

        public PressedEffect_Droid()
        {
        }

        protected override void OnAttached()
        {
            // because an effect can be detached immediately after attached (happens in listview),
            // only attach the handler one time.
            if (!_attached)
            {
                _view = Control ?? Container;

                if (_view != null)
                {
                    _attached = true;

                    _preClickable = _view.Clickable;
                    _preLongClickable = _view.LongClickable;

                    _view.Clickable = true;
                    _view.Click += Control_Click;

                    _view.LongClickable = true;
                    _view.LongClick += Control_LongClick;
                }
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
                _attached = false;

                _view.Click -= Control_Click;
                _view.LongClick -= Control_LongClick;

                _view.Clickable = _preClickable;
                _view.LongClickable = _preLongClickable;

                _view = null;
            }
        }
    }
}