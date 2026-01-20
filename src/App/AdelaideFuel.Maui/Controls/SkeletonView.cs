namespace AdelaideFuel.Maui.Controls
{
    public class SkeletonView : BoxView
    {
        public static readonly BindableProperty IsAnimatingProperty =
            BindableProperty.Create(
                propertyName: nameof(IsAnimating),
                returnType: typeof(bool),
                declaringType: typeof(SkeletonView),
                defaultValue: false,
                propertyChanged: (b, o, n) => OnIsAnimatingChanged((SkeletonView)b, (bool)o, (bool)n));

        private const string AnimationHandle = "FadeInOut";

        private readonly Animation _smoothAnimation;

        public SkeletonView()
        {
            Color = ThemeManager.ResolvedTheme == AdelaideFuel.Services.Theme.Dark
                ? Colors.DarkGray
                : Colors.LightGray;

            _smoothAnimation = new Animation();
            _smoothAnimation.WithConcurrent((f) => Opacity = f, 0.30, 0.80, Easing.Linear);
            _smoothAnimation.WithConcurrent((f) => Opacity = f, 0.80, 0.30, Easing.Linear);
        }

        public bool IsAnimating
        {
            get => (bool)GetValue(IsAnimatingProperty);
            set => SetValue(IsAnimatingProperty, value);
        }

        private void StartAnimating()
        {
            if (!this.AnimationIsRunning(AnimationHandle))
                this.Animate(AnimationHandle, _smoothAnimation, 16, 2200, Easing.Linear, null, () => IsVisible);
        }

        private void StopAnimating()
        {
            if (this.AnimationIsRunning(AnimationHandle))
                this.AbortAnimation(AnimationHandle);
        }

        private static void OnIsAnimatingChanged(SkeletonView sender, bool oldValue, bool newValue)
        {
            if (newValue)
            {
                sender.StartAnimating();
            }
            else
            {
                sender.StopAnimating();
            }
        }
    }
}