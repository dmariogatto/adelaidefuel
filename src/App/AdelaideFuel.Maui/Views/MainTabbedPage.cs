using AdelaideFuel.Maui.Controls;
using AdelaideFuel.Maui.Extensions;
using AdelaideFuel.Services;
using System.ComponentModel;
using PropertyChangingEventArgs = Microsoft.Maui.Controls.PropertyChangingEventArgs;

namespace AdelaideFuel.Maui.Views
{
    public class MainTabbedPage : ContentPage
    {
        private readonly List<View> _tabViews = new List<View>();
        private readonly BottomTabControl _bottomTabs = new BottomTabControl();

        public MainTabbedPage() : base()
        {
            SafeAreaEdges = SafeAreaEdges.None;

            _tabViews.Add(new PricesTab());
            _tabViews.Add(new MapTab());
            _tabViews.Add(new SettingsTab());

            for (var i = 0; i < _tabViews.Count; i++)
            {
                _tabViews[i].IsVisible = false;
                var viewTrigger = new DataTrigger(typeof(View))
                {
                    Binding = Binding.Create(static (BottomTabControl i) => i.SelectedIndex, mode: BindingMode.OneWay, source: _bottomTabs),
                    Value = i
                };
                viewTrigger.Setters.Add(new Setter() { Property = View.IsVisibleProperty, Value = true });
                _tabViews[i].Triggers.Add(viewTrigger);
            }

            _bottomTabs.SetDynamicResource(BottomTabControl.BackgroundColorProperty, Styles.Keys.CardBackgroundColor);
            _bottomTabs.SetDynamicResource(BottomTabControl.PrimaryColorProperty, Styles.Keys.PrimaryAccentColor);

            foreach (var i in _tabViews)
            {
                var tab = new BottomTabItem();

                tab.SetDynamicResource(BottomTabItem.UnselectedColorProperty, Styles.Keys.UnselectedTabColor);
                tab.SetDynamicResource(BottomTabItem.SelectedColorProperty, Styles.Keys.PrimaryAccentColor);

                tab.SetBinding(
                    BottomTabItem.TextProperty,
                    static (IBaseTabView i) => i.Title,
                    BindingMode.OneWay,
                    source: i);
                tab.SetBinding(
                    BottomTabItem.IconSourceProperty,
                    static (IBaseTabView i) => i.IconImageSource,
                    BindingMode.OneWay,
                    source: i);

                _bottomTabs.Children.Add(tab);
            }

            var mainGrid = new Grid()
            {
                RowDefinitions =
                [
                    new RowDefinition(GridLength.Auto),
                    new RowDefinition(GridLength.Star),
                    new RowDefinition(GridLength.Auto)
                ],
                SafeAreaEdges = new SafeAreaEdges(SafeAreaRegions.None, SafeAreaRegions.None, SafeAreaRegions.None, SafeAreaRegions.Default),
            };
            mainGrid.SetDynamicResource(BackgroundColorProperty, Styles.Keys.CardBackgroundColor);

            var titleContent = new ContentView() { SafeAreaEdges = new SafeAreaEdges(SafeAreaRegions.Container) };
            foreach (var i in new[] { _tabViews.FirstIndexOf(i => i is MapTab) })
            {
                if (i < 0)
                    continue;

                var titleTrigger = new DataTrigger(typeof(View))
                {
                    Binding = Binding.Create(static (BottomTabControl i) => i.SelectedIndex, mode: BindingMode.OneWay, source: _bottomTabs),
                    Value = i
                };
                titleTrigger.Setters.Add(new Setter() { Property = View.IsVisibleProperty, Value = false });
                titleContent.Triggers.Add(titleTrigger);
            }

            var titleLbl = new Label();
            titleLbl.SetBinding(
                Label.TextProperty,
                static (MainTabbedPage i) => i.SelectedTab?.Title,
                BindingMode.OneWay,
                source: this);
            titleLbl.SetDynamicResource(View.StyleProperty, Styles.Keys.LabelStyle);
            titleLbl.SetDynamicResource(Label.FontFamilyProperty, Styles.Keys.BoldFontFamily);

            titleContent.Content = titleLbl;

            if (DeviceInfo.Platform == DevicePlatform.Android)
            {
                titleContent.SetDynamicResource(View.BackgroundColorProperty, Styles.Keys.NavigationBarColor);
                titleContent.Padding = App.Current.FindResource<Thickness>(Styles.Keys.MediumThickness);
                titleLbl.SetDynamicResource(Label.FontSizeProperty, Styles.FontSizes.Title);
                titleLbl.TextColor = Colors.White;
                titleLbl.HorizontalOptions = LayoutOptions.Start;
                titleLbl.VerticalOptions = LayoutOptions.Center;
            }
            else
            {
                titleContent.SetDynamicResource(View.BackgroundColorProperty, Styles.Keys.PageBackgroundColor);
                titleLbl.Padding = App.Current.FindResource<Thickness>(Styles.Keys.SmallThickness);
                titleLbl.HorizontalOptions = LayoutOptions.Center;
                titleLbl.VerticalOptions = LayoutOptions.Center;
            }

            foreach (var v in _tabViews)
                mainGrid.Add(v, 0, 1);

            mainGrid.Add(_bottomTabs, 0, 2);
            mainGrid.Add(titleContent, 0, 0);

            this.SetBinding(Page.TitleProperty, static (MainTabbedPage i) => i.SelectedTab?.Title, BindingMode.OneWay, source: this);

            Content = mainGrid;
        }

        public IBaseTabView SelectedTab =>
            _tabViews[Math.Max(0, _bottomTabs.SelectedIndex)] as IBaseTabView;

        public int SelectedIndex
        {
            get => _bottomTabs.SelectedIndex;
            set => _bottomTabs.SelectedIndex = value;
        }

        public int GetIndexForViewModel(Type viewModelType)
        {
            for (var i = 0; i < _tabViews.Count; i++)
            {
                if (_tabViews[i].BindingContext.GetType() == viewModelType)
                    return i;
            }

            return -1;
        }

        public IBaseTabView GetTabForIndex(int index)
        {
            if (index >= 0 && index < _tabViews.Count)
                return _tabViews[index] as IBaseTabView;

            return null;
        }

        public virtual void OnDestroy()
        {
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();

            SelectedTab?.OnAppearing();

            _bottomTabs.PropertyChanging += BottomTabsOnPropertyChanging;
            _bottomTabs.PropertyChanged += BottomTabsOnPropertyChanged;
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();

            SelectedTab?.OnDisappearing();

            _bottomTabs.PropertyChanging -= BottomTabsOnPropertyChanging;
            _bottomTabs.PropertyChanged -= BottomTabsOnPropertyChanged;
        }

        private void BottomTabsOnPropertyChanging(object sender, PropertyChangingEventArgs e)
        {
            if (e.PropertyName == nameof(BottomTabControl.SelectedIndex))
            {
                OnPropertyChanging(nameof(SelectedTab));
                SelectedTab?.OnDisappearing();
            }
        }

        private void BottomTabsOnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(BottomTabControl.SelectedIndex))
            {
                OnPropertyChanged(nameof(SelectedTab));
                SelectedTab?.OnAppearing();
            }
        }
    }
}