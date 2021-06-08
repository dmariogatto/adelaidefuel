using AdelaideFuel.Localisation;
using AdelaideFuel.Models;
using MvvmHelpers.Commands;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace AdelaideFuel.ViewModels
{
    public class RadiiViewModel : BaseUserEntityViewModel<UserRadius>
    {
        public RadiiViewModel(
            IBvmConstructor bvmConstructor) : base(bvmConstructor)
        {
            Title = Resources.Radii;
            EntityName = Resources.Radius;

            ResetRadiiCommand = new AsyncCommand(ResetRadiiAsync);
            AddRadiusCommand = new AsyncCommand(AddRadiusAsync);
        }

        #region Overrides
        public override void OnAppearing()
        {
            base.OnAppearing();

            TrackEvent(AppCenterEvents.PageView.RadiiView);
        }
        #endregion

        public AsyncCommand ResetRadiiCommand { get; private set; }
        public AsyncCommand AddRadiusCommand { get; private set; }

        private async Task ResetRadiiAsync()
        {
            try
            {
                var confirm = await UserDialogs.ConfirmAsync(
                    Resources.RestoreDefaultSettings,
                    Resources.Reset,
                    Resources.OK,
                    Resources.Cancel);

                if (confirm)
                {
                    var radii = await FuelService.GetUserRadiiAsync(default);
                    await FuelService.RemoveUserRadiiAsync(radii, default);
                    await FuelService.SyncRadiiAsync(default);

                    await LoadEntitiesCommand.ExecuteAsync();
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
        }

        private async Task AddRadiusAsync()
        {
            try
            {
                var prompt = await UserDialogs.PromptAsync(
                    Resources.EnterNewRadius,
                    Resources.Add,
                    Resources.OK,
                    Resources.Cancel,
                    Resources.Kilometres,
                    Acr.UserDialogs.InputType.Number);

                if (prompt?.Ok == true &&
                    int.TryParse(prompt.Value, out var km) &&
                    km > 0 &&
                    Entities.All(i => i.Id != km))
                {
                    var newRadius = new UserRadius()
                    {
                        Id = km,
                        IsActive = true
                    };

                    var insertIdx = Entities.FirstIndexOf(i => km < i.Id);
                    Entities.Insert(Math.Max(0, insertIdx), newRadius);
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
        }
    }
}