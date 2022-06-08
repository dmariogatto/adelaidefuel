using AdelaideFuel.UI.Attributes;
using AdelaideFuel.ViewModels;
using Xamarin.Forms;

namespace AdelaideFuel.UI.Views
{
    [NavigationRoute(NavigationRoutes.Subscription)]
    public partial class SubscriptionPage : BasePage<SubscriptionViewModel>
    {
        public SubscriptionPage() : base()
        {
            InitializeComponent();
        }
    }
}