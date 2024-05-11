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

            Search.SetBinding(SearchBar.TextProperty, new Binding(nameof(Query), source: this));
            Search.SetBinding(SearchBar.PlaceholderProperty, new Binding(nameof(Placeholder), source: this));
        }

        private void ListViewItemTapped(object sender, ItemTappedEventArgs e)
        {
            if (e.Item is SiteFuelPrice model)
            {
                ViewModel.TappedCommand.ExecuteAsync(model);
            }
        }
    }
}