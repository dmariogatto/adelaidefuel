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
        private readonly static TimeSpan CacheDuration = TimeSpan.FromMinutes(3.5);

        private readonly ITableRepository<SitePriceEntity> _sitePricesRepository;

        public SitePrices(
            ITableRepository<SitePriceEntity> sitePriceRepository)
        {
            _sitePricesRepository = sitePriceRepository;
        }

        [FunctionName(nameof(SitePrices))]
        public async Task<IList<SitePriceDto>> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = null)] HttpRequest req,
            ILogger log,
            CancellationToken ct)
        {
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

            var prices =
                (from sp in await GetSitePriceEntitiesAsync(ct)
                 where (!brandIds.Any() || brandIds.Contains(sp.BrandId)) &&
                       (!fuelIds.Any() || fuelIds.Contains(sp.FuelId))
                 select sp.ToSitePrice()).ToList();

            return prices ?? new List<SitePriceDto>(0);
        }

        private async Task<IList<SitePriceEntity>> GetSitePriceEntitiesAsync(CancellationToken ct)
        {
            const string entitiesKey = "SitePrices_Entities";

            if (!Cache.TryGetValue(entitiesKey, out IList<SitePriceEntity> entities))
            {
                entities =
                    (from sp in await _sitePricesRepository.GetAllEntitiesAsync(ct) ?? Array.Empty<SitePriceEntity>()
                     where sp.IsActive
                     orderby sp.TransactionDateUtc descending
                     group sp by (sp.SiteId, sp.FuelId) into fuelGroup
                     select fuelGroup.First()).ToList();
                if (entities?.Any() == true)
                    Cache.Set(entitiesKey, entities, CacheDuration);
            }

            return entities ?? Array.Empty<SitePriceEntity>();
        }
    }
}