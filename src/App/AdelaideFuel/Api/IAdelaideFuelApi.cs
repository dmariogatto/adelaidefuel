using AdelaideFuel.Shared;
using Refit;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace AdelaideFuel.Api
{
    public interface IAdelaideFuelApi
    {
        [Get("/Brands")]
        Task<List<BrandDto>> GetBrandsAsync([Header(Constants.AuthHeader)] string code, CancellationToken cancellationToken);

        [Get("/Fuels")]
        Task<List<FuelDto>> GetFuelsAsync([Header(Constants.AuthHeader)] string code, CancellationToken cancellationToken);

        [Get("/Sites/{brandId}")]
        Task<List<SiteDto>> GetSitesAsync([Header(Constants.AuthHeader)] string code, CancellationToken cancellationToken, long? brandId = null);

        [Get("/SitePrices/{siteId}")]
        Task<List<SitePriceDto>> GetSitePricesAsync([Header(Constants.AuthHeader)] string code, IEnumerable<int> brandIds, IEnumerable<int> fuelIds,  CancellationToken cancellationToken, long? siteId = null);
    }
}