using AdelaideFuel.Localisation;
using AdelaideFuel.Models;

namespace AdelaideFuel.ViewModels
{
    public class BrandsViewModel : BaseUserEntityViewModel<UserBrand>
    {
        public BrandsViewModel(
            IBvmConstructor bvmConstructor) : base(bvmConstructor)
        {
            Title = Resources.Brands;
            EntityName = Resources.Brand;
        }

        #region Overrides
        public override void OnCreate()
        {
            base.OnCreate();

            TrackEvent(AppCenterEvents.PageView.BrandsView);
        }
        #endregion
    }
}