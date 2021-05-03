using AdelaideFuel.Models;
using AdelaideFuel.UI.Converters;
using MvvmHelpers;
using System.Collections.Specialized;
using System.Linq;
using Xamarin.Essentials.Interfaces;
using Xamarin.Forms;

namespace AdelaideFuel.UI.Controls
{
    public class FuelCategoriesView : Grid
    {
        public static readonly BindableProperty FuelCategoriesProperty =
          BindableProperty.Create(
              propertyName: nameof(FuelCategoriesView),
              returnType: typeof(ObservableRangeCollection<FuelCategory>),
              declaringType: typeof(FuelCategoriesView),
              defaultValue: null,
              propertyChanged: (b, o, n) => ((FuelCategoriesView)b).FuelCategoriesSourceChanged((ObservableRangeCollection<FuelCategory>)o, (ObservableRangeCollection<FuelCategory>)n));

        private readonly PriceCategoryToColorConverter _priceCategoryToColorConverter = new PriceCategoryToColorConverter();

        public FuelCategoriesView()
        {
            HorizontalOptions = LayoutOptions.Center;

            RowSpacing = 0;
            RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto });
            RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto });
            RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto });

            ColumnSpacing = 0;

            PropertyChanged += (sender, args) =>
            {
                if (args.PropertyName == nameof(IsVisible) && IsVisible)
                    InvalidateLayout();
            };
        }

        public ObservableRangeCollection<FuelCategory> FuelCategories
        {
            get => (ObservableRangeCollection<FuelCategory>)GetValue(FuelCategoriesProperty);
            set => SetValue(FuelCategoriesProperty, value);
        }

        private void Redraw()
        {
            Children.Clear();
            ColumnDefinitions.Clear();

            var separatorSpacing = !IoC.Resolve<IDeviceDisplay>().IsSmall()
                ? (double)Application.Current.Resources[Styles.Keys.MediumSpacing]
                : (double)Application.Current.Resources[Styles.Keys.XSmallSpacing];

            if (FuelCategories?.Any() == true)
            {
                FuelCategories.ForEach((i, v) =>
                {
                    var tintImg = new TintImage();
                    tintImg.HeightRequest = tintImg.WidthRequest = 24;
                    tintImg.HorizontalOptions = LayoutOptions.Center;
                    tintImg.Source = Application.Current.Resources[Styles.Keys.TwoToneCircleImg]?.ToString();
                    tintImg.SetBinding(TintImage.TintColorProperty, new Binding(nameof(v.PriceCategory), converter: _priceCategoryToColorConverter, source: v));
                    AutomationProperties.SetHelpText(tintImg, v.PriceCategory.GetDescription());

                    var lbl = new Label();
                    lbl.SetDynamicResource(StyleProperty, Styles.Keys.LabelStyle);
                    lbl.HorizontalOptions = LayoutOptions.Center;
                    lbl.FontSize = Device.GetNamedSize(NamedSize.Micro, typeof(Label));
                    lbl.SetBinding(Label.TextProperty, new Binding(nameof(v.Description), source: v));

                    ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                    Children.Add(tintImg, ColumnDefinitions.Count - 1, 0);
                    Children.Add(lbl, ColumnDefinitions.Count - 1, 1);

                    if (i < FuelCategories.Count - 1)
                    {
                        var separator = new BoxView() { VerticalOptions = LayoutOptions.FillAndExpand, WidthRequest = 1 };
                        separator.Margin = new Thickness(separatorSpacing, 0, separatorSpacing, 0);
                        separator.SetDynamicResource(BackgroundColorProperty, Styles.Keys.PrimaryAccentColor);

                        ColumnDefinitions.Add(new ColumnDefinition { Width = 1 + separatorSpacing * 2 });
                        Children.Add(separator, ColumnDefinitions.Count - 1, 0);
                        SetRowSpan(separator, 2);
                    }
                });
            }
        }

        private void FuelCategoriesSourceChanged(ObservableRangeCollection<FuelCategory> oldValue, ObservableRangeCollection<FuelCategory> newValue)
        {
            if (oldValue != null)
                newValue.CollectionChanged -= CollectionChanged;

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