using AdelaideFuel.UI.Attributes;
using AdelaideFuel.ViewModels;

namespace AdelaideFuel.UI.Views
{
    [NavigationRoute(NavigationRoutes.Fuels)]
    public partial class RadiiPage : BasePage<RadiiViewModel>
    {
        public RadiiPage() : base()
        {
            InitializeComponent();
        }
    }
}