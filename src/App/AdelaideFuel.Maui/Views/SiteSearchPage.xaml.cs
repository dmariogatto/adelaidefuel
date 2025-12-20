using AdelaideFuel.Models;
using AdelaideFuel.ViewModels;

namespace AdelaideFuel.Maui.Views
{
    public partial class SiteSearchPage : BaseSearchPage<SiteSearchViewModel>, ISearchPage
    {
        public SiteSearchPage() : base()
        {
            InitializeComponent();

            Search.SetBinding(SearchBar.TextProperty, static (ISearchPage i) => i.Query, source: this);
            Search.SetBinding(SearchBar.PlaceholderProperty, static (ISearchPage i) => i.Placeholder, mode: BindingMode.OneWay, source: this);
        }

        private void ItemTapped(object sender, TappedEventArgs e)
        {
            if (Search.IsSoftInputShowing())
            {
                Search.HideSoftInputAsync(CancellationToken.None);
            }

            Search.Unfocus();

            if (sender is View v && v.BindingContext is SiteFuelPrice model)
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

            Search.Unfocus();
        }
    }
}