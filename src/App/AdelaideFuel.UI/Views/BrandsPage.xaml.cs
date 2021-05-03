using AdelaideFuel.UI.Attributes;
using AdelaideFuel.ViewModels;

namespace AdelaideFuel.UI.Views
{
    [NavigationRoute(NavigationRoutes.Brands)]
    public partial class BrandsPage : BasePage<BrandsViewModel>
    {
        public BrandsPage() : base()
        {
            InitializeComponent();
        }
    }
}