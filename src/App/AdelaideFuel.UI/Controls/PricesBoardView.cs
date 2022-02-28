using AdelaideFuel.Models;
using MvvmHelpers;
using System.Collections.Specialized;
using System.Linq;
using Xamarin.Forms;

namespace AdelaideFuel.UI.Controls
{
    public class PricesBoardView : StackLayout
    {
        public static readonly BindableProperty SiteFuelPricesProperty =
          BindableProperty.Create(
              propertyName: nameof(SiteFuelPrices),
              returnType: typeof(ObservableRangeCollection<SiteFuelPrice>),
              declaringType: typeof(PricesBoardView),
              defaultValue: null,
              propertyChanged: (b, o, n) => ((PricesBoardView)b).SiteFuelPricesSourceChanged((ObservableRangeCollection<SiteFuelPrice>)o, (ObservableRangeCollection<SiteFuelPrice>)n));

        private readonly Grid _pricesGrid;
        private readonly Label _oosLabel;

        public PricesBoardView()
        {
            Spacing = (double)App.Current.Resources[Styles.Keys.XSmallSpacing];

            _pricesGrid = new Grid()
            {
                RowSpacing = 0,
                ColumnSpacing = 0
            };

            _pricesGrid.ColumnDefinitions.Add(new ColumnDefinition());
            _pricesGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = GridLength.Auto });

            Children.Add(_pricesGrid);

            _oosLabel = new Label()
            {
                FontFamily = (string)App.Current.Resources[Styles.Keys.ItalicFontFamily],
                FontSize = Device.GetNamedSize(NamedSize.Small, typeof(Label)),
                HorizontalTextAlignment = TextAlignment.End,
                Text = Localisation.Resources.OosFuels,
            };
            _oosLabel.SetDynamicResource(Label.StyleProperty, Styles.Keys.LabelStyle);
        }

        public ObservableRangeCollection<SiteFuelPrice> SiteFuelPrices
        {
            get => (ObservableRangeCollection<SiteFuelPrice>)GetValue(SiteFuelPricesProperty);
            set => SetValue(SiteFuelPricesProperty, value);
        }

        private void Redraw()
        {
            var dataItemCount = SiteFuelPrices?.Count ?? 0;
            var childItemCount = _pricesGrid.Children.Count / 2;

            for (var i = childItemCount; i < dataItemCount; i++)
            {
                var fuelLabel = new Label()
                {
                    FontFamily = (string)Application.Current.Resources[Styles.Keys.BoldFontFamily],
                    FontSize = Device.GetNamedSize(NamedSize.Medium, typeof(Label)),
                    VerticalTextAlignment = TextAlignment.Center
                };
                fuelLabel.SetDynamicResource(View.StyleProperty, Styles.Keys.LabelStyle);
                fuelLabel.SetBinding(Label.TextProperty, new Binding(
                    $"{nameof(SiteFuelPrices)}[{i}].{nameof(SiteFuelPrice.FuelName)}",
                    source: this));

                var fuelPricePropertyPath = $"{nameof(SiteFuelPrices)}[{i}].{nameof(SiteFuelPrice.PriceInCents)}";
                var priceLabel = new Label()
                {
                    FontSize = Device.GetNamedSize(NamedSize.Large, typeof(Label)),
                    HorizontalOptions = LayoutOptions.End,
                    VerticalTextAlignment = TextAlignment.Center
                };
                priceLabel.SetDynamicResource(View.StyleProperty, Styles.Keys.LabelStyle);
                priceLabel.SetBinding(Label.TextProperty, new Binding(
                    fuelPricePropertyPath,
                    stringFormat: "{0:#.0}",
                    source: this));

                var oosTrigger = new DataTrigger(typeof(View))
                {
                    Binding = new Binding(fuelPricePropertyPath, source: this),
                    Value = Constants.OutOfStockPriceInCents
                };
                oosTrigger.Setters.Add(new Setter() { Property = View.OpacityProperty, Value = (double)App.Current.Resources[Styles.Keys.UnselectedOpacity] });

                priceLabel.Triggers.Add(oosTrigger);

                _pricesGrid.Children.Add(fuelLabel, 0, _pricesGrid.RowDefinitions.Count);
                _pricesGrid.Children.Add(priceLabel, 1, _pricesGrid.RowDefinitions.Count);

                _pricesGrid.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto });
            }

            void removeLastChild()
            {
                if (_pricesGrid.Children.Count > 0)
                    _pricesGrid.Children.RemoveAt(_pricesGrid.Children.Count - 1);
            }

            void removeLastRowDef()
            {
                if (_pricesGrid.RowDefinitions.Count > 0)
                    _pricesGrid.RowDefinitions.RemoveAt(_pricesGrid.RowDefinitions.Count - 1);
            }

            for (var i = childItemCount; i > dataItemCount; i--)
            {
                removeLastChild();
                removeLastChild();
                removeLastRowDef();
            }

            if (SiteFuelPrices?.Any(i => i.PriceInCents == Constants.OutOfStockPriceInCents) == true)
            {
                if (!Children.Contains(_oosLabel))
                    Children.Add(_oosLabel);
            }
            else
            {
                Children.Remove(_oosLabel);
            }
        }

        private void SiteFuelPricesSourceChanged(ObservableRangeCollection<SiteFuelPrice> oldValue, ObservableRangeCollection<SiteFuelPrice> newValue)
        {
            if (oldValue != null)
                oldValue.CollectionChanged -= CollectionChanged;

            if (newValue != null)
                newValue.CollectionChanged += CollectionChanged;

            Redraw();
        }

        private void CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            Redraw();
        }
    }
}