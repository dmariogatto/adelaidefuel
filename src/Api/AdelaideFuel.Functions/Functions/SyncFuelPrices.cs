using AdelaideFuel.Api;
using AdelaideFuel.TableStore.Entities;
using AdelaideFuel.TableStore.Repositories;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Blob;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AdelaideFuel.Functions
{
    public class SyncFuelPrices
    {
        private readonly ISaFuelPricingApi _saFuelPricingApi;

        private readonly ITableRepository<SiteEntity> _siteRepository;
        private readonly ITableRepository<SitePriceEntity> _sitePriceRepository;
        private readonly ITableRepository<SitePriceArchiveEntity> _sitePriceArchiveRepository;

        private readonly CloudStorageAccount _cloudStorageAccount;

        public SyncFuelPrices(
            ISaFuelPricingApi saFuelPricingApi,
            ITableRepository<SiteEntity> siteRepository,
            ITableRepository<SitePriceEntity> sitePriceRepository,
            ITableRepository<SitePriceArchiveEntity> sitePriceArchiveRepository,
            CloudStorageAccount cloudStorageAccount)
        {
            _saFuelPricingApi = saFuelPricingApi;

            _siteRepository = siteRepository;
            _sitePriceRepository = sitePriceRepository;
            _sitePriceArchiveRepository = sitePriceArchiveRepository;

            _cloudStorageAccount = cloudStorageAccount;
        }

        [FunctionName(nameof(SyncFuelPrices))]
        public async Task Run(
            [TimerTrigger("0 */10 * * * *")] TimerInfo myTimer,
            ILogger log,
            CancellationToken ct)
        {
            log.LogInformation($"Starting sync of fuel prices...");

            var sw = new System.Diagnostics.Stopwatch();
            sw.Start();

            var apiPrices = await _saFuelPricingApi.GetPricesAsync(ct);

            sw.Stop();
            log.LogInformation($"Finished API call in {sw.ElapsedMilliseconds}...");
            sw.Start();

            var siteBrands =
                (from site in await _siteRepository.GetAllEntitiesAsync(ct)
                 group site by site.SiteId into sg
                 // sometimes sites change brands
                 let active = sg.FirstOrDefault(s => s.IsActive) ?? sg.First()
                 select (active.SiteId, active.BrandId))
                 .ToDictionary(i => i.SiteId, i => i.BrandId);

            var priceEntities =
                (from sp in apiPrices.SitePrices
                 where siteBrands.ContainsKey(sp.SiteId)
                 let spe = new SitePriceEntity(siteBrands[sp.SiteId], sp)
                 group spe by spe.PartitionKey into g
                 select g)
                .ToDictionary(g => g.Key, g => g.Select(e => e).ToList());

            // The Smoky Bay & Mt Ive Exception, reporting in cents
            if (priceEntities.TryGetValue("12", out var independants))
            {
                var exceptions = new[]
                {
                    // Smoky Bay
                    61577275,
                    // Mt Ive
                    61577323,
                    // Minnipa Community Store
                    61577296,
                };

                foreach (var i in independants.Where(i => i.Price < 500 && exceptions.Contains(i.SiteId)))
                    i.Price *= 10;
            }

            var sitePriceDtos =
                (from vals in priceEntities.Values
                 from sp in vals
                 orderby sp.TransactionDateUtc descending
                 group sp by (sp.SiteId, sp.FuelId) into fuelGroup
                 select fuelGroup.First().ToSitePrice()).ToList();

            try
            {
                log.LogInformation("Updating site prices JSON file...");
                var blobClient = _cloudStorageAccount.CreateCloudBlobClient();
                var blobContainer = blobClient.GetContainerReference(Startup.BlobContainerName);
                await blobContainer.CreateIfNotExistsAsync();
                var blobSitePrices = blobContainer.GetBlockBlobReference("site_prices.json");
                using var writer = new StreamWriter(await blobSitePrices.OpenWriteAsync());
                using var jtw = new JsonTextWriter(writer);
                JsonSerializer.CreateDefault().Serialize(jtw, sitePriceDtos);
                log.LogInformation("Updated site prices JSON file");
            }
            catch (Exception ex)
            {
                log.LogError(ex, "Error writing 'adelaidefuel/site_prices.json'");
            }

            await _sitePriceRepository.CreateIfNotExistsAsync(ct);

            var syncResult = await _sitePriceRepository.SyncPartitionsWithDeleteAsync(priceEntities, log, ct);
            log.LogInformation($"Finished sync of fuel prices in {sw.ElapsedMilliseconds}.");

            var deletedPrices = syncResult.changes.Where(re => !re.IsActive).ToList();
            if (deletedPrices.Any())
            {
                log.LogInformation($"Moving deleted prices to archive...");
                await _sitePriceArchiveRepository.CreateIfNotExistsAsync(ct);
                await _sitePriceArchiveRepository.InsertOrReplaceBulkAsync(deletedPrices.Select(i => new SitePriceArchiveEntity(i.BrandId, i)), log, ct);
            }

            sw.Stop();
            log.LogInformation($"Finished sync in {sw.ElapsedMilliseconds}.");
            log.LogInformation($"Have a nice day 😋");
        }
    }
}