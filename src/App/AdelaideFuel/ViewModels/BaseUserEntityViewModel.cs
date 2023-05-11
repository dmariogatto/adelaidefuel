using AdelaideFuel.Localisation;
using AdelaideFuel.Models;
using MvvmHelpers;
using MvvmHelpers.Commands;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace AdelaideFuel.ViewModels
{
    public abstract class BaseUserEntityViewModel<T> : BaseViewModel where T : IUserSortableEntity
    {
        private readonly ObservableRangeCollection<IUserEntity> _originalEntities;

        public BaseUserEntityViewModel(
            IBvmConstructor bvmConstructor) : base(bvmConstructor)
        {
            _originalEntities = new ObservableRangeCollection<IUserEntity>();
            Entities = new ObservableRangeCollection<IUserSortableEntity>();

            LoadEntitiesCommand = new AsyncCommand(LoadEntitiesAsync);
            SaveEntitiesCommand = new AsyncCommand(SaveEntitiesAsync);
            EntityTappedCommand = new AsyncCommand<IUserSortableEntity>(EntityTappedAsync);
            EntityRemoveCommand = new AsyncCommand<IUserSortableEntity>(EntityRemoveAsync);
        }

        #region Overrides
        public override void OnAppearing()
        {
            base.OnAppearing();

            LoadEntitiesCommand.ExecuteAsync();
        }

        public override void OnDisappearing()
        {
            base.OnDisappearing();

            SaveEntitiesCommand.ExecuteAsync();
        }
        #endregion

        private string _entityName;
        public string EntityName
        {
            get => _entityName;
            set => SetProperty(ref _entityName, value);
        }

        public ObservableRangeCollection<IUserSortableEntity> Entities { get; private set; }

        public AsyncCommand LoadEntitiesCommand { get; private set; }
        public AsyncCommand SaveEntitiesCommand { get; private set; }
        public AsyncCommand<IUserSortableEntity> EntityTappedCommand { get; private set; }
        public AsyncCommand<IUserSortableEntity> EntityRemoveCommand { get; private set; }

        private async Task LoadEntitiesAsync()
        {
            if (IsBusy)
                return;

            IsBusy = true;

            try
            {
                async Task loadEntitiesAsync()
                {
                    // iOS binding issue when reseting radii
                    Entities.Clear();

                    switch (typeof(T))
                    {
                        case Type t when t == typeof(UserBrand):
                            Entities.ReplaceRange(await FuelService.GetUserBrandsAsync(default));
                            break;
                        case Type t when t == typeof(UserFuel):
                            Entities.ReplaceRange(await FuelService.GetUserFuelsAsync(default));
                            break;
                        case Type t when t == typeof(UserRadius):
                            Entities.ReplaceRange(await FuelService.GetUserRadiiAsync(default));
                            break;
                        default:
                            throw new InvalidOperationException($"Unsupported type {typeof(T).Name}");
                    }
                };

                await loadEntitiesAsync();

                if (!Entities.Any())
                {
                    await FuelService.SyncAllAsync(default);
                    await loadEntitiesAsync();
                }

                _originalEntities.ReplaceRange(Entities.Select(i => i.Clone()));
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
                    var toSave = Entities.Except(_originalEntities).ToList();
                    var toRemove = _originalEntities.Where(o => Entities.All(e => e.Id != o.Id)).ToList();

                    if (toSave.Any())
                        await FuelService.UpsertUserRadiiAsync(toSave.OfType<UserRadius>().ToList(), default);
                    if (toRemove.Any())
                        await FuelService.RemoveUserRadiiAsync(toRemove.OfType<UserRadius>().ToList(), default);
                }
                else
                {
                    Entities.ForEach((idx, e) => e.SortOrder = idx);
                    var toSave = Entities.Except(_originalEntities).ToList();

                    if (toSave.Any())
                    {
                        switch (typeof(T))
                        {
                            case Type t when t == typeof(UserBrand):
                                await FuelService.UpsertUserBrandsAsync(toSave.OfType<UserBrand>().ToList(), default);
                                break;
                            case Type t when t == typeof(UserFuel):
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
            if (entity is null || (entity is UserRadius ur && ur.Id == int.MaxValue))
                return;

            try
            {
                var active = Entities.Where(i => i.IsActive).ToList();
                if (active.Count == 1 && active.First() == entity)
                {
                    await UserDialogs.AlertAsync(
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
            if (entity is null || (entity is UserRadius ur && ur.Id == int.MaxValue))
                return;

            try
            {
                if (Entities.Count == 1 && Entities.First() == entity)
                {
                    await UserDialogs.AlertAsync(
                        string.Format(Resources.MustHaveAtLeastOneActiveItem, EntityName.ToLower()),
                        Resources.Oops,
                        Resources.OK);
                }
                else
                {
                    Entities.Remove(entity);
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
        }
    }
}