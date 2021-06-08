using AdelaideFuel.Models;
using AdelaideFuel.Services;
using Foundation;
using System;
using System.Threading.Tasks;

namespace AdelaideFuel.iOS.Services
{
    [Preserve(AllMembers = true)]
    public class UserNativeService_iOS : UserNativeReadOnlyService_iOS, IUserNativeService
    {
        private readonly IStoreFactory _storeFactory;

        public UserNativeService_iOS(
            IStoreFactory storeFactory,
            ILogger logger) : base(logger)
        {
            _storeFactory = storeFactory;
        }

        public Task<bool> SyncUserBrandsAsync() => SyncUserDataAsync<UserBrand>();

        public Task<bool> SyncUserFuelsAsync() => SyncUserDataAsync<UserFuel>();

        public Task<bool> SyncUserRadiiAsync() => SyncUserDataAsync<UserRadius>();

        public async Task<bool> SyncUserDataAsync<T>() where T : class, IUserEntity
        {
            var success = false;

            try
            {
                var groupDefaults = new NSUserDefaults();

                var entities = await _storeFactory.GetUserStore<T>().AllAsync(true, default).ConfigureAwait(false);
                groupDefaults.SetString(Newtonsoft.Json.JsonConvert.SerializeObject(entities), typeof(T).Name);

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