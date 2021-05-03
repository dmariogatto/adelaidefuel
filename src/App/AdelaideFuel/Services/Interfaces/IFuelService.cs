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
        Task<IList<SiteFuelPrice>> GetSitePricesAsync(int siteId, CancellationToken cancellationToken);

        Task<IList<SiteFuelPriceItemGroup>> GetFuelPricesByRadiusAsync(int[] radiiKm, CancellationToken cancellationToken);

        Task<IList<UserBrand>> GetUserBrandsAsync(CancellationToken cancellationToken);
        Task UpdateUserBrandsAsync(IList<UserBrand> brands, CancellationToken cancellationToken);

        Task<IList<UserFuel>> GetUserFuelsAsync(CancellationToken cancellationToken);
        Task UpdateUserFuelsAsync(IList<UserFuel> fuels, CancellationToken cancellationToken);
    }
}