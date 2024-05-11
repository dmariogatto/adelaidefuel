using AdelaideFuel.Functions.Services;
using AdelaideFuel.Shared;
using AdelaideFuel.TableStore.Entities;
using AdelaideFuel.TableStore.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
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

        private readonly static TimeSpan CacheDuration = TimeSpan.FromMinutes(3);

        private readonly ITableRepository<SitePriceEntity> _sitePricesRepository;

        private readonly ICacheService _cacheService;
        private readonly IBlobService _blobService;

        private ILogger _logger;

        public SitePrices(
            ITableRepository<SitePriceEntity> sitePriceRepository,
            ICacheService cacheService,
            IBlobService blobService,
            ILoggerFactory loggerFactory)
        {
            _sitePricesRepository = sitePriceRepository;

            _cacheService = cacheService;
            _blobService = blobService;
            _logger = loggerFactory.CreateLogger<SitePrices>();
        }

        [Function(nameof(SitePrices))]
        public async Task<IActionResult> GetSitePrices(
            [HttpTrigger(AuthorizationLevel.Function, "head", "get")] HttpRequest req,
            CancellationToken ct)
        {
            if (long.TryParse(await _blobService.ReadAllTextAsync(SitePrices.PricesTicksTxt, ct), out var ticks))
            {
                var lastModified = new DateTime(ticks, DateTimeKind.Utc);
                req.HttpContext.Response.Headers.Append(LastModifiedHeader, lastModified.ToString("R"));
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
                (from sp in await GetSitePriceDtosAsync(_logger, ct)
                 where (!brandIds.Any() || brandIds.Contains(sp.BrandId)) &&
                       (!fuelIds.Any() || fuelIds.Contains(sp.FuelId))
                 select sp);

            return new JsonResult(prices ?? []);
        }

        private async Task<IList<SitePriceDto>> GetSitePriceDtosAsync(ILogger log, CancellationToken ct)
        {
            const string dtosKey = "SitePrices_Entities";

            if (!_cacheService.TryGetValue(dtosKey, out IList<SitePriceDto> dtos))
            {
                try
                {
                    dtos = await _blobService.DeserialiseAsync<List<SitePriceDto>>(PricesJson, ct);
                }
                catch (Exception ex)
                {
                    log.LogError(ex, "Error reading 'adelaidefuel/site_prices.json'");
                }

                if (dtos is null || !dtos.Any())
                {
                    dtos =
                        (from sp in await _sitePricesRepository.GetAllEntitiesAsync(ct) ?? Array.Empty<SitePriceEntity>()
                         where sp.IsActive
                         orderby sp.TransactionDateUtc descending
                         select sp.ToSitePrice()).ToList();
                }

                if (dtos?.Any() == true)
                    _cacheService.SetAbsolute(dtosKey, dtos, CacheDuration);
            }

            return dtos ?? Array.Empty<SitePriceDto>();
        }
    }
}