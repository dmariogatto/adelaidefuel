using AdelaideFuel.Api;
using AdelaideFuel.Models;
using AdelaideFuel.Shared;
using AdelaideFuel.Storage;
using Polly;
using Polly.Retry;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.Essentials;
using Xamarin.Essentials.Interfaces;

namespace AdelaideFuel.Services
{
    public class FuelService : BaseService, IFuelService
    {
        private TimeSpan CacheExpireTimeSpan => TimeSpan.FromHours(12);

        private readonly IConnectivity _connectivity;
        private readonly IGeolocation _geolocation;

        private readonly IAdelaideFuelApi _fuelApi;
        private readonly IStoreFactory _storeFactory;

        private readonly IStore<UserBrand> _brandUserStore;
        private readonly IStore<UserFuel> _fuelUserStore;

        private readonly AsyncRetryPolicy _retryPolicy;

        private readonly SemaphoreSlim _userBrandsSemaphore = new SemaphoreSlim(1, 1);
        private readonly SemaphoreSlim _userFuelsSemaphore = new SemaphoreSlim(1, 1);

        public FuelService(
            IConnectivity connectivity,
            IGeolocation geolocation,
            IAdelaideFuelApi adelaideFuelApi,
            IStoreFactory storeFactory,
            ICacheService cacheService,
            IRetryPolicyFactory retryPolicyFactory,
            ILogger logger) : base(cacheService, logger)
        {
            _connectivity = connectivity;
            _geolocation = geolocation;

            _fuelApi = adelaideFuelApi;
            _storeFactory = storeFactory;

            _brandUserStore = _storeFactory.GetUserStore<UserBrand>();
            _fuelUserStore = _storeFactory.GetUserStore<UserFuel>();

            _retryPolicy =
                retryPolicyFactory.GetNetRetryPolicy()
                    .WaitAndRetryAsync
                    (
                        retryCount: 2,
                        sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt))
                    );
        }

        public async Task<IList<BrandDto>> GetBrandsAsync(CancellationToken cancellationToken)
        {
            IList<BrandDto> brands = await GetResponseAsync(CacheKey(), ct => _fuelApi.GetBrandsAsync(Constants.ApiKeyBrands, ct), cancellationToken).ConfigureAwait(false);
            return brands ?? Array.Empty<BrandDto>();
        }

        public async Task<IList<FuelDto>> GetFuelsAsync(CancellationToken cancellationToken)
        {
            IList<FuelDto> fuels = await GetResponseAsync(CacheKey(), ct => _fuelApi.GetFuelsAsync(Constants.ApiKeyFuels, ct), cancellationToken).ConfigureAwait(false);
            return fuels ?? Array.Empty<FuelDto>();
        }

        public async Task<IList<SiteDto>> GetSitesAsync(CancellationToken cancellationToken)
        {
            IList<SiteDto> sites = await GetResponseAsync(CacheKey(), ct => _fuelApi.GetSitesAsync(Constants.ApiKeySites, ct), cancellationToken).ConfigureAwait(false);
            return sites ?? Array.Empty<SiteDto>();
        }

        public async Task<IList<SiteDto>> GetSitesAsync(int brandId, CancellationToken cancellationToken)
        {
            if (brandId < 0) throw new ArgumentOutOfRangeException(nameof(brandId));

            IList<SiteDto> sites = await GetResponseAsync(CacheKey(brandId), ct => _fuelApi.GetSitesAsync(Constants.ApiKeySites, ct, brandId), cancellationToken).ConfigureAwait(false);
            return sites ?? Array.Empty<SiteDto>();
        }

        public Task<IList<SiteFuelPrice>> GetSitePricesAsync(CancellationToken cancellationToken)
            => GetPricesAsync(null, cancellationToken);

        public Task<IList<SiteFuelPrice>> GetSitePricesAsync(int siteId, CancellationToken cancellationToken)
        {
            if (siteId <= 0) throw new ArgumentOutOfRangeException(nameof(siteId));
            return GetPricesAsync(siteId, cancellationToken);
        }

        public async Task<IList<SiteFuelPriceItemGroup>> GetFuelPricesByRadiusAsync(int[] radiiKm, CancellationToken cancellationToken)
        {
            var locTask = _geolocation.GetLocationAsync(cancellationToken);
            var userFuels = GetUserFuelsAsync(cancellationToken);
            var pricesTask = GetSitePricesAsync(cancellationToken);

            await Task.WhenAll(locTask, userFuels, pricesTask).ConfigureAwait(false);

            var loc = locTask.Result;

            int getRadiusKm(double distanceKm)
            {
                if (distanceKm < 0) return int.MaxValue;

                foreach (var rkm in radiiKm)
                {
                    if (distanceKm <= rkm) return rkm;
                    if ((distanceKm - rkm) <= 0.05d) return rkm;
                }

                return int.MaxValue;
            }

            var fuelGroups =
                (from fp in pricesTask.Result
                 let distanceKm = loc?.CalculateDistance(fp.Latitude, fp.Longitude, DistanceUnits.Kilometers) ?? -1
                 let radiusKm = getRadiusKm(distanceKm)
                 let fuelPrice = (fp, distanceKm, radiusKm)
                 orderby fp.FuelSortOrder, radiusKm, fp.PriceInCents, distanceKm
                 group fuelPrice by fp.FuelId into fg
                 join f in userFuels.Result on fg.Key equals f.Id
                 let prices = fg
                        .GroupBy(i => i.radiusKm)
                        .Select(g => g.First())
                        .Select(i => new SiteFuelPriceItem(i.fp)
                        {
                            LastKnowDistanceKm = i.distanceKm,
                            RadiusKm = i.radiusKm
                        })
                 select new SiteFuelPriceItemGroup(f, prices)).ToList();

            foreach (var g in fuelGroups)
            {
                var cheapest = g.First();
                foreach (var fp in g.Skip(1).ToList())
                {
                    if (fp.PriceInCents >= cheapest.PriceInCents)
                        g.Remove(fp);
                    else
                        cheapest = fp;
                }
            }

            return fuelGroups;
        }

        public async Task<IList<UserBrand>> GetUserBrandsAsync(CancellationToken cancellationToken)
        {
            var userBrands = default(IList<UserBrand>);

            await _userBrandsSemaphore.WaitAsync().ConfigureAwait(false);

            try
            {
                var apiBrandsTask = GetBrandsAsync(cancellationToken);
                var userBrandsTask = _brandUserStore.AllAsync(true, cancellationToken);

                if (apiBrandsTask.Result?.Any() == true)
                {
                    await Task.WhenAll(apiBrandsTask, userBrandsTask).ConfigureAwait(false);
                    userBrands = await SyncSortableEntitiesWithApiAsync(apiBrandsTask.Result, userBrandsTask.Result, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    userBrands = userBrandsTask.Result
                        ?.OrderBy(i => i.SortOrder)
                        ?.ToList();
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
            finally
            {
                _userBrandsSemaphore.Release();
            }

            return userBrands ?? Array.Empty<UserBrand>();
        }

        public Task UpdateUserBrandsAsync(IList<UserBrand> brands, CancellationToken cancellationToken)
            => UpsertUserEntitiesAsync(brands, cancellationToken);

        public async Task<IList<UserFuel>> GetUserFuelsAsync(CancellationToken cancellationToken)
        {
            var userFuels = default(IList<UserFuel>);

            await _userFuelsSemaphore.WaitAsync().ConfigureAwait(false);

            try
            {
                var apiFuelsTask = GetFuelsAsync(cancellationToken);
                var userFuelsTask = _fuelUserStore.AllAsync(true, cancellationToken);

                await Task.WhenAll(apiFuelsTask, userFuelsTask).ConfigureAwait(false);

                if (apiFuelsTask.Result?.Any() == true)
                {
                    userFuels = await SyncSortableEntitiesWithApiAsync(apiFuelsTask.Result, userFuelsTask.Result, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    userFuels = userFuelsTask.Result
                        ?.OrderBy(i => i.SortOrder)
                        ?.ToList();
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
            finally
            {
                _userFuelsSemaphore.Release();
            }

            return userFuels ?? Array.Empty<UserFuel>();
        }

        public Task UpdateUserFuelsAsync(IList<UserFuel> fuels, CancellationToken cancellationToken)
            => UpsertUserEntitiesAsync(fuels, cancellationToken);

        private async Task<IList<SiteFuelPrice>> GetPricesAsync(long? siteId, CancellationToken cancellationToken)
        {
            var userBrandsTask = GetUserBrandsAsync(cancellationToken);
            var userFuelsTask = GetUserFuelsAsync(cancellationToken);

            await Task.WhenAll(userBrandsTask, userFuelsTask).ConfigureAwait(false);

            var brandIds = userBrandsTask.Result.Where(i => i.IsActive).Select(i => i.Id).ToList();
            var fuelIds = userFuelsTask.Result.Where(i => i.IsActive).Select(i => i.Id).ToList();

            var keys = new List<string>();
            if (siteId > 0)
                keys.Add($"sId={siteId}");
            if (brandIds.Count > 0)
                keys.Add($"bIds={string.Join(",", brandIds)}");
            if (fuelIds.Count > 0)
                keys.Add($"fIds={string.Join(",", fuelIds)}");

            var cacheKey = CacheKey(string.Join("&", keys));
            var sitePrices = default(IList<SiteFuelPrice>);

            if (!MemoryCache.TryGetValue(cacheKey, out sitePrices))
            {
                var diskCache = _storeFactory.GetCacheStore<IList<SiteFuelPrice>>();

                if (_connectivity.NetworkAccess != NetworkAccess.Internet)
                {
                    sitePrices = await diskCache.GetAsync(cacheKey, true, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    try
                    {
                        var sitesTask = GetSitesAsync(cancellationToken);
                        var pricesTask = _retryPolicy.ExecuteAsync(
                            (ct) => _fuelApi.GetSitePricesAsync(Constants.ApiKeySitePrices, brandIds, fuelIds, ct, siteId),
                            cancellationToken);

                        await Task.WhenAll(sitesTask, pricesTask).ConfigureAwait(false);

                        var fuelPriceGroups =
                            (from sp in pricesTask.Result
                             join s in sitesTask.Result on sp.SiteId equals s.SiteId
                             join b in userBrandsTask.Result on s.BrandId equals b.Id
                             join f in userFuelsTask.Result on sp.FuelId equals f.Id
                             where b.IsActive && f.IsActive
                             orderby f.SortOrder, sp.Price, b.SortOrder
                             let sfp = new SiteFuelPrice(b, f, s, sp)
                             group sfp by sfp.FuelId into fpg
                             select fpg).ToList();

                        foreach (var fpg in fuelPriceGroups)
                        {
                            var fns = Statistics.FiveNumberSummary(fpg.Select(i => i.PriceInCents).ToArray());
                            var median = fns[2];

                            var outliers = fpg
                                .Where(i => median - i.PriceInCents >= 100)
                                .ToList();

                            foreach (var o in outliers)
                                o.PriceInCents *= 10;
                        }

                        sitePrices = fuelPriceGroups
                            .SelectMany(fpg => fpg.Select(i => i))
                            .ToList();
                    }
                    catch (Exception ex)
                    {
                        Logger.Error(ex);
                    }

                    if (sitePrices?.Any() == true)
                    {
                        MemoryCache.SetAbsolute(cacheKey, sitePrices, TimeSpan.FromMinutes(2.5));
                        _ = diskCache.UpsertAsync(cacheKey, sitePrices, TimeSpan.FromDays(2), default);
                    }
                }
            }

            return sitePrices ?? Array.Empty<SiteFuelPrice>();
        }

        private async Task<IList<T>> SyncSortableEntitiesWithApiAsync<T, V>(IList<V> apiEntities, IList<T> userEntities, CancellationToken cancellationToken)
            where V : class, IFuelLookup
            where T : class, IUserSortableEntity, new()
        {
            var updatedEntities =
                (from ae in apiEntities
                 join ue in userEntities on ae.Id equals ue.Id into j
                 from mue in j.DefaultIfEmpty()
                 where mue == null || mue.Name != ae.Name
                 select new T()
                 {
                     Id = ae.Id,
                     Name = ae.Name,
                     SortOrder = int.MaxValue,
                     IsActive = true,
                 }).ToList();

            var removedEntities =
                (from ue in userEntities
                 join ae in apiEntities on ue.Id equals ae.Id into j
                 from mae in j.DefaultIfEmpty()
                 where mae == null
                 select ue).ToList();

            await Task.WhenAll(
                UpsertUserEntitiesAsync(updatedEntities, cancellationToken),
                RemoveUserEntitiesAsync(removedEntities, cancellationToken)).ConfigureAwait(false);

            var unmodified = userEntities
                .Except(removedEntities)
                .Where(e => updatedEntities.All(ue => ue.Id != e.Id));

            var result = updatedEntities
                .Concat(unmodified)
                .OrderBy(e => e.SortOrder).ToList();

            return result;
        }

        private Task UpsertUserEntitiesAsync<T>(IList<T> entities, CancellationToken cancellationToken) where T : class, IUserEntity
            => entities?.Any() == true
               ? _storeFactory
                    .GetUserStore<T>()
                    .UpsertRangeAsync(entities.Select(e => (e.Id.ToString(CultureInfo.InvariantCulture), e)).ToList(), TimeSpan.MaxValue, cancellationToken)
               : Task.CompletedTask;

        private Task RemoveUserEntitiesAsync<T>(IList<T> entities, CancellationToken cancellationToken) where T : class, IUserEntity
            => entities?.Any() == true
               ? _storeFactory
                    .GetUserStore<T>()
                    .RemoveRangeAsync(entities.Select(e => e.Id.ToString(CultureInfo.InvariantCulture)).ToList(), cancellationToken)
               : Task.CompletedTask;

        private async Task<TResponse> GetResponseAsync<TResponse>(string cacheKey, Func<CancellationToken, Task<TResponse>> apiRequest, CancellationToken cancellationToken, TimeSpan? cacheTimeSpan = null)
            where TResponse : class
        {
            if (string.IsNullOrEmpty(cacheKey)) throw new ArgumentException("Cache key cannot be empty!");

            // Cache LVL1
            if (!MemoryCache.TryGetValue(cacheKey, out TResponse response))
            {
                var diskCache = _storeFactory.GetCacheStore<TResponse>();

                if (_connectivity.NetworkAccess != NetworkAccess.Internet ||
                    !await diskCache.IsExpiredAsync(cacheKey, cancellationToken).ConfigureAwait(false))
                {
                    response = await diskCache.GetAsync(cacheKey, true, cancellationToken).ConfigureAwait(false);
                }

                if (response == default &&
                    _connectivity.NetworkAccess == NetworkAccess.Internet)
                {
                    try
                    {
                        response = await _retryPolicy.ExecuteAsync(apiRequest, cancellationToken).ConfigureAwait(false);
                        if (!cancellationToken.IsCancellationRequested && response != default)
                            await diskCache.UpsertAsync(cacheKey, response, cacheTimeSpan ?? CacheExpireTimeSpan, cancellationToken).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        Logger.Error(ex);
                    }
                }

                if (response != default)
                    MemoryCache.SetSliding(cacheKey, response, TimeSpan.FromMinutes(5));
            }

            return response;
        }
    }
}