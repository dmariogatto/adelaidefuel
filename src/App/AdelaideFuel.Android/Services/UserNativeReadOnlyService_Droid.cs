using AdelaideFuel.Models;
using Android.Runtime;
using System;
using System.Collections.Generic;
using Xamarin.Essentials;

namespace AdelaideFuel.Services
{
    [Preserve(AllMembers = true)]
    public class UserNativeReadOnlyService_Droid : IUserNativeReadOnlyService
    {
        protected readonly ILogger Logger;

        public UserNativeReadOnlyService_Droid(ILogger logger)
        {
            Logger = logger;
        }

        public IList<UserBrand> GetUserBrands() => GetList<UserBrand>();

        public IList<UserFuel> GetUserFuels() => GetList<UserFuel>();

        public IList<UserFuel> GetUserRadii() => GetList<UserRadius>();

        private IList<T> GetList<T>() where T : IUserEntity
        {
            var result = default(List<T>);
            var context = Platform.AppContext;

            try
            {
                var prefs = context.GetSharedPreferences(Constants.AndroidId, Android.Content.FileCreationMode.Private);
                var json = prefs.GetString(typeof(T).Name, string.Empty);

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