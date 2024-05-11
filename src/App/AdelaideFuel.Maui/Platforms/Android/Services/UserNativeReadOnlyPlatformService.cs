using AdelaideFuel.Models;
using System.Text.Json;

namespace AdelaideFuel.Services
{
    public class UserNativeReadOnlyPlatformService : IUserNativeReadOnlyService
    {
        protected readonly ILogger Logger;

        public UserNativeReadOnlyPlatformService(ILogger logger)
        {
            Logger = logger;
        }

        public IList<UserBrand> GetUserBrands() => GetList<UserBrand>();

        public IList<UserFuel> GetUserFuels() => GetList<UserFuel>();

        public IList<UserRadius> GetUserRadii() => GetList<UserRadius>();

        private IList<T> GetList<T>() where T : IUserEntity
        {
            var result = default(List<T>);
            var context = Platform.AppContext;

            try
            {
                var prefs = context.GetSharedPreferences(Constants.AppId, Android.Content.FileCreationMode.Private);
                var json = prefs.GetString(typeof(T).Name, string.Empty);

                if (!string.IsNullOrWhiteSpace(json))
                    result = JsonSerializer.Deserialize<List<T>>(json);
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }

            return result;
        }
    }
}