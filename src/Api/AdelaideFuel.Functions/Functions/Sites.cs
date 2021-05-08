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

        public Sites(
            ITableRepository<SiteEntity> siteRepository)
        {
            _siteRepository = siteRepository;
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
    }
}