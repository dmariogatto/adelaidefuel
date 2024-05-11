using AdelaideFuel.Models;
using Foundation;
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

            try
            {
                var groupDefaults = new NSUserDefaults();

                var entities = await _storeFactory.GetUserStore<T>().AllAsync(true, default).ConfigureAwait(false);
                groupDefaults.SetString(JsonSerializer.Serialize(entities), typeof(T).Name);

                groupDefaults.Synchronize();

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