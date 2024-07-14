using AdelaideFuel.Shared;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AdelaideFuel.Api
{
    public class AdelaideFuelApi : BaseApi, IAdelaideFuelApi
    {
        public AdelaideFuelApi(HttpClient httpClient) : base(httpClient)
        {
        }

        public Task<List<BrandDto>> GetBrandsAsync(string code, CancellationToken cancellationToken)
        {
            return GetAsync<List<BrandDto>>("Brands", cancellationToken, functionKey: code);
        }

        public Task<List<FuelDto>> GetFuelsAsync(string code, CancellationToken cancellationToken)
        {
            return GetAsync<List<FuelDto>>("Fuels", cancellationToken, functionKey: code);
        }

        public Task<List<SiteDto>> GetSitesAsync(string code, CancellationToken cancellationToken, long? brandId = null)
        {
            var requestUri = brandId.HasValue ? $"Sites/{brandId}" : "Sites";
            return GetAsync<List<SiteDto>>(requestUri, cancellationToken, functionKey: code);
        }

        public Task<List<SitePriceDto>> GetSitePricesAsync(string code, IEnumerable<int> brandIds, IEnumerable<int> fuelIds, CancellationToken cancellationToken)
        {
            var uriBuilder = new StringBuilder();

            if (brandIds?.Any() == true)
            {
                uriBuilder.Append(nameof(brandIds));
                uriBuilder.Append('=');
                uriBuilder.AppendJoin(',', brandIds);
            }

            if (fuelIds?.Any() == true)
            {
                if (uriBuilder.Length > 0) uriBuilder.Append('&');
                uriBuilder.Append(nameof(fuelIds));
                uriBuilder.Append('=');
                uriBuilder.AppendJoin(',', fuelIds);
            }

            uriBuilder.Insert(0, uriBuilder.Length > 0 ? "SitePrices?" : "SitePrices");

            return GetAsync<List<SitePriceDto>>(uriBuilder.ToString(), cancellationToken, functionKey: code);
        }
    }
}