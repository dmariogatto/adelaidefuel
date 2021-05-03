using AdelaideFuel.Shared;
using AdelaideFuel.TableStore.Entities;
using AdelaideFuel.TableStore.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AdelaideFuel.Functions
{
    public class SitePrices
    {
        private readonly static MemoryCache Cache = new MemoryCache(new MemoryCacheOptions());
        private readonly static TimeSpan CacheDuration = TimeSpan.FromMinutes(3);

        private readonly ITableRepository<SitePriceEntity> _sitePricesRepository;

        public SitePrices(
            ITableRepository<SitePriceEntity> sitePriceRepository)
        {
            _sitePricesRepository = sitePriceRepository;
        }

        [FunctionName(nameof(SitePrices))]
        public async Task<IList<SitePriceDto>> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "SitePrices/{siteId?}")] HttpRequest req,
            string siteId,
            ILogger log,
            CancellationToken ct)
        {
            const string pricesKey = "SitePrices_DTOs";

            const string brandIdsKey = "brandIds";
            const string fuelIdsKey = "fuelIds";

            var brandIds = new HashSet<int>();
            var fuelIds = new HashSet<int>();

            if (req.Query.ContainsKey(brandIdsKey))
                foreach (var i in req.Query[brandIdsKey].ToString().Split(','))
                    if (int.TryParse(i, out var id)) brandIds.Add(id);
            if (req.Query.ContainsKey(fuelIdsKey))
                foreach (var i in req.Query[fuelIdsKey].ToString().Split(','))
                    if (int.TryParse(i, out var id)) fuelIds.Add(id);

            var dtoCacheEnabled = !brandIds.Any() && !fuelIds.Any();
            if (!dtoCacheEnabled || !Cache.TryGetValue(pricesKey, out IList<SitePriceDto> prices))
            {
                prices =
                    (from sp in await GetSitePriceEntitiesAsync(siteId, ct)
                     where sp.IsActive &&
                           sp.TransactionDateUtc > DateTime.UtcNow.AddDays(-2) &&
                           (!brandIds.Any() || brandIds.Contains(sp.BrandId)) &&
                           (!fuelIds.Any() || fuelIds.Contains(sp.FuelId))
                     orderby sp.TransactionDateUtc descending
                     group sp by (sp.SiteId, sp.FuelId) into fuelGroup
                     select fuelGroup.First().ToSitePrice()).ToList();

                if (dtoCacheEnabled && prices?.Any() == true)
                    Cache.Set(pricesKey, prices, CacheDuration);
            }

            return prices ?? Array.Empty<SitePriceDto>();
        }

        private async Task<IList<SitePriceEntity>> GetSitePriceEntitiesAsync(string siteId, CancellationToken ct)
        {
            const string entitiesKey = "SitePrices_Entities";

            var entitiesCacheKey = string.IsNullOrWhiteSpace(siteId) ? entitiesKey : $"{entitiesKey}_{siteId}";
            if (!Cache.TryGetValue(entitiesCacheKey, out IList<SitePriceEntity> entities))
            {
                entities = string.IsNullOrWhiteSpace(siteId)
                    ? await _sitePricesRepository.GetAllEntitiesAsync(ct)
                    : await _sitePricesRepository.GetPartitionAsync(siteId, ct);

                if (entities?.Any() == true)
                    Cache.Set(entitiesCacheKey, entities, CacheDuration);
            }

            return entities ?? Array.Empty<SitePriceEntity>();
        }
    }
}
