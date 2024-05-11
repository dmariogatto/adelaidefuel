using AdelaideFuel.Models;
using System.Text.Json;

namespace AdelaideFuel.Services
{
    public class UserNativePlatformService : UserNativeReadOnlyPlatformService, IUserNativeService
    {
        private readonly IStoreFactory _storeFactory;

        public UserNativePlatformService(
            IStoreFactory storeFactory,
            ILogger logger) : base(logger)
        {
            _storeFactory = storeFactory;
        }

        public Task<bool> SyncUserBrandsAsync() => SyncUserDataAsync<UserBrand>();

        public Task<bool> SyncUserFuelsAsync() => SyncUserDataAsync<UserFuel>();

        public Task<bool> SyncUserRadiiAsync() => SyncUserDataAsync<UserRadius>();

        private async Task<bool> SyncUserDataAsync<T>() where T : class, IUserEntity
        {
            var success = false;
            var context = Platform.AppContext;

            try
            {
                var prefs = context.GetSharedPreferences(Constants.AppId, Android.Content.FileCreationMode.Private);
                var editor = prefs.Edit();

                var entities = await _storeFactory.GetUserStore<T>().AllAsync(true, default).ConfigureAwait(false);
                editor.PutString(typeof(T).Name, JsonSerializer.Serialize(entities));

                editor.Commit();

                success = true;
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }

            return success;
        }
    }
}