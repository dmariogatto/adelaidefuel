using AdelaideFuel.Maui.Effects;
using AdelaideFuel.Models;
using AdelaideFuel.ViewModels;

namespace AdelaideFuel.Maui.Views
{
    public partial class SiteSearchPage : BaseSearchPage<SiteSearchViewModel>, ISearchPage
    {
        public SiteSearchPage() : base()
        {
            InitializeComponent();

            if (DeviceInfo.Current.Platform == DevicePlatform.Android)
            {
                Search.SetDynamicResource(SearchBar.BackgroundColorProperty, Styles.Keys.CardBackgroundColor);
                Search.SetDynamicResource(SearchBarIconEffect.TintColorProperty, Styles.Keys.SecondaryTextColor);
                Search.Effects.Add(new SearchBarIconEffect());
            }
            else
            {
                Search.SetDynamicResource(SearchBar.BackgroundColorProperty, Styles.Keys.PageBackgroundColor);
            }

            Search.SetBinding(SearchBar.TextProperty, static (ISearchPage i) => i.Query, source: this);
            Search.SetBinding(SearchBar.PlaceholderProperty, static (ISearchPage i) => i.Placeholder, mode: BindingMode.OneWay, source: this);
        }

        private void ListViewItemTapped(object sender, ItemTappedEventArgs e)
        {
            if (e.Item is SiteFuelPrice model)
            {
                ViewModel.TappedCommand.ExecuteAsync(model);
            }
        }

        private void OnSearchButtonPressed(object sender, EventArgs e)
        {
            if (Search.IsSoftInputShowing())
            {
                Search.HideSoftInputAsync(CancellationToken.None);
            }
        }
    }
}