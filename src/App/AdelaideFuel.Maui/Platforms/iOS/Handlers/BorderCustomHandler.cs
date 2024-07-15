using Microsoft.Maui.Platform;
using ContentView = Microsoft.Maui.Platform.ContentView;

namespace AdelaideFuel.Maui.Handlers
{
    public class BorderCustomHandler : Microsoft.Maui.Handlers.BorderHandler
    {
        protected override ContentView CreatePlatformView()
        {
            _ = VirtualView ?? throw new InvalidOperationException($"{nameof(VirtualView)} must be set to create a {nameof(ContentView)}");
            _ = MauiContext ?? throw new InvalidOperationException($"{nameof(MauiContext)} cannot be null");

            return new BorderContentView
            {
                CrossPlatformLayout = VirtualView
            };
        }

        private class BorderContentView : ContentView
        {
            public override void LayoutSubviews()
            {
                if (Layer.Sublayers?.FirstOrDefault(i => i is MauiCALayer) is { AnimationKeys: not null } caLayer)
                {
                    caLayer.RemoveAnimation("bounds");
                    caLayer.RemoveAnimation("position");
                }

                base.LayoutSubviews();
            }
        }
    }
}