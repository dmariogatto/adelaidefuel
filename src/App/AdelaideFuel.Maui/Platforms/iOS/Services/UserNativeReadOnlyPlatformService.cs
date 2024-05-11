using AdelaideFuel.Models;
using Foundation;
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

            try
            {
                var groupDefaults = new NSUserDefaults();
                var json = groupDefaults.StringForKey(typeof(T).Name);

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