using AdelaideFuel.Models;
using MvvmHelpers;
using System.Collections.Specialized;
using System.Linq;
using Xamarin.Forms;

namespace AdelaideFuel.UI.Controls
{
    public class PricesBoardView : Grid
    {
        public static readonly BindableProperty SiteFuelPricesProperty =
          BindableProperty.Create(
              propertyName: nameof(SiteFuelPrices),
              returnType: typeof(ObservableRangeCollection<SiteFuelPrice>),
              declaringType: typeof(PricesBoardView),
              defaultValue: null,
              propertyChanged: (b, o, n) => ((PricesBoardView)b).SiteFuelPricesSourceChanged((ObservableRangeCollection<SiteFuelPrice>)o, (ObservableRangeCollection<SiteFuelPrice>)n));

        public PricesBoardView()
        {
            RowSpacing = 0;
            ColumnSpacing = 0;

            ColumnDefinitions.Add(new ColumnDefinition());
            ColumnDefinitions.Add(new ColumnDefinition() { Width = GridLength.Auto });
        }

        public ObservableRangeCollection<SiteFuelPrice> SiteFuelPrices
        {
            get => (ObservableRangeCollection<SiteFuelPrice>)GetValue(SiteFuelPricesProperty);
            set => SetValue(SiteFuelPricesProperty, value);
        }

        private void Redraw()
        {
            Children.Clear();
            RowDefinitions.Clear();

            if (SiteFuelPrices?.Any() == true)
            {
                foreach (var item in SiteFuelPrices)
                {
                    var fuelLabel = new Label()
                    {
                        FontFamily = (string)Application.Current.Resources[Styles.Keys.BoldFontFamily],
                        FontSize = Device.GetNamedSize(NamedSize.Medium, typeof(Label)),
                        VerticalTextAlignment = TextAlignment.Center
                    };
                    fuelLabel.SetDynamicResource(View.StyleProperty, Styles.Keys.LabelStyle);
                    fuelLabel.SetBinding(Label.TextProperty, new Binding(nameof(SiteFuelPrice.FuelName), source: item));

                    var priceLabel = new Label()
                    {
                        FontSize = Device.GetNamedSize(NamedSize.Large, typeof(Label)),
                        HorizontalOptions = LayoutOptions.End,
                        VerticalTextAlignment = TextAlignment.Center
                    };
                    priceLabel.SetDynamicResource(View.StyleProperty, Styles.Keys.LabelStyle);
                    priceLabel.SetBinding(Label.TextProperty, new Binding(nameof(SiteFuelPrice.PriceInCents), stringFormat: "{0:#.0}", source: item));

                    Children.Add(fuelLabel, 0, RowDefinitions.Count);
                    Children.Add(priceLabel, 1, RowDefinitions.Count);

                    RowDefinitions.Add(new RowDefinition() {  Height = GridLength.Auto });
                }
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