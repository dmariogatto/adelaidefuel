using AdelaideFuel.Models;
using AdelaideFuel.Services;
using Foundation;
using System;
using System.Collections.Generic;

namespace AdelaideFuel.iOS.Services
{
    [Preserve(AllMembers = true)]
    public class UserNativeReadOnlyService_iOS : IUserNativeReadOnlyService
    {
        protected readonly ILogger Logger;

        public UserNativeReadOnlyService_iOS(ILogger logger)
        {
            Logger = logger;
        }

        public IList<UserBrand> GetUserBrands() => GetList<UserBrand>();

        public IList<UserFuel> GetUserFuels() => GetList<UserFuel>();

        private IList<T> GetList<T>() where T : IUserEntity
        {
            var result = default(List<T>);

            try
            {
                var groupDefaults = new NSUserDefaults();
                var json = groupDefaults.StringForKey(typeof(T).Name);

                if (!string.IsNullOrWhiteSpace(json))
                    result = Newtonsoft.Json.JsonConvert.DeserializeObject<List<T>>(json);
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }

            return result;
        }
    }
}