using AdelaideFuel.Models;
using AdelaideFuel.Services;
using Android.Runtime;
using System;
using System.Threading.Tasks;
using Xamarin.Essentials;

namespace AdelaideFuel.Droid.Services
{
    [Preserve(AllMembers = true)]
    public class UserNativeService_Droid : UserNativeReadOnlyService_Droid, IUserNativeService
    {
        private readonly IStoreFactory _storeFactory;

        public UserNativeService_Droid(
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
            var context = Platform.AppContext;

            try
            {
                var prefs = context.GetSharedPreferences(Constants.AppId, Android.Content.FileCreationMode.Private);
                var editor = prefs.Edit();

                var entities = await _storeFactory.GetUserStore<T>().AllAsync(true, default).ConfigureAwait(false);
                editor.PutString(typeof(T).Name, Newtonsoft.Json.JsonConvert.SerializeObject(entities));

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