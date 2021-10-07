using AdelaideFuel.Shared;
using AdelaideFuel.TableStore.Entities;
using AdelaideFuel.TableStore.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
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

        public Sites(
            ITableRepository<SiteEntity> siteRepository,
            ITableRepository<SitePriceEntity> sitePriceRepository)
        {
            _siteRepository = siteRepository;
            _sitePriceRepository = sitePriceRepository;
        }

        [FunctionName(nameof(Sites))]
        public async Task<IList<SiteDto>> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "Sites/{brandId?}")] HttpRequest req,
            string brandId,
            ILogger log,
            CancellationToken ct)
        {
            var sites = string.IsNullOrWhiteSpace(brandId)
                ? await _siteRepository.GetAllEntitiesAsync(ct)
                : await _siteRepository.GetPartitionAsync(brandId, ct);
            return sites.Where(s => s.IsActive).Select(s => s.ToSite()).ToList();
        }

        [FunctionName(nameof(EmptySites))]
        public async Task<IList<SiteDto>> EmptySites(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "Sites/Empty")] HttpRequest req,
            ILogger log,
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
                .ToList();

            return emptySites.Select(i => i.ToSite()).ToList();
        }
    }
}