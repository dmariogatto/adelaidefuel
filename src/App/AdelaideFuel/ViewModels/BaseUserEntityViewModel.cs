using AdelaideFuel.Localisation;
using AdelaideFuel.Models;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AdelaideFuel.ViewModels
{
    public abstract class BaseUserEntityViewModel<T> : BaseViewModel where T : IUserSortableEntity
    {
        private IReadOnlyList<IUserEntity> _originalEntities = [];

        public BaseUserEntityViewModel(
            IBvmConstructor bvmConstructor) : base(bvmConstructor)
        {
            LoadEntitiesCommand = new AsyncRelayCommand(LoadEntitiesAsync);
            SaveEntitiesCommand = new AsyncRelayCommand(SaveEntitiesAsync);
            EntityTappedCommand = new AsyncRelayCommand<IUserSortableEntity>(EntityTappedAsync);
            EntityRemoveCommand = new AsyncRelayCommand<IUserSortableEntity>(EntityRemoveAsync);
        }

        #region Overrides
        public override void OnAppearing()
        {
            base.OnAppearing();

            LoadEntitiesCommand.Execute(null);
        }

        public override void OnDisappearing()
        {
            base.OnDisappearing();

            SaveEntitiesCommand.Execute(null);
        }
        #endregion

        private string _entityName;
        public string EntityName
        {
            get => _entityName;
            set => SetProperty(ref _entityName, value);
        }

        private IReadOnlyList<IUserSortableEntity> _entities;
        public IReadOnlyList<IUserSortableEntity> Entities
        {
            get => _entities;
            protected set => SetProperty(ref _entities, value);
        }

        public AsyncRelayCommand LoadEntitiesCommand { get; private set; }
        public AsyncRelayCommand SaveEntitiesCommand { get; private set; }
        public AsyncRelayCommand<IUserSortableEntity> EntityTappedCommand { get; private set; }
        public AsyncRelayCommand<IUserSortableEntity> EntityRemoveCommand { get; private set; }

        private async Task LoadEntitiesAsync()
        {
            if (IsBusy)
                return;

            IsBusy = true;

            try
            {
                async Task loadEntitiesAsync()
                {
                    Entities = typeof(T) switch
                    {
                        var t when t == typeof(UserBrand) => await FuelService.GetUserBrandsAsync(default),
                        var t when t == typeof(UserFuel) => await FuelService.GetUserFuelsAsync(default),
                        var t when t == typeof(UserRadius) => await FuelService.GetUserRadiiAsync(default),
                        _ => throw new InvalidOperationException($"Unsupported type {typeof(T).Name}")
                    };
                };

                await loadEntitiesAsync();

                if (!Entities.Any())
                {
                    await FuelService.SyncAllAsync(default);
                    await loadEntitiesAsync();
                }

                _originalEntities = Entities.Select(i => i.Clone()).ToList();
                HasError = !Entities.Any();
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

        private async Task SaveEntitiesAsync()
        {
            if (IsBusy)
                return;

            IsBusy = true;

            try
            {
                if (typeof(T) == typeof(UserRadius))
                {
                    var toSave = Entities
                        .Except(_originalEntities)
                        .OfType<UserRadius>()
                        .ToList();
                    var toRemove = _originalEntities
                        .Where(o => Entities.All(e => e.Id != o.Id))
                        .OfType<UserRadius>()
                        .ToList();

                    if (toSave.Count != 0)
                        await FuelService.UpsertUserRadiiAsync(toSave, default);
                    if (toRemove.Count != 0)
                        await FuelService.RemoveUserRadiiAsync(toRemove, default);
                }
                else
                {
                    Entities.ForEach((idx, e) => e.SortOrder = idx);
                    var toSave = Entities.Except(_originalEntities);

                    if (toSave.Any())
                    {
                        switch (typeof(T))
                        {
                            case var t when t == typeof(UserBrand):
                                await FuelService.UpsertUserBrandsAsync(toSave.OfType<UserBrand>().ToList(), default);
                                break;
                            case var t when t == typeof(UserFuel):
                                await FuelService.UpsertUserFuelsAsync(toSave.OfType<UserFuel>().ToList(), default);
                                break;
                            default:
                                throw new InvalidOperationException($"Unsupported type {typeof(T).Name}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task EntityTappedAsync(IUserSortableEntity entity)
        {
            if (entity is null or UserRadius { Id: int.MaxValue })
                return;

            try
            {
                var active = Entities.Where(i => i.IsActive).ToList();
                if (active.Count == 1 && active[0].Equals(entity))
                {
                    await DialogService.AlertAsync(
                        string.Format(Resources.MustHaveAtLeastOneActiveItem, EntityName.ToLower()),
                        Resources.Oops,
                        Resources.OK);
                }
                else
                {
                    entity.IsActive = !entity.IsActive;
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
        }

        private async Task EntityRemoveAsync(IUserSortableEntity entity)
        {
            if (entity is null or UserRadius { Id: int.MaxValue })
                return;

            try
            {
                if (Entities.Count == 1 && Entities[0].Equals(entity))
                {
                    await DialogService.AlertAsync(
                        string.Format(Resources.MustHaveAtLeastOneActiveItem, EntityName.ToLower()),
                        Resources.Oops,
                        Resources.OK);
                }
                else
                {
                    Entities = Entities.Except([entity]).ToList();
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
        }
    }
}