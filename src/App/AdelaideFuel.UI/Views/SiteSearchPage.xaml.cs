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
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();

            ViewModel.SearchText = null;
        }

        protected override void OnSizeAllocated(double width, double height)
        {
            base.OnSizeAllocated(width, height);

            if (width > 0 && height > 0)
            {
                ListHeaderView.Padding = new Thickness(0, SelectedFuelView.Height, 0, 0);
            }
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