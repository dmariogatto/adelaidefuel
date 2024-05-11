using AdelaideFuel.Shared;
using AdelaideFuel.TableStore.Entities;
using AdelaideFuel.TableStore.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AdelaideFuel.Functions
{
    public class Sites
    {
        private readonly ITableRepository<SiteEntity> _siteRepository;
        private readonly ITableRepository<SitePriceEntity> _sitePriceRepository;

        private readonly ILogger _logger;

        public Sites(
            ITableRepository<SiteEntity> siteRepository,
            ITableRepository<SitePriceEntity> sitePriceRepository,
            ILoggerFactory loggerFactory)
        {
            _siteRepository = siteRepository;
            _sitePriceRepository = sitePriceRepository;
            _logger = loggerFactory.CreateLogger<Sites>();
        }

        [Function(nameof(Sites))]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "Sites/{brandId?}")] HttpRequest req,
            string brandId,
            CancellationToken ct)
        {
            var sites = string.IsNullOrWhiteSpace(brandId)
                ? await _siteRepository.GetAllEntitiesAsync(ct)
                : await _siteRepository.GetPartitionAsync(brandId, ct);
            var projected = sites.Where(s => s.IsActive).Select(s => s.ToSite());
            return new JsonResult(projected);
        }

        [Function(nameof(EmptySites))]
        public async Task<IActionResult> EmptySites(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "Sites/Empty")] HttpRequest req,
            CancellationToken ct)
        {
            var sitesTask = _siteRepository.GetAllEntitiesAsync(ct);
            var sitePricesTask = _sitePriceRepository.GetAllEntitiesAsync(ct);

            await Task.WhenAll(sitesTask, sitePricesTask);

            var activeSiteIds = new HashSet<int>(sitePricesTask.Result
                .Where(i => i.IsActive)
                .Select(i => i.SiteId));

            var emptySites = sitesTask.Result
                .Where(i => i.IsActive && !activeSiteIds.Contains(i.SiteId))
                .Select(i => i.ToSite());

            return new JsonResult(emptySites);
        }
    }
}