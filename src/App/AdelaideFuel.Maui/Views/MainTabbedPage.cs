using AdelaideFuel.Maui.Extensions;
using Sharpnado.Tabs;

namespace AdelaideFuel.Maui.Views
{
    public class MainTabbedPage : ContentPage
    {
        private readonly ViewSwitcher _viewSwitcher;
        private readonly TabHostView _tabHostView;

        public MainTabbedPage() : base()
        {
            SafeAreaEdges = SafeAreaEdges.None;

            _viewSwitcher = new ViewSwitcher()
            {
                Animate = false,
                Children =
                {
                    new PricesTab(),
                    new MapTab(),
                    new SettingsTab(),
                },
                SafeAreaEdges =  SafeAreaEdges.None,
            };

            _tabHostView = new TabHostView();
            _tabHostView.PropertyChanging += (_, args) =>
            {
                if (args.PropertyName == nameof(TabHostView.SelectedIndex))
                {
                    OnPropertyChanging(nameof(SelectedTab));
                    SelectedTab?.OnDisappearing();
                }
            };
            _tabHostView.PropertyChanged += (_, args) =>
            {
                if (args.PropertyName == nameof(TabHostView.SelectedIndex))
                {
                    OnPropertyChanged(nameof(SelectedTab));
                    SelectedTab?.OnAppearing();
                }
            };

            foreach (var i in _viewSwitcher.Children)
            {
                var tab = new BottomTabItem();
                tab.SetBinding(
                    TabTextItem.LabelProperty,
                    static (IBaseTabView i) => i.Title,
                    BindingMode.OneWay,
                    source: i);
                tab.SetBinding(
                    BottomTabItem.IconImageSourceProperty,
                    static (IBaseTabView i) => i.IconImageSource,
                    BindingMode.OneWay,
                    source: i);
                _tabHostView.Tabs.Add(tab);
            }

            _viewSwitcher.SetBinding(
                ViewSwitcher.SelectedIndexProperty,
                static (TabHostView i) => i.SelectedIndex,
                BindingMode.OneWay,
                source: _tabHostView);

            _tabHostView.SelectedIndex = 0;

            var mainGrid = new Grid()
            {
                RowDefinitions =
                [
                    new RowDefinition(GridLength.Auto),
                    new RowDefinition(GridLength.Star),
                    new RowDefinition(GridLength.Auto)
                ],
                SafeAreaEdges = SafeAreaEdges.None,
            };

            var titleContent = new ContentView() { SafeAreaEdges = new SafeAreaEdges(SafeAreaRegions.Container) };
            var titleTrigger = new DataTrigger(typeof(View))
            {
                Binding = Binding.Create(static (ViewSwitcher i) => i.SelectedIndex, mode: BindingMode.OneWay, source: _viewSwitcher),
                Value = 1
            };
            titleTrigger.Setters.Add(new Setter() { Property = View.IsVisibleProperty, Value = false });
            titleContent.Triggers.Add(titleTrigger);

            var titleLbl = new Label();
            titleLbl.SetBinding(
                Label.TextProperty,
                static (MainTabbedPage i) => i.SelectedTab.Title,
                BindingMode.OneWay,
                source: this);
            titleLbl.SetDynamicResource(View.StyleProperty, Styles.Keys.LabelStyle);
            titleLbl.SetDynamicResource(Label.FontFamilyProperty, Styles.Keys.BoldFontFamily);

            titleContent.Content = titleLbl;

            if (DeviceInfo.Platform == DevicePlatform.Android)
            {
                titleContent.SetDynamicResource(View.BackgroundColorProperty, Styles.Keys.PrimaryAccentColor);
                titleContent.Padding = App.Current.FindResource<Thickness>(Styles.Keys.MediumBottomThickness);
                titleLbl.SetDynamicResource(Label.FontSizeProperty, Styles.FontSizes.Title);
                titleLbl.TextColor = Colors.White;
                titleLbl.Padding = App.Current.FindResource<Thickness>(Styles.Keys.MediumLeftThickness);
                titleLbl.HorizontalOptions = LayoutOptions.Start;
                titleLbl.VerticalOptions = LayoutOptions.Center;
            }
            else
            {
                titleContent.SetDynamicResource(View.BackgroundColorProperty, Styles.Keys.PageBackgroundColor);
                titleLbl.Padding = App.Current.FindResource<Thickness>(Styles.Keys.SmallBottomThickness);
                titleLbl.HorizontalOptions = LayoutOptions.Center;
                titleLbl.VerticalOptions = LayoutOptions.Center;
            }
                        
            mainGrid.Add(_viewSwitcher, 0, 1);
            mainGrid.Add(_tabHostView, 0, 2);
            mainGrid.Add(titleContent, 0, 0);

            Content = mainGrid;
        }

        public IBaseTabView SelectedTab =>
            _viewSwitcher.Children[Math.Max(0, _tabHostView.SelectedIndex)] as IBaseTabView;

        public int SelectedIndex
        {
            get => _tabHostView.SelectedIndex;
            set => _tabHostView.SelectedIndex = value;
        }

        public int GetIndexForViewModel(Type viewModelType)
        {
            for (var i = 0; i < _viewSwitcher.Children.Count; i++)
            {
                if (_viewSwitcher.Children[i] is View tab && tab.BindingContext.GetType() == viewModelType)
                    return i;
            }

            return -1;
        }

        public IBaseTabView GetTabForIndex(int index)
        {
            if (index >= 0 && index < _viewSwitcher.Children.Count)
                return _viewSwitcher.Children[index] as IBaseTabView;

            return null;
        }

        public virtual void OnDestroy()
        {
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();

            SelectedTab?.OnAppearing();
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();

            SelectedTab?.OnDisappearing();
        }
    }
}