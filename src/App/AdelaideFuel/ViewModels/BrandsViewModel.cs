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
    public class BrandsViewModel : BaseViewModel
    {
        public BrandsViewModel(
            IBvmConstructor bvmConstructor) : base(bvmConstructor)
        {
            Title = Resources.Brands;

            Brands = new ObservableRangeCollection<UserBrand>();

            LoadBrandsCommand = new AsyncCommand(LoadBrandsAsync);
            SaveBrandsCommand = new AsyncCommand(SaveBrandsAsync);
            BrandTappedCommand = new AsyncCommand<UserBrand>(BrandTappedAsync);
        }

        #region Overrides
        public override void OnAppearing()
        {
            base.OnAppearing();

            LoadBrandsCommand.ExecuteAsync();

            TrackEvent(AppCenterEvents.PageView.BrandsView);
        }

        public override void OnDisappearing()
        {
            base.OnDisappearing();

            SaveBrandsCommand.ExecuteAsync();
        }
        #endregion

        public ObservableRangeCollection<UserBrand> Brands { get; private set; }

        public AsyncCommand LoadBrandsCommand { get; private set; }
        public AsyncCommand SaveBrandsCommand { get; private set; }
        public AsyncCommand<UserBrand> BrandTappedCommand { get; private set; }

        private async Task LoadBrandsAsync()
        {
            if (IsBusy)
                return;

            IsBusy = true;

            try
            {
                Brands.ReplaceRange(await FuelService.GetUserBrandsAsync(default));
                HasError = !Brands.Any();
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

        private async Task SaveBrandsAsync()
        {
            try
            {
                var brandsToSave = new List<UserBrand>();

                Brands.ForEach((idx, brand) =>
                {
                    if (idx != brand.SortOrder)
                    {
                        brand.SortOrder = idx;
                        brandsToSave.Add(brand);
                    }
                });

                if (brandsToSave.Any())
                {
                    await FuelService.UpdateUserBrandsAsync(brandsToSave, default);
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
        }

        private async Task BrandTappedAsync(UserBrand brand)
        {
            if (brand == null)
                return;

            try
            {
                var active = Brands.Where(i => i.IsActive).ToList();
                if (active.Count == 1 && active.First() == brand)
                {
                    await UserDialogs.AlertAsync(
                        string.Format(Resources.MustHaveAtLeastOneActiveItem, Resources.Brand.ToLower()),
                        Resources.Error,
                        Resources.OK);
                }
                else
                {
                    brand.IsActive = !brand.IsActive;
                    await FuelService.UpdateUserBrandsAsync(new[] { brand }, default);
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
        }
    }
}