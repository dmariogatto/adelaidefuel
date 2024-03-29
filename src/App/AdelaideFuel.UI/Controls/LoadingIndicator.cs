﻿using AdelaideFuel.ViewModels;
using Xamarin.Forms;

namespace AdelaideFuel.UI.Controls
{
    public class LoadingIndicator : Frame
    {
        public static readonly BindableProperty IsBusyProperty =
          BindableProperty.Create(
              propertyName: nameof(IsBusy),
              returnType: typeof(bool),
              declaringType: typeof(LoadingIndicator),
              defaultValue: false);

        public LoadingIndicator()
        {
            InputTransparent = true;
            HorizontalOptions = LayoutOptions.Center;
            VerticalOptions = LayoutOptions.End;

            Margin = new Thickness(
                (double)App.Current.Resources[Styles.Keys.MediumSpacing],
                (double)App.Current.Resources[Styles.Keys.LargeSpacing]);
            Padding = (Thickness)App.Current.Resources[Styles.Keys.XSmallThickness];

            SetDynamicResource(StyleProperty, Styles.Keys.CardStyle);

            var activityIndicator = new ActivityIndicator()
            {
                WidthRequest = 24,
                HeightRequest = 24
            };
            activityIndicator.SetDynamicResource(ActivityIndicator.ColorProperty, Styles.Keys.PrimaryAccentColor);
            activityIndicator.SetBinding(ActivityIndicator.IsRunningProperty,
                new Binding(nameof(IsBusy), source: this));

            SetBinding(IsVisibleProperty, new Binding(nameof(IsBusy), source: this));
            SetBinding(IsBusyProperty, new Binding(nameof(BaseViewModel.IsBusy)));

            Content = activityIndicator;
        }

        public bool IsBusy
        {
            get => (bool)GetValue(IsBusyProperty);
            set => SetValue(IsBusyProperty, value);
        }
    }
}