using AdelaideFuel.Models;
using System.Collections.Generic;

namespace AdelaideFuel.Services
{
    public interface IUserNativeReadOnlyService
    {
        IList<UserBrand> GetUserBrands();
        IList<UserFuel> GetUserFuels();
        IList<UserRadius> GetUserRadii();
    }
}