using AdelaideFuel.Models;
using AdelaideFuel.Shared;
using Microsoft.Maui.Devices.Sensors;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace AdelaideFuel.Services
{
    public interface IFuelService
    {
        Task<IReadOnlyList<BrandDto>> GetBrandsAsync(CancellationToken cancellationToken);
        Task<IReadOnlyList<FuelDto>> GetFuelsAsync(CancellationToken cancellationToken);

        Task<IReadOnlyList<SiteDto>> GetSitesAsync(CancellationToken cancellationToken);
        Task<IReadOnlyList<SiteDto>> GetSitesAsync(int brandId, CancellationToken cancellationToken);

        Task<(IReadOnlyList<SiteFuelPrice> prices, DateTime modifiedUtc)> GetSitePricesAsync(CancellationToken cancellationToken);

        Task<(IReadOnlyList<SiteFuelPriceItemGroup> groups, Location location, DateTime modifiedUtc)> GetFuelPricesByRadiusAsync(CancellationToken cancellationToken);

        Task<bool> SyncBrandsAsync(CancellationToken cancellationToken);
        Task<bool> SyncFuelsAsync(CancellationToken cancellationToken);
        Task<bool> SyncRadiiAsync(CancellationToken cancellationToken);

        Task<bool> SyncAllAsync(CancellationToken cancellationToken);

        Task<IReadOnlyList<UserBrand>> GetUserBrandsAsync(CancellationToken cancellationToken);
        Task<int> UpsertUserBrandsAsync(IReadOnlyList<UserBrand> brands, CancellationToken cancellationToken);

        Task<IReadOnlyList<UserFuel>> GetUserFuelsAsync(CancellationToken cancellationToken);
        Task<int> UpsertUserFuelsAsync(IReadOnlyList<UserFuel> fuels, CancellationToken cancellationToken);

        Task<IReadOnlyList<UserRadius>> GetUserRadiiAsync(CancellationToken cancellationToken);
        Task<int> UpsertUserRadiiAsync(IReadOnlyList<UserRadius> radii, CancellationToken cancellationToken);
        Task<int> RemoveUserRadiiAsync(IReadOnlyList<UserRadius> radii, CancellationToken cancellationToken);
    }
}