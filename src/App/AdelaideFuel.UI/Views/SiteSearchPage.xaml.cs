using AdelaideFuel.Shared;
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

            SetBinding(QueryProperty, new Binding(nameof(ViewModel.SearchText)));
        }

        private void ListViewItemTapped(object sender, ItemTappedEventArgs e)
        {
            if (e.Item is SiteDto model)
            {
                ViewModel.TappedCommand.ExecuteAsync(model);
            }
        }
    }
}