using AdelaideFuel.Localisation;
using AdelaideFuel.Models;
using AdelaideFuel.Services;
using CommunityToolkit.Mvvm.Input;
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

            ResetRadiiCommand = new AsyncRelayCommand(ResetRadiiAsync);
            AddRadiusCommand = new AsyncRelayCommand(AddRadiusAsync);
        }

        #region Overrides
        public override void OnAppearing()
        {
            base.OnAppearing();

            TrackEvent(Events.PageView.RadiiView);
        }
        #endregion

        public AsyncRelayCommand ResetRadiiCommand { get; private set; }
        public AsyncRelayCommand AddRadiusCommand { get; private set; }

        private async Task ResetRadiiAsync()
        {
            try
            {
                var confirm = await DialogService.ConfirmAsync(
                    Resources.RestoreDefaultSettings,
                    Resources.Reset,
                    Resources.OK,
                    Resources.Cancel);

                if (confirm)
                {
                    var radii = await FuelService.GetUserRadiiAsync(default);
                    await FuelService.RemoveUserRadiiAsync(radii, default);
                    await FuelService.SyncRadiiAsync(default);

                    await LoadEntitiesCommand.ExecuteAsync(null);
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
                var prompt = await DialogService.PromptAsync(
                    Resources.EnterNewRadius,
                    Resources.Add,
                    Resources.OK,
                    Resources.Cancel,
                    Resources.Kilometres,
                    keyboard: KeyboardType.Numeric);

                if (prompt is not null &&
                    int.TryParse(prompt, out var km) &&
                    km > 0 &&
                    Entities.All(i => i.Id != km))
                {
                    var newRadius = new UserRadius()
                    {
                        Id = km,
                        IsActive = true
                    };

                    var insertIdx = Entities.FirstIndexOf(i => km < i.Id);
                    var entities = Entities.ToList();
                    entities.Insert(Math.Max(0, insertIdx), newRadius);

                    Entities = entities;
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
        }
    }
}