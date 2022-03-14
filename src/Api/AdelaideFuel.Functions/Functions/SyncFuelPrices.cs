using AdelaideFuel.Api;
using AdelaideFuel.Functions.Models;
using AdelaideFuel.Functions.Services;
using AdelaideFuel.TableStore.Entities;
using AdelaideFuel.TableStore.Repositories;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AdelaideFuel.Functions
{
    public class SyncFuelPrices
    {
        private readonly ISaFuelPricingApi _saFuelPricingApi;

        private readonly ITableRepository<FuelEntity> _fuelRepository;
        private readonly ITableRepository<SiteEntity> _siteRepository;
        private readonly ITableRepository<SitePriceEntity> _sitePriceRepository;
        private readonly ITableRepository<SitePriceArchiveEntity> _sitePriceArchiveRepository;

        private readonly IBlobService _blobService;
        private readonly ISendGridService _sendGridService;

        public SyncFuelPrices(
            ISaFuelPricingApi saFuelPricingApi,
            ITableRepository<FuelEntity> fuelRepository,
            ITableRepository<SiteEntity> siteRepository,
            ITableRepository<SitePriceEntity> sitePriceRepository,
            ITableRepository<SitePriceArchiveEntity> sitePriceArchiveRepository,
            IBlobService blobService,
            ISendGridService sendGridService)
        {
            _saFuelPricingApi = saFuelPricingApi;

            _fuelRepository = fuelRepository;
            _siteRepository = siteRepository;
            _sitePriceRepository = sitePriceRepository;
            _sitePriceArchiveRepository = sitePriceArchiveRepository;

            _blobService = blobService;
            _sendGridService = sendGridService;
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

            var exceptionSiteIds = new HashSet<int>()
            {
                // Tinty Auto & Ag
                61501446,
                // Smoky Bay
                61577275,
                // Mt Ive
                61577323,
                // Minnipa Community Store
                61577296,
                // Liberty Autopro
                61501603,
            };

            var exceptionSitePrices =
                (from g in priceEntities
                 from pe in g.Value
                 where exceptionSiteIds.Contains(pe.SiteId) &&
                       pe.Price < 500
                 select pe).ToList();

            foreach(var i in exceptionSitePrices)
                i.Price *= 10;

            var exceptions = await FindPossibleExceptionsAsync(
                    priceEntities
                        .SelectMany(i => i.Value)
                        .Where(i => !exceptionSiteIds.Contains(i.SiteId)),
                    ct);
            var exEmailTask = SendPossibleExceptionsEmailAsync(exceptions, ct);

            var sitePriceDtos =
                (from vals in priceEntities.Values
                 from sp in vals
                 orderby sp.TransactionDateUtc descending
                 group sp by (sp.SiteId, sp.FuelId) into fuelGroup
                 select fuelGroup.First().ToSitePrice()).ToList();

            var modifiedUtc = sitePriceDtos.FirstOrDefault()?.TransactionDateUtc ?? DateTime.UnixEpoch;

            var previousModifiedUtc = DateTime.MinValue;
            DateTime.TryParseExact(
                await _blobService.ReadAllTextAsync(SitePrices.PricesLastModifiedTxt, ct),
                CultureInfo.InvariantCulture.DateTimeFormat.RFC1123Pattern,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AdjustToUniversal,
                out previousModifiedUtc);

            if (modifiedUtc > previousModifiedUtc)
            {
                try
                {
                    log.LogInformation("Updating site prices JSON file...");
                    await _blobService.SerialiseAsync(sitePriceDtos, SitePrices.PricesJson, ct);

                    await _blobService.WriteAllTextAsync(modifiedUtc.ToString("R", CultureInfo.InvariantCulture.DateTimeFormat), SitePrices.PricesLastModifiedTxt, ct);

                    log.LogInformation($"Updated site prices JSON file in {sw.ElapsedMilliseconds}.");
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

                await exEmailTask;
            }

            sw.Stop();
            log.LogInformation($"Finished sync in {sw.ElapsedMilliseconds}ms");
            log.LogInformation($"Have a nice day 😋");
        }

        private async Task<IList<SitePriceException>> FindPossibleExceptionsAsync(IEnumerable<SitePriceEntity> sitePrices, CancellationToken ct)
        {
            var possibleExceptions = sitePrices
                .Where(i => NumOfIntDigits(i.Price) <= (i.FuelId == 4 ? 2 : 3))
                .ToList();

            if (!possibleExceptions.Any())
                return Array.Empty<SitePriceException>();

            var previousSitePricesTask = _sitePriceRepository.GetEntitiesAsync(possibleExceptions.Select(i => (i.PartitionKey, i.RowKey)).ToList(), ct);
            var sitesTask = _siteRepository.GetEntitiesAsync(possibleExceptions.Select(i => (i.BrandId.ToString(CultureInfo.InvariantCulture), i.SiteId.ToString(CultureInfo.InvariantCulture))).ToList(), ct);
            var fuelsTask = _fuelRepository.GetAllEntitiesAsync(ct);

            await Task.WhenAll(previousSitePricesTask, sitesTask, fuelsTask);

            var previousSitePrices = previousSitePricesTask.Result.ToDictionary(i => (i.SiteId, i.FuelId), i => i);
            var sites = sitesTask.Result.ToDictionary(i => (i.BrandId, i.SiteId), i => i);
            var fuels = fuelsTask.Result.ToDictionary(i => i.FuelId, i => i);

            var exceptions = new List<SitePriceException>();

            foreach (var i in possibleExceptions)
            {
                previousSitePrices.TryGetValue((i.SiteId, i.FuelId), out var previous);
                sites.TryGetValue((i.BrandId, i.SiteId), out var site);
                fuels.TryGetValue(i.FuelId, out var fuel);

                if (i.Price != previous?.Price)
                {
                    exceptions.Add(new SitePriceException()
                    {
                        BrandId = i.BrandId,
                        SiteId = i.SiteId,
                        FuelId = i.FuelId,
                        SiteName = site?.Name ?? string.Empty,
                        FuelName = fuel?.Name ?? string.Empty,
                        PreviousPrice = previous?.Price ?? -1,
                        CurrentPrice = i.Price
                    });
                }
            }

            return exceptions;
        }

        private async Task SendPossibleExceptionsEmailAsync(IList<SitePriceException> exceptions, CancellationToken ct)
        {
            if (!exceptions.Any())
                return;

            const string thFmt = "<td><b>{0}</b></td>";
            const string tdFmt = "<td>{0}</td>";
            const string tdRightFmt = "<td style=\"text-align:right\">{0}</td>";

            var emailBuilder = new StringBuilder();
            emailBuilder.Append("<table cellpadding=\"6\" border=\"1\">");
            emailBuilder.Append("<thead>");
            emailBuilder.AppendFormat(thFmt, "Brand Id");
            emailBuilder.AppendFormat(thFmt, "Site Id");
            emailBuilder.AppendFormat(thFmt, "Site Name");
            emailBuilder.AppendFormat(thFmt, "Fuel Id");
            emailBuilder.AppendFormat(thFmt, "Fuel Name");
            emailBuilder.AppendFormat(thFmt, "Previous Price");
            emailBuilder.AppendFormat(thFmt, "Current Price");
            emailBuilder.Append("</thead>");

            emailBuilder.Append("<tbody>");

            foreach (var ex in exceptions)
            {
                emailBuilder.Append("<tr>");

                emailBuilder.AppendFormat(tdFmt, ex.BrandId);
                emailBuilder.AppendFormat(tdFmt, ex.SiteId);
                emailBuilder.AppendFormat(tdFmt, ex?.SiteName ?? string.Empty);
                emailBuilder.AppendFormat(tdFmt, ex.FuelId);
                emailBuilder.AppendFormat(tdFmt, ex.FuelName ?? string.Empty);
                emailBuilder.AppendFormat(tdRightFmt, ex.PreviousPrice);
                emailBuilder.AppendFormat(tdRightFmt, ex.CurrentPrice);

                emailBuilder.Append("</tr>");
            }

            emailBuilder.Append("</tbody>");
            emailBuilder.Append("</table>");

            await _sendGridService.SendEmailAsync("Possible Fuel Price Exceptions", emailBuilder.ToString());
        }

        private static int NumOfIntDigits(double value)
            => value == 0 ? 1 : 1 + (int)Math.Log10(Math.Abs(value));
    }
}