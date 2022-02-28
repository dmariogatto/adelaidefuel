using AdelaideFuel.Models;
using AdelaideFuel.UI.Attributes;
using AdelaideFuel.ViewModels;
using Xamarin.Forms;

namespace AdelaideFuel.UI.Views
{
    [NavigationRoute(NavigationRoutes.SiteSearch)]
    public partial class SiteSearchPage : BaseSearchPage<SiteSearchViewModel>, ISearchPage
    {
        public SiteSearchPage() : base()
        {
            InitializeComponent();

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