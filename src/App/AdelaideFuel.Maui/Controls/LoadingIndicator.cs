using AdelaideFuel.Maui.Extensions;
using AdelaideFuel.ViewModels;

namespace AdelaideFuel.Maui.Controls
{
    public class LoadingIndicator : Border
    {
        public static readonly BindableProperty IsBusyProperty =
          BindableProperty.Create(
              propertyName: nameof(IsBusy),
              returnType: typeof(bool),
              declaringType: typeof(LoadingIndicator),
              defaultValue: false);

        public LoadingIndicator()
        {
            SetDynamicResource(StyleProperty, Styles.Keys.CardBorderStyle);

            InputTransparent = true;
            HorizontalOptions = LayoutOptions.Center;
            VerticalOptions = LayoutOptions.End;

            Margin = new Thickness(
                App.Current.FindResource<double>(Styles.Keys.MediumSpacing),
                App.Current.FindResource<double>(Styles.Keys.LargeSpacing));
            Padding = App.Current.FindResource<Thickness>(Styles.Keys.XSmallThickness);

            var activityIndicator = new ActivityIndicator()
            {
                WidthRequest = 24,
                HeightRequest = 24
            };

            activityIndicator.SetDynamicResource(ActivityIndicator.ColorProperty, Styles.Keys.PrimaryAccentColor);

            activityIndicator.SetBinding(
                ActivityIndicator.IsRunningProperty,
                static (LoadingIndicator i) => i.IsBusy,
                mode: BindingMode.OneWay,
                source: this);

            this.SetBinding(
                IsVisibleProperty,
                static (LoadingIndicator i) => i.IsBusy,
                mode: BindingMode.OneWay,
                source: RelativeBindingSource.Self);
            this.SetBinding(
                IsBusyProperty,
                static (BaseViewModel i) => i.IsBusy,
                mode: BindingMode.OneWay);

            Content = activityIndicator;
        }

        public bool IsBusy
        {
            get => (bool)GetValue(IsBusyProperty);
            set => SetValue(IsBusyProperty, value);
        }
    }
}