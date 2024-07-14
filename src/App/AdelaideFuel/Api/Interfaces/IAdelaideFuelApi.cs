using AdelaideFuel.Shared;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace AdelaideFuel.Api
{
    public interface IAdelaideFuelApi
    {
        Task<List<BrandDto>> GetBrandsAsync(string code, CancellationToken cancellationToken);

        Task<List<FuelDto>> GetFuelsAsync(string code, CancellationToken cancellationToken);

        Task<List<SiteDto>> GetSitesAsync(string code, CancellationToken cancellationToken, long? brandId = null);

        Task<List<SitePriceDto>> GetSitePricesAsync(string code, IEnumerable<int> brandIds, IEnumerable<int> fuelIds, CancellationToken cancellationToken);
    }
}