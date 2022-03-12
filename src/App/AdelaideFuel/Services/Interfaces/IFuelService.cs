using AdelaideFuel.Models;
using AdelaideFuel.Shared;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.Essentials;

namespace AdelaideFuel.Services
{
    public interface IFuelService
    {
        Task<IList<BrandDto>> GetBrandsAsync(CancellationToken cancellationToken);
        Task<IList<FuelDto>> GetFuelsAsync(CancellationToken cancellationToken);

        Task<IList<SiteDto>> GetSitesAsync(CancellationToken cancellationToken);
        Task<IList<SiteDto>> GetSitesAsync(int brandId, CancellationToken cancellationToken);

        Task<(IList<SiteFuelPrice> prices, DateTime modifiedUtc)> GetSitePricesAsync(CancellationToken cancellationToken);

        Task<(IList<SiteFuelPriceItemGroup> groups, Location location, DateTime modifiedUtc)> GetFuelPricesByRadiusAsync(CancellationToken cancellationToken);

        Task<bool> SyncBrandsAsync(CancellationToken cancellationToken);
        Task<bool> SyncFuelsAsync(CancellationToken cancellationToken);
        Task<bool> SyncRadiiAsync(CancellationToken cancellationToken);

        Task<bool> SyncAllAsync(CancellationToken cancellationToken);

        Task<IList<UserBrand>> GetUserBrandsAsync(CancellationToken cancellationToken);
        Task<int> UpsertUserBrandsAsync(IList<UserBrand> brands, CancellationToken cancellationToken);

        Task<IList<UserFuel>> GetUserFuelsAsync(CancellationToken cancellationToken);
        Task<int> UpsertUserFuelsAsync(IList<UserFuel> fuels, CancellationToken cancellationToken);

        Task<IList<UserRadius>> GetUserRadiiAsync(CancellationToken cancellationToken);
        Task<int> UpsertUserRadiiAsync(IList<UserRadius> radii, CancellationToken cancellationToken);
        Task<int> RemoveUserRadiiAsync(IList<UserRadius> radii, CancellationToken cancellationToken);
    }
}