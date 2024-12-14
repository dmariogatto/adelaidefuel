using Acr.UserDialogs;
using AdelaideFuel.Maui.Converters;
using AdelaideFuel.Maui.Extensions;
using AdelaideFuel.Models;
using AdelaideFuel.Services;

namespace AdelaideFuel.Maui.Controls
{
    public class FuelSelectionView : Border
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

        private static readonly HeightToRoundRectangleConverter ToRoundRectangleConverter = new HeightToRoundRectangleConverter();

        private readonly IUserDialogs _userDialogs;
        private readonly ILogger _logger;

        public FuelSelectionView()
        {
            SetDynamicResource(StyleProperty, Styles.Keys.CardBorderStyle);

            Margin = Thickness.Zero;
            HorizontalOptions = LayoutOptions.Center;
            VerticalOptions = LayoutOptions.Start;

            this.SetBinding(
                StrokeShapeProperty,
                static (FuelSelectionView i) => i.Height,
                converter: ToRoundRectangleConverter,
                mode: BindingMode.OneWay,
                source: RelativeBindingSource.Self);

            var fuelImg = new TintImage()
            {
                Margin = App.Current.FindResource<Thickness>(Styles.Keys.SmallLeftThickness),
                HeightRequest = 24,
                Source = App.Current.FindResource<string>(Styles.Keys.FuelImg),
                VerticalOptions = LayoutOptions.Center,
                WidthRequest = 24
            };
            fuelImg.SetDynamicResource(TintImage.TintColorProperty, Styles.Keys.TintColor);

            var fuelLbl = new Label()
            {
                Margin = App.Current.FindResource<Thickness>(Styles.Keys.SmallRightThickness),
                VerticalTextAlignment = TextAlignment.Center,
            };
            fuelLbl.SetDynamicResource(Label.StyleProperty, Styles.Keys.LabelStyle);
            fuelLbl.SetBinding(Label.TextProperty, static (FuelSelectionView i) => i.SelectedFuel.Name, mode: BindingMode.OneWay, source: this);

            var stackLayout = new HorizontalStackLayout()
            {
                Spacing = App.Current.FindResource<double>(Styles.Keys.XSmallSpacing)
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