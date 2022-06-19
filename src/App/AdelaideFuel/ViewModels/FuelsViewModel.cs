using AdelaideFuel.Localisation;
using AdelaideFuel.Models;

namespace AdelaideFuel.ViewModels
{
    public class FuelsViewModel : BaseUserEntityViewModel<UserFuel>
    {
        public FuelsViewModel(
            IBvmConstructor bvmConstructor) : base(bvmConstructor)
        {
            Title = Resources.Fuels;
            EntityName = Resources.Fuel;
        }

        #region Overrides
        public override void OnAppearing()
        {
            base.OnAppearing();

            TrackEvent(AppCenterEvents.PageView.FuelsView);
        }
        #endregion
    }
}