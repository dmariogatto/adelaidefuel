using AdelaideFuel.Localisation;
using AdelaideFuel.Models;
using MvvmHelpers;
using MvvmHelpers.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AdelaideFuel.ViewModels
{
    public class FuelsViewModel : BaseViewModel
    {
        public FuelsViewModel(
            IBvmConstructor bvmConstructor) : base(bvmConstructor)
        {
            Title = Resources.Fuels;

            Fuels = new ObservableRangeCollection<UserFuel>();

            LoadFuelsCommand = new AsyncCommand(LoadFuelsAsync);
            SaveFuelsCommand = new AsyncCommand(SaveFuelsAsync);
            FuelTappedCommand = new AsyncCommand<UserFuel>(FuelTappedAsync);
        }

        #region Overrides
        public override void OnAppearing()
        {
            base.OnAppearing();

            LoadFuelsCommand.ExecuteAsync();

            TrackEvent(AppCenterEvents.PageView.FuelsView);
        }

        public override void OnDisappearing()
        {
            base.OnDisappearing();

            SaveFuelsCommand.ExecuteAsync();
        }
        #endregion

        public ObservableRangeCollection<UserFuel> Fuels { get; private set; }

        public AsyncCommand LoadFuelsCommand { get; private set; }
        public AsyncCommand SaveFuelsCommand { get; private set; }
        public AsyncCommand<UserFuel> FuelTappedCommand { get; private set; }

        private async Task LoadFuelsAsync()
        {
            if (IsBusy)
                return;

            IsBusy = true;

            try
            {
                Fuels.ReplaceRange(await FuelService.GetUserFuelsAsync(default));
                HasError = !Fuels.Any();
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                HasError = true;
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task SaveFuelsAsync()
        {
            try
            {
                var fuelsToSave = new List<UserFuel>();

                Fuels.ForEach((idx, fuel) =>
                {
                    if (idx != fuel.SortOrder)
                    {
                        fuel.SortOrder = idx;
                        fuelsToSave.Add(fuel);
                    }
                });

                if (fuelsToSave.Any())
                {
                    await FuelService.UpdateUserFuelsAsync(fuelsToSave, default);
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
        }

        private async Task FuelTappedAsync(UserFuel fuel)
        {
            if (fuel == null)
                return;

            try
            {
                var active = Fuels.Where(i => i.IsActive).ToList();
                if (active.Count == 1 && active.First() == fuel)
                {
                    await UserDialogs.AlertAsync(
                        string.Format(Resources.MustHaveAtLeastOneActiveItem, Resources.Fuel.ToLower()),
                        Resources.Oops,
                        Resources.OK);
                }
                else
                {
                    fuel.IsActive = !fuel.IsActive;
                    await FuelService.UpdateUserFuelsAsync(new[] { fuel }, default);
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
        }
    }
}