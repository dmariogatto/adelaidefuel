using AdelaideFuel.Models;
using AdelaideFuel.Shared;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace AdelaideFuel.Services
{
    public interface IFuelService
    {
        Task<IList<BrandDto>> GetBrandsAsync(CancellationToken cancellationToken);
        Task<IList<FuelDto>> GetFuelsAsync(CancellationToken cancellationToken);

        Task<IList<SiteDto>> GetSitesAsync(CancellationToken cancellationToken);
        Task<IList<SiteDto>> GetSitesAsync(int brandId, CancellationToken cancellationToken);

        Task<IList<SiteFuelPrice>> GetSitePricesAsync(CancellationToken cancellationToken);

        Task<IList<SiteFuelPriceItemGroup>> GetFuelPricesByRadiusAsync(CancellationToken cancellationToken);

        Task SyncBrandsAsync(CancellationToken cancellationToken);
        Task SyncFuelsAsync(CancellationToken cancellationToken);
        Task SyncRadiiAsync(CancellationToken cancellationToken);

        Task SyncAllAsync(CancellationToken cancellationToken);

        Task<IList<UserBrand>> GetUserBrandsAsync(CancellationToken cancellationToken);
        Task UpsertUserBrandsAsync(IList<UserBrand> brands, CancellationToken cancellationToken);

        Task<IList<UserFuel>> GetUserFuelsAsync(CancellationToken cancellationToken);
        Task UpsertUserFuelsAsync(IList<UserFuel> fuels, CancellationToken cancellationToken);

        Task<IList<UserRadius>> GetUserRadiiAsync(CancellationToken cancellationToken);
        Task UpsertUserRadiiAsync(IList<UserRadius> radii, CancellationToken cancellationToken);
        Task RemoveUserRadiiAsync(IList<UserRadius> radii, CancellationToken cancellationToken);
    }
}