using AdelaideFuel.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
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

        public async Task<(List<SitePriceDto> Prices, DateTime ModifiedUtc)> GetSitePricesAsync(string code, IEnumerable<int> brandIds, IEnumerable<int> fuelIds, CancellationToken cancellationToken)
        {
            var queryString = GetSitePricesQueryString(brandIds, fuelIds);

            using var request = new HttpRequestMessage(HttpMethod.Get, "SitePrices" + queryString);
            request.Headers.Add(Constants.AuthHeader, code);

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(10));

            using var response = await HttpClient.SendAsync(request, cts.Token).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            var modifiedUtc = response.Content.Headers.LastModified?.UtcDateTime ?? DateTime.UtcNow;

            using var stream = await response.Content.ReadAsStreamAsync(cts.Token).ConfigureAwait(false);
            var result = await JsonSerializer.DeserializeAsync<List<SitePriceDto>>(stream, cancellationToken: cts.Token).ConfigureAwait(false);

            return (result, modifiedUtc);
        }

        public async Task<DateTime> GetSitePricesModifiedUtcAsync(string code, CancellationToken cancellationToken)
        {
            using var request = new HttpRequestMessage(HttpMethod.Head, "SitePrices");
            request.Headers.Add(Constants.AuthHeader, code);

            using var response = await HttpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            var modifiedUtc = response.Content.Headers.LastModified?.UtcDateTime ?? DateTime.UtcNow;
            return modifiedUtc;
        }

        private string GetSitePricesQueryString(IEnumerable<int> brandIds, IEnumerable<int> fuelIds)
        {
            var queryBuilder = new StringBuilder();

            if (brandIds?.Any() == true)
            {
                queryBuilder.Append('?');
                queryBuilder.Append(nameof(brandIds));
                queryBuilder.Append('=');
                queryBuilder.AppendJoin(',', brandIds);
            }

            if (fuelIds?.Any() == true)
            {
                queryBuilder.Append(queryBuilder.Length > 0 ? '&' : '?');
                queryBuilder.Append(nameof(fuelIds));
                queryBuilder.Append('=');
                queryBuilder.AppendJoin(',', fuelIds);
            }

            return queryBuilder.ToString();
        }
    }
}