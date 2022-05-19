using Acr.UserDialogs;
using AdelaideFuel.Models;
using AdelaideFuel.Services;
using AdelaideFuel.UI.Converters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xamarin.Forms;
using Xamarin.Forms.PancakeView;

namespace AdelaideFuel.UI.Controls
{
    public class FuelSelectionView : PancakeView
    {
        public static readonly BindableProperty SelectedFuelProperty =
          BindableProperty.Create(
              propertyName: nameof(SelectedFuel),
              returnType: typeof(UserFuel),
              declaringType: typeof(FuelSelectionView),
              defaultBindingMode: BindingMode.TwoWay,
              defaultValue: default,
              propertyChanged: null);

        public static readonly BindableProperty FuelsSourceProperty =
         BindableProperty.Create(
             propertyName: nameof(FuelsSource),
             returnType: typeof(IList<UserFuel>),
             declaringType: typeof(FuelSelectionView),
             defaultValue: default,
             propertyChanged: (b, o, n) => OnFuelsSourceChanged((FuelSelectionView)b, (IList<UserFuel>)o, (IList<UserFuel>)n));

        private readonly IUserDialogs _userDialogs;
        private readonly ILogger _logger;

        public FuelSelectionView()
        {
            SetDynamicResource(PancakeView.StyleProperty, Styles.Keys.CardStyle);

            Margin = new Thickness(0);
            Padding = (Thickness)App.Current.Resources[Styles.Keys.SmallThickness];
            HorizontalOptions = LayoutOptions.Center;

            SetBinding(PancakeView.CornerRadiusProperty, new Binding(
                nameof(Height),
                converter: new MultiplyByConverter(),
                converterParameter: 0.5d,
                source: this));

            var fuelImg = new TintImage()
            {
                Margin = (Thickness)App.Current.Resources[Styles.Keys.SmallLeftThickness],
                HeightRequest = 24,
                Source = (string)App.Current.Resources[Styles.Keys.FuelImg],
                VerticalOptions = LayoutOptions.Center,
                WidthRequest = 24
            };
            fuelImg.SetDynamicResource(TintImage.TintColorProperty, Styles.Keys.TintColor);

            var fuelLbl = new Label()
            {
                Margin = (Thickness)App.Current.Resources[Styles.Keys.SmallRightThickness],
                VerticalTextAlignment = TextAlignment.Center,
            };
            fuelLbl.SetDynamicResource(Label.StyleProperty, Styles.Keys.LabelStyle);
            fuelLbl.SetBinding(Label.TextProperty, new Binding($"{nameof(SelectedFuel)}.{nameof(SelectedFuel.Name)}", source: this));

            var stackLayout = new StackLayout()
            {
                Orientation = StackOrientation.Horizontal,
                Spacing = (double)App.Current.Resources[Styles.Keys.XSmallSpacing]
            };

            stackLayout.Children.Add(fuelImg);
            stackLayout.Children.Add(fuelLbl);

            Content = stackLayout;

            _userDialogs = IoC.Resolve<IUserDialogs>();
            _logger = IoC.Resolve<ILogger>();

            var tapGesture = new TapGestureRecognizer();
            tapGesture.Tapped += (s, e) =>
            {
                _ = ChangeFuelAsync();
            };
            GestureRecognizers.Add(tapGesture);
        }

        public UserFuel SelectedFuel
        {
            get => (UserFuel)GetValue(SelectedFuelProperty);
            set => SetValue(SelectedFuelProperty, value);
        }

        public IList<UserFuel> FuelsSource
        {
            get => (IList<UserFuel>)GetValue(FuelsSourceProperty);
            set => SetValue(FuelsSourceProperty, value);
        }

        private async Task ChangeFuelAsync()
        {
            if (FuelsSource is null)
                return;

            try
            {
                var fuelNames = FuelsSource.Select(i => i.Name).ToArray();
                var result = await _userDialogs.ActionSheetAsync(
                    Localisation.Resources.Fuels,
                    Localisation.Resources.Cancel,
                    default,
                    default,
                    fuelNames);

                if (!string.IsNullOrEmpty(result) &&
                    Array.IndexOf(fuelNames, result) is int idx &&
                    idx >= 0)
                    SelectedFuel = FuelsSource[idx];
            }
            catch (Exception ex)
            {
                _logger.Error(ex);
            }
        }

        private static void OnFuelsSourceChanged(FuelSelectionView view, IList<UserFuel> oldValue, IList<UserFuel> newValue)
        {
            view.SelectedFuel ??= newValue?.FirstOrDefault();

            if (view.SelectedFuel is not null && (newValue is null || !newValue.Contains(view.SelectedFuel)))
                view.SelectedFuel = null;
        }
    }
}