using AdelaideFuel.UI.Attributes;
using AdelaideFuel.ViewModels;

namespace AdelaideFuel.UI.Views
{
    [NavigationRoute(NavigationRoutes.Fuels)]
    public partial class FuelsPage : BasePage<FuelsViewModel>
    {
        public FuelsPage() : base()
        {
            InitializeComponent();
        }
    }
}