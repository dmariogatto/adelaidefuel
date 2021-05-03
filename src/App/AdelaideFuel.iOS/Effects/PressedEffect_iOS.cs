using AdelaideFuel.iOS.Effects;
using AdelaideFuel.UI.Effects;
using Foundation;
using UIKit;
using Xamarin.Forms;
using Xamarin.Forms.Platform.iOS;

[assembly: ExportEffect(typeof(PressedEffect_iOS), nameof(PressedEffect))]
namespace AdelaideFuel.iOS.Effects
{
    [Preserve(AllMembers = true)]
    public class PressedEffect_iOS : PlatformEffect
    {
        private readonly UITapGestureRecognizer _tapRecognizer;
        private readonly UILongPressGestureRecognizer _longPressRecognizer;

        private bool _attached;

        public PressedEffect_iOS()
        {
            _tapRecognizer = new UITapGestureRecognizer(HandleClick);
            _longPressRecognizer = new UILongPressGestureRecognizer(HandleLongClick);
        }

        protected override void OnAttached()
        {
            // because an effect can be detached immediately after attached (happens in listview),
            // only attach the handler one time
            if (!_attached)
            {
                Container.AddGestureRecognizer(_tapRecognizer);
                Container.AddGestureRecognizer(_longPressRecognizer);
                _attached = true;
            }
        }

        private void HandleClick()
        {
            var command = PressedEffect.GetCommand(Element);
            command?.Execute(PressedEffect.GetCommandParameter(Element));
        }

        private void HandleLongClick()
        {
            var command = PressedEffect.GetCommandLong(Element);
            command?.Execute(PressedEffect.GetCommandLongParameter(Element));
        }
        
        protected override void OnDetached()
        {
            if (_attached)
            {
                Container.RemoveGestureRecognizer(_tapRecognizer);
                Container.RemoveGestureRecognizer(_longPressRecognizer);
                _attached = false;
            }
        }
    }
}