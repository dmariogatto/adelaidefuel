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
        private readonly EnumToDescriptionConverter _enumToDescriptionConverter = new EnumToDescriptionConverter();
        private readonly double _separatorSpacing;

        public FuelCategoriesView()
        {
            HorizontalOptions = LayoutOptions.Center;

            ColumnSpacing = RowSpacing = 0;
            RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto });
            RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto });
            RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto });

            _separatorSpacing = !IoC.Resolve<IDeviceDisplay>().IsSmall()
                ? (double)Application.Current.Resources[Styles.Keys.MediumSpacing]
                : (double)Application.Current.Resources[Styles.Keys.XSmallSpacing];

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
            var dataItemCount = FuelCategories?.Count ?? 0;
            var childItemCount = (Children.Count + 1) / 3;

            void createSeparator()
            {
                var separator = new BoxView() { VerticalOptions = LayoutOptions.Fill, WidthRequest = 1 };
                separator.Margin = new Thickness(_separatorSpacing, 0, _separatorSpacing, 0);
                separator.SetDynamicResource(BackgroundColorProperty, Styles.Keys.PrimaryAccentColor);

                ColumnDefinitions.Add(new ColumnDefinition { Width = 1 + _separatorSpacing * 2 });
                Children.Add(separator, ColumnDefinitions.Count - 1, 0);
                SetRowSpan(separator, 2);
            }

            for (var i = childItemCount; i < dataItemCount; i++)
            {
                if (i == childItemCount && childItemCount > 0)
                    createSeparator();

                var tintImg = new TintImage();
                tintImg.HeightRequest = tintImg.WidthRequest = 24;
                tintImg.HorizontalOptions = LayoutOptions.Center;
                tintImg.Source = Application.Current.Resources[Styles.Keys.TwoToneCircleImg]?.ToString();
                tintImg.SetBinding(TintImage.TintColorProperty, new Binding(
                    $"{nameof(FuelCategories)}[{i}].{nameof(FuelCategory.PriceCategory)}",
                    converter: _priceCategoryToColorConverter,
                    source: this));
                tintImg.SetBinding(AutomationProperties.HelpTextProperty, new Binding(
                    $"{nameof(FuelCategories)}[{i}].{nameof(FuelCategory.PriceCategory)}",
                    converter: _enumToDescriptionConverter,
                    source: this));

                var lbl = new Label();
                lbl.SetDynamicResource(StyleProperty, Styles.Keys.LabelStyle);
                lbl.HorizontalOptions = LayoutOptions.Center;
                lbl.FontSize = Device.GetNamedSize(NamedSize.Micro, typeof(Label));
                lbl.SetBinding(Label.TextProperty, new Binding(
                    $"{nameof(FuelCategories)}[{i}].{nameof(FuelCategory.Description)}",
                    source: this));

                ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                Children.Add(tintImg, ColumnDefinitions.Count - 1, 0);
                Children.Add(lbl, ColumnDefinitions.Count - 1, 1);

                if (i < dataItemCount - 1)
                    createSeparator();
            }

            void removeLastChild()
            {
                if (Children.Count > 0)
                    Children.RemoveAt(Children.Count - 1);
            }

            void removeLastColDef()
            {
                if (ColumnDefinitions.Count > 0)
                    ColumnDefinitions.RemoveAt(ColumnDefinitions.Count - 1);
            }

            for (var i = childItemCount; i > dataItemCount; i--)
            {
                removeLastChild();
                removeLastChild();
                removeLastChild();
                removeLastColDef();
                removeLastColDef();
            }
        }

        private void FuelCategoriesSourceChanged(ObservableRangeCollection<FuelCategory> oldValue, ObservableRangeCollection<FuelCategory> newValue)
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