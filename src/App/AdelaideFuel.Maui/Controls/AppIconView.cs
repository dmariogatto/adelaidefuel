using AdelaideFuel.Maui.Extensions;
using Microsoft.Maui.Controls.Shapes;

namespace AdelaideFuel.Maui.Controls
{
    public class AppIconView : Border
    {
        public static readonly BindableProperty IsBouncingProperty =
          BindableProperty.Create(
              propertyName: nameof(IsBouncing),
              returnType: typeof(bool),
              declaringType: typeof(AppIconView),
              defaultValue: false,
              propertyChanged: (b, n, o) => IsBouncingChanged((AppIconView)b, (bool)n, (bool)o));

        private const string AnimationHandle = "BounceAnimation";

        private readonly Animation _bounceAnimation;

        private CancellationTokenSource _animationCts;

        public AppIconView()
        {
            StrokeShape = new RoundRectangle() { CornerRadius = 8 };

            _bounceAnimation = new Animation
            {
                { 0.00, 0.25, new Animation(v => TranslationY = v, 0, -40) },
                { 0.25, 0.50, new Animation(v => TranslationY = v, -40, 0, easing: Easing.BounceOut) }
            };

            BackgroundColor = Color.FromRgba(0x4C, 0xAF, 0x50, 0xFF);
            HeightRequest = WidthRequest = 60;
            HorizontalOptions = VerticalOptions = LayoutOptions.Center;
            Margin = Padding = 0;

            var icon = new Image() { Source = Application.Current.FindResource<string>(Styles.Keys.TwoToneFuelImg) };
            icon.HeightRequest = icon.WidthRequest = 48;
            icon.HorizontalOptions = icon.VerticalOptions = LayoutOptions.Center;

            Content = icon;
        }

        public bool IsBouncing
        {
            get => (bool)GetValue(IsBouncingProperty);
            set => SetValue(IsBouncingProperty, value);
        }

        private static void IsBouncingChanged(AppIconView view, bool oldVal, bool newVal)
        {
            if (view is not null)
            {
                if (newVal)
                {
                    if (view.AnimationIsRunning(AnimationHandle))
                        view.AbortAnimation(AnimationHandle);

                    view._animationCts = new CancellationTokenSource();
                    var tok = view._animationCts.Token;

                    view.Animate(AnimationHandle, view._bounceAnimation, length: 1000, repeat: () => !tok.IsCancellationRequested);
                }
                else
                {
                    view._animationCts?.Cancel();
                }
            }
        }
    }
}