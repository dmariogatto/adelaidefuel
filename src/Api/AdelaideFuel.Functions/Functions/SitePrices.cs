using AdelaideFuel.Shared;
using AdelaideFuel.TableStore.Entities;
using AdelaideFuel.TableStore.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Blob;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
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

        private readonly CloudStorageAccount _cloudStorageAccount;

        public SitePrices(
            ITableRepository<SitePriceEntity> sitePriceRepository,
            CloudStorageAccount cloudStorageAccount)
        {
            _sitePricesRepository = sitePriceRepository;

            _cloudStorageAccount = cloudStorageAccount;
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
                (from sp in await GetSitePriceDtosAsync(log, ct)
                 where (!brandIds.Any() || brandIds.Contains(sp.BrandId)) &&
                       (!fuelIds.Any() || fuelIds.Contains(sp.FuelId))
                 select sp).ToList();

            return prices ?? new List<SitePriceDto>(0);
        }

        private async Task<IList<SitePriceDto>> GetSitePriceDtosAsync(ILogger log, CancellationToken ct)
        {
            const string dtosKey = "SitePrices_Entities";

            if (!Cache.TryGetValue(dtosKey, out IList<SitePriceDto> dtos))
            {
                try
                {
                    var blobClient = _cloudStorageAccount.CreateCloudBlobClient();
                    var blobContainer = blobClient.GetContainerReference(Startup.BlobContainerName);
                    var blobSitePrices = blobContainer.GetBlockBlobReference("site_prices.json");
                    if (await blobSitePrices.ExistsAsync())
                    {
                        using var reader = new StreamReader(await blobSitePrices.OpenReadAsync());
                        using var jtr = new JsonTextReader(reader);
                        dtos = JsonSerializer.CreateDefault().Deserialize<List<SitePriceDto>>(jtr);
                    }
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