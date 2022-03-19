using AdelaideFuel.Functions.Services;
using AdelaideFuel.Shared;
using AdelaideFuel.TableStore.Entities;
using AdelaideFuel.TableStore.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
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
        public const string PricesJson = "site_prices.json";
        public const string PricesTicksTxt = "site_prices_ticks.txt";
        public const string LastModifiedHeader = "Last-Modified";

        private readonly static MemoryCache Cache = new MemoryCache(new MemoryCacheOptions());
        private readonly static TimeSpan CacheDuration = TimeSpan.FromMinutes(3.5);

        private readonly ITableRepository<SitePriceEntity> _sitePricesRepository;

        private readonly IBlobService _blobService;

        public SitePrices(
            ITableRepository<SitePriceEntity> sitePriceRepository,
            IBlobService blobService)
        {
            _sitePricesRepository = sitePriceRepository;

            _blobService = blobService;
        }

        [FunctionName(nameof(SitePrices))]
        public async Task<IActionResult> GetSitePrices(
            [HttpTrigger(AuthorizationLevel.Function, Route = null)] HttpRequest req,
            ILogger log,
            CancellationToken ct)
        {
            if (long.TryParse(await _blobService.ReadAllTextAsync(SitePrices.PricesTicksTxt, ct), out var ticks))
            {
                var lastModified = new DateTime(ticks, DateTimeKind.Utc);
                req.HttpContext.Response.Headers.Add(LastModifiedHeader, lastModified.ToString("R"));
            }

            if (req.Method == HttpMethods.Head)
                return new OkResult();
            if (req.Method != HttpMethods.Get)
                return new NotFoundResult();

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
                (from sp in await GetSitePriceDtosAsync(log, ct)
                 where (!brandIds.Any() || brandIds.Contains(sp.BrandId)) &&
                       (!fuelIds.Any() || fuelIds.Contains(sp.FuelId))
                 select sp).ToList();

            return new OkObjectResult(prices ?? new List<SitePriceDto>(0));
        }

        private async Task<IList<SitePriceDto>> GetSitePriceDtosAsync(ILogger log, CancellationToken ct)
        {
            const string dtosKey = "SitePrices_Entities";

            if (!Cache.TryGetValue(dtosKey, out IList<SitePriceDto> dtos))
            {
                try
                {
                    dtos = await _blobService.DeserialiseAsync<List<SitePriceDto>>(PricesJson, ct);
                }
                catch (Exception ex)
                {
                    log.LogError(ex, "Error reading 'adelaidefuel/site_prices.json'");
                }

                if (dtos == null || !dtos.Any())
                {
                    dtos =
                        (from sp in await _sitePricesRepository.GetAllEntitiesAsync(ct) ?? Array.Empty<SitePriceEntity>()
                         where sp.IsActive
                         orderby sp.TransactionDateUtc descending
                         group sp by (sp.SiteId, sp.FuelId) into fuelGroup
                         select fuelGroup.First().ToSitePrice()).ToList();
                }

                if (dtos?.Any() == true)
                    Cache.Set(dtosKey, dtos, CacheDuration);
            }

            return dtos ?? Array.Empty<SitePriceDto>();
        }
    }
}