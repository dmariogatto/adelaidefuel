﻿using AdelaideFuel.Api;
using AdelaideFuel.TableStore.Entities;
using AdelaideFuel.TableStore.Repositories;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AdelaideFuel.Functions
{
    public class SyncFuelData
    {
        private readonly ISaFuelPricingApi _saFuelPricingApi;

        private readonly ITableRepository<BrandEntity> _brandRepository;
        private readonly ITableRepository<FuelEntity> _fuelRepository;
        private readonly ITableRepository<GeographicRegionEntity> _geographicRegionRepository;
        private readonly ITableRepository<SiteEntity> _siteRepository;
        private readonly ITableRepository<SitePriceEntity> _sitePriceRepository;

        public SyncFuelData(
            ISaFuelPricingApi saFuelPricingApi,
            ITableRepository<BrandEntity> brandRepository,
            ITableRepository<FuelEntity> fuelRepository,
            ITableRepository<GeographicRegionEntity> geographicRegionRepository,
            ITableRepository<SiteEntity> siteRepository,
            ITableRepository<SitePriceEntity> sitePriceRepository)
        {
            _saFuelPricingApi = saFuelPricingApi;

            _brandRepository = brandRepository;
            _fuelRepository = fuelRepository;
            _geographicRegionRepository = geographicRegionRepository;
            _siteRepository = siteRepository;
            _sitePriceRepository = sitePriceRepository;
        }

        [FunctionName(nameof(SyncFuelData))]
        public async Task Run(
            [TimerTrigger("0 30 12 * * *")] TimerInfo myTimer,
            ILogger log,
            CancellationToken ct)
        {
            log.LogInformation($"Starting sync of static fuel data...");

            var sw = new System.Diagnostics.Stopwatch();
            sw.Start();

            var brandsTask = _saFuelPricingApi.GetBrandsAsync(ct);
            var fuelsTask = _saFuelPricingApi.GetFuelTypesAsync(ct);
            var geographicRegionsTask = _saFuelPricingApi.GetCountryGeographicRegionsAsync(ct);
            var sitesTask = _saFuelPricingApi.GetFullSiteDetailsAsync(ct);
            var pricesTask = _sitePriceRepository.GetAllEntitiesAsync(ct);

            await Task.WhenAll(brandsTask, fuelsTask, geographicRegionsTask, sitesTask, pricesTask);

            sw.Stop();
            log.LogInformation($"Finished API calls in {sw.ElapsedMilliseconds}...");
            sw.Start();

            var brandEntities =
                (from b in brandsTask.Result.Brands
                 where sitesTask.Result.Sites.Any(s => s.BrandId == b.BrandId)
                 let be = new BrandEntity(b)
                 group be by be.PartitionKey into g
                 select g)
                .ToDictionary(g => g.Key, g => g.Take(1).ToList());
            var fuelEntities =
                (from f in fuelsTask.Result.Fuels
                 where pricesTask.Result.Any(p => p.FuelId == f.FuelId)
                 let fe = new FuelEntity(f)
                 group fe by fe.PartitionKey into g
                 select g)
                .ToDictionary(g => g.Key, g => g.Take(1).ToList());
            var geographicRegionEntities =
                (from gr in geographicRegionsTask.Result.GeographicRegions
                 let ge = new GeographicRegionEntity(gr)
                 group ge by ge.PartitionKey into g
                 select g)
                .ToDictionary(g => g.Key, g => g.Select(e => e).ToList());
            var siteEntities =
                (from s in sitesTask.Result.Sites
                 where pricesTask.Result.Any(p => p.SiteId == s.SiteId)
                 let se = new SiteEntity(s)
                 group se by se.PartitionKey into g
                 select g)
                .ToDictionary(g => g.Key, g => g.Select(e => e).ToList());

            await Task.WhenAll(
                _brandRepository.CreateIfNotExistsAsync(ct),
                _fuelRepository.CreateIfNotExistsAsync(ct),
                _geographicRegionRepository.CreateIfNotExistsAsync(ct),
                _siteRepository.CreateIfNotExistsAsync(ct));

            await _brandRepository.SyncPartitionsWithDeactivateAsync(brandEntities, log, ct);
            await _fuelRepository.SyncPartitionsWithDeactivateAsync(fuelEntities, log, ct);
            await _geographicRegionRepository.SyncPartitionsWithDeactivateAsync(geographicRegionEntities, log, ct);
            await _siteRepository.SyncPartitionsWithDeactivateAsync(siteEntities, log, ct);

            sw.Stop();
            log.LogInformation($"Finished sync of entities in {sw.ElapsedMilliseconds}.");
            log.LogInformation($"Have a nice day 😋");
        }
    }
}
