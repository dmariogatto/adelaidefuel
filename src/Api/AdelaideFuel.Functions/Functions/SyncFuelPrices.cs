using AdelaideFuel.Api;
using AdelaideFuel.Functions.Models;
using AdelaideFuel.Functions.Services;
using AdelaideFuel.Shared;
using AdelaideFuel.TableStore.Entities;
using AdelaideFuel.TableStore.Repositories;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Caching.Memory;
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
        private const int OutOfStockPrice = 9999;

        private readonly static MemoryCache Cache = new MemoryCache(new MemoryCacheOptions());
        private readonly static TimeSpan CacheDuration = TimeSpan.FromMinutes(60);

        private readonly ISaFuelPricingApi _saFuelPricingApi;

        private readonly ITableRepository<FuelEntity> _fuelRepository;
        private readonly ITableRepository<SiteEntity> _siteRepository;
        private readonly ITableRepository<SitePriceEntity> _sitePriceRepository;
        private readonly ITableRepository<SitePriceArchiveEntity> _sitePriceArchiveRepository;
        private readonly ITableRepository<SiteExceptionEntity> _siteExceptionRepository;
        private readonly ITableRepository<SitePriceExceptionLogEntity> _sitePriceExceptionLogRepository;

        private readonly ICacheService _cacheService;
        private readonly IBlobService _blobService;
        private readonly ISendGridService _sendGridService;

        public SyncFuelPrices(
            ISaFuelPricingApi saFuelPricingApi,
            ITableRepository<FuelEntity> fuelRepository,
            ITableRepository<SiteEntity> siteRepository,
            ITableRepository<SitePriceEntity> sitePriceRepository,
            ITableRepository<SitePriceArchiveEntity> sitePriceArchiveRepository,
            ITableRepository<SiteExceptionEntity> siteExceptionRepository,
            ITableRepository<SitePriceExceptionLogEntity> sitePriceExceptionLogRepository,
            ICacheService cacheService,
            IBlobService blobService,
            ISendGridService sendGridService)
        {
            _saFuelPricingApi = saFuelPricingApi;

            _fuelRepository = fuelRepository;
            _siteRepository = siteRepository;
            _sitePriceRepository = sitePriceRepository;
            _sitePriceArchiveRepository = sitePriceArchiveRepository;
            _siteExceptionRepository = siteExceptionRepository;
            _sitePriceExceptionLogRepository = sitePriceExceptionLogRepository;

            _cacheService = cacheService;
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

            var siteToBrandTask = GetSiteToBrandMapAsync(default);
            var exceptionSitesTask = _siteExceptionRepository.GetAllEntitiesAsync(ct);

            var previousModifiedUtc = DateTime.UnixEpoch;
            if (long.TryParse(await _blobService.ReadAllTextAsync(SitePrices.PricesTicksTxt, ct), out var ticks))
                previousModifiedUtc = new DateTime(ticks, DateTimeKind.Utc);

            var apiPricesTask = _saFuelPricingApi.GetPricesAsync(ct);

            await Task.WhenAll(
                apiPricesTask,
                siteToBrandTask,
                _sitePriceRepository.CreateIfNotExistsAsync(ct),
                _sitePriceArchiveRepository.CreateIfNotExistsAsync(ct),
                _siteExceptionRepository.CreateIfNotExistsAsync(ct),
                _sitePriceExceptionLogRepository.CreateIfNotExistsAsync(ct));

            log.LogInformation($"Finished API call in {sw.ElapsedMilliseconds}...");

            var apiPricesOrdered = apiPricesTask.Result
                ?.SitePrices
                ?.OrderByDescending(i => i.TransactionDateUtc)
                ?.GroupBy(i => (i.SiteId, i.FuelId))
                ?.Select(g => g.First());
            var modifiedUtc = apiPricesOrdered?.FirstOrDefault()?.TransactionDateUtc ?? DateTime.UnixEpoch;

            if (modifiedUtc > previousModifiedUtc)
            {
                var priceEntities = apiPricesOrdered
                    .Where(i => siteToBrandTask.Result.ContainsKey(i.SiteId))
                    .Select(i => new SitePriceEntity(siteToBrandTask.Result[i.SiteId], i))
                    .ToList();

                // Get sites that have previously reported incorrect pricing
                var exceptionSiteIds = (await exceptionSitesTask)
                    .Where(i => i.IsActive)
                    .Select(i => i.SiteId)
                    .ToHashSet();
                // Find new sites that are possibly incorrect
                var newExceptions = await FindPriceExceptionsAsync(
                        priceEntities.Where(i => !exceptionSiteIds.Contains(i.SiteId)),
                        ct);

                // Add new sites to exception list
                var newSiteExceptions = newExceptions
                    .Select(i => (i.BrandId, i.SiteId, i.SiteName))
                    .Distinct()
                    .Select(i => new SiteExceptionEntity(i.BrandId, i.SiteId, i.SiteName))
                    .ToList();
                await _siteExceptionRepository.InsertOrReplaceBulkAsync(newSiteExceptions, log, ct);
                foreach (var i in newSiteExceptions)
                    exceptionSiteIds.Add(i.SiteId);

                // Get price exceptions & attempt to update
                var exceptionLogEntries = new List<SitePriceExceptionLogEntity>();
                foreach (var i in priceEntities
                                    .Where(i => exceptionSiteIds.Contains(i.SiteId) &&
                                                IsPriceException(i.Price, i.FuelId)))
                {
                    var adjustedPrice = i.Price == 999
                        ? OutOfStockPrice
                        : i.Price * 10;

                    // Price is still invalid - then set to OOS
                    if (IsPriceException(adjustedPrice, i.FuelId))
                        adjustedPrice = OutOfStockPrice;

                    // Log any new changes for later review
                    if (i.TransactionDateUtc > previousModifiedUtc)
                        exceptionLogEntries.Add(new SitePriceExceptionLogEntity(i.BrandId, i, adjustedPrice));

                    i.Price = adjustedPrice;

                    var sitePriceEx = newExceptions
                        .FirstOrDefault(e =>
                            e.BrandId == i.BrandId &&
                            e.SiteId == i.SiteId &&
                            e.FuelId == i.FuelId);
                    if (sitePriceEx is not null)
                        sitePriceEx.AdjustedPrice = adjustedPrice;
                }

                // Notification of newly found exceptions
                var exEmailTask = SendPriceExceptionsEmailAsync(newExceptions, ct);

                var sitePriceDtos = priceEntities.Select(i => i.ToSitePrice());
                var existingPriceEntities = await _sitePriceRepository.GetAllEntitiesAsync(ct);

                var toDelete = existingPriceEntities.Except(priceEntities).ToList();
                var toUpdate = priceEntities.Where(i => i.TransactionDateUtc > previousModifiedUtc).ToList();
                var toArchive = existingPriceEntities
                    .Intersect(toUpdate)
                    .Concat(toDelete)
                    .Select(i => new SitePriceArchiveEntity(i.BrandId, i))
                    .ToList();

                var updateTasks = new List<Task>
                {
                    _blobService.SerialiseAsync(sitePriceDtos, SitePrices.PricesJson, ct),
                    _blobService.WriteAllTextAsync(sitePriceDtos.First().TransactionDateUtc.Ticks.ToString(), SitePrices.PricesTicksTxt, ct),
                    _sitePriceRepository.InsertOrReplaceBulkAsync(toUpdate, log, ct),
                    _sitePriceRepository.DeleteAsync(toDelete, log, ct),
                    _sitePriceExceptionLogRepository.InsertOrReplaceBulkAsync(exceptionLogEntries, log, ct),
                    _sitePriceArchiveRepository.InsertOrReplaceBulkAsync(toArchive, log, ct),
                    exEmailTask
                };

                await Task.WhenAll(updateTasks);

                log.LogInformation($"Finished sync of fuel prices in {sw.ElapsedMilliseconds}.");
            }

            sw.Stop();

            log.LogInformation($"Finished sync in {sw.ElapsedMilliseconds}ms");
            log.LogInformation($"Have a nice day 😋");
        }

        private async Task<IDictionary<int, int>> GetSiteToBrandMapAsync(CancellationToken ct)
        {
            const string cacheKey = "SyncFuelPrices_SiteToBrandMap";

            if (!_cacheService.TryGetValue(cacheKey, out Dictionary<int, int> map))
            {
                map =
                    (from site in await _siteRepository.GetAllEntitiesAsync(ct)
                     group site by site.SiteId into sg
                     // sometimes sites change brands
                     let active = sg.FirstOrDefault(s => s.IsActive) ?? sg.First()
                     select (active.SiteId, active.BrandId))
                    .ToDictionary(i => i.SiteId, i => i.BrandId);

                if (map.Any())
                    _cacheService.SetAbsolute(cacheKey, map, CacheDuration);
            }

            return map ?? new Dictionary<int, int>(0);
        }

        private async Task<IList<SitePriceException>> FindPriceExceptionsAsync(IEnumerable<SitePriceEntity> sitePrices, CancellationToken ct)
        {
            var possibleExceptions = sitePrices
                .Where(i => IsPriceException(i.Price, i.FuelId))
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
                        CurrentPrice = i.Price,
                        AdjustedPrice = -1
                    });
                }
            }

            return exceptions;
        }

        private async Task SendPriceExceptionsEmailAsync(IList<SitePriceException> exceptions, CancellationToken ct)
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
            emailBuilder.AppendFormat(thFmt, "Adjusted Price");
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
                emailBuilder.AppendFormat(tdRightFmt, ex.AdjustedPrice);

                emailBuilder.Append("</tr>");
            }

            emailBuilder.Append("</tbody>");
            emailBuilder.Append("</table>");

            var subject = "New Fuel Price Exceptions";

#if DEBUG
            subject = $"{subject} - DEBUG";
#endif

            await _sendGridService.SendEmailAsync(subject, emailBuilder.ToString());
        }

        private static bool IsPriceException(double price, int fuelId)
            => NumOfIntDigits(price) <= (fuelId == 4 ? 2 : 3);

        private static int NumOfIntDigits(double value)
            => value == 0 ? 1 : 1 + (int)Math.Log10(Math.Abs(value));
    }
}