﻿using AdelaideFuel.Api;
using AdelaideFuel.Models;
using AdelaideFuel.Shared;
using AdelaideFuel.Storage;
using Polly;
using Polly.Retry;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.Essentials;
using Xamarin.Essentials.Interfaces;

namespace AdelaideFuel.Services
{
    public class FuelService : BaseHttpService, IFuelService
    {
        private TimeSpan CacheExpireTimeSpan => TimeSpan.FromHours(12);

        private readonly int[] _defaultRadii = new[] { 1, 3, 5, 10, 25, 50, int.MaxValue };

        private readonly IConnectivity _connectivity;
        private readonly IGeolocation _geolocation;

        private readonly IAdelaideFuelApi _fuelApi;
        private readonly IStoreFactory _storeFactory;

        private readonly IStore<UserBrand> _brandUserStore;
        private readonly IStore<UserFuel> _fuelUserStore;
        private readonly IStore<UserRadius> _radiusUserStore;

        private readonly AsyncRetryPolicy _retryPolicy;

        private readonly SemaphoreSlim _syncBrandsSemaphore = new SemaphoreSlim(1, 1);
        private readonly SemaphoreSlim _syncFuelsSemaphore = new SemaphoreSlim(1, 1);
        private readonly SemaphoreSlim _syncRadiiSemaphore = new SemaphoreSlim(1, 1);

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
            _radiusUserStore = _storeFactory.GetUserStore<UserRadius>();

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

        public async Task<IList<SiteFuelPrice>> GetSitePricesAsync(CancellationToken cancellationToken)
        {
            var userBrandsTask = GetUserBrandsAsync(cancellationToken);
            var userFuelsTask = GetUserFuelsAsync(cancellationToken);

            await Task.WhenAll(userBrandsTask, userFuelsTask).ConfigureAwait(false);

            var brandIds = userBrandsTask.Result.Where(i => i.IsActive).Select(i => i.Id).ToList();
            var fuelIds = userFuelsTask.Result.Where(i => i.IsActive).Select(i => i.Id).ToList();

            var keys = new List<string>();

            if (brandIds.Count > 0)
                keys.Add($"brandIds={string.Join(",", brandIds)}");
            if (fuelIds.Count > 0)
                keys.Add($"fuelIds={string.Join(",", fuelIds)}");

            var queryString = string.Join("&", keys);

            var cacheKey = CacheKey(queryString, nameof(GetSitePricesAsync));
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
                            (ct) => GetSitePriceDtosAsync(queryString, ct),
                            cancellationToken);

                        await Task.WhenAll(sitesTask, pricesTask).ConfigureAwait(false);

                        if (!cancellationToken.IsCancellationRequested)
                        {
                            sitePrices =
                                (from sp in pricesTask.Result
                                 join s in sitesTask.Result on sp.SiteId equals s.SiteId
                                 join b in userBrandsTask.Result on s.BrandId equals b.Id
                                 join f in userFuelsTask.Result on sp.FuelId equals f.Id
                                 where b.IsActive && f.IsActive
                                 orderby f.SortOrder, sp.Price, b.SortOrder
                                 let sfp = new SiteFuelPrice(b, f, s, sp)
                                 select sfp).ToList();
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Error(ex);
                    }

                    if (sitePrices?.Any() == true)
                    {
                        MemoryCache.SetAbsolute(cacheKey, sitePrices, TimeSpan.FromMinutes(3));
                        _ = diskCache.UpsertAsync(cacheKey, sitePrices, TimeSpan.FromDays(2), default);
                    }
                }
            }

            return sitePrices ?? Array.Empty<SiteFuelPrice>();
        }

        public async Task<IList<SiteFuelPriceItemGroup>> GetFuelPricesByRadiusAsync(CancellationToken cancellationToken)
        {
            var locTask = _geolocation.GetLocationAsync(cancellationToken);
            var userFuelsTask = GetUserFuelsAsync(cancellationToken);
            var userRadiiTask = GetUserRadiiAsync(cancellationToken);
            var pricesTask = GetSitePricesAsync(cancellationToken);

            await Task.WhenAll(locTask, userFuelsTask, userRadiiTask, pricesTask).ConfigureAwait(false);

            var userFuels = userFuelsTask.Result.Where(i => i.IsActive).ToList();
            var userRadii = userRadiiTask.Result.Where(i => i.IsActive).ToList();

            var loc = locTask.Result;

            static int getRadiusKm(double distanceKm, List<UserRadius> radii)
            {
                if (distanceKm < 0) return int.MaxValue;

                foreach (var rkm in radii)
                {
                    var km = rkm.Id;

                    if (distanceKm <= km) return km;
                    if ((distanceKm - km) <= 0.05d) return km;
                }

                return int.MaxValue;
            }

            var fuelGroups =
                (from fp in pricesTask.Result
                 let distanceKm = loc?.CalculateDistance(fp.Latitude, fp.Longitude, DistanceUnits.Kilometers) ?? -1
                 let radiusKm = getRadiusKm(distanceKm, userRadii)
                 let fuelPrice = (fp, distanceKm, radiusKm)
                 orderby fp.FuelSortOrder, radiusKm, fp.PriceInCents, distanceKm
                 group fuelPrice by fp.FuelId into fg
                 join f in userFuels on fg.Key equals f.Id
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

        public async Task SyncBrandsAsync(CancellationToken cancellationToken)
        {
            await _syncBrandsSemaphore.WaitAsync().ConfigureAwait(false);

            try
            {
                var apiBrandsTask = GetBrandsAsync(cancellationToken);
                var userBrandsTask = _brandUserStore.AllAsync(true, cancellationToken);

                await Task.WhenAll(apiBrandsTask, userBrandsTask).ConfigureAwait(false);
                await SyncSortableEntitiesWithApiAsync(apiBrandsTask.Result, userBrandsTask.Result, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
            finally
            {
                _syncBrandsSemaphore.Release();
            }
        }

        public async Task SyncFuelsAsync(CancellationToken cancellationToken)
        {
            await _syncFuelsSemaphore.WaitAsync().ConfigureAwait(false);

            try
            {
                var apiFuelsTask = GetFuelsAsync(cancellationToken);
                var userFuelsTask = _fuelUserStore.AllAsync(true, cancellationToken);

                await Task.WhenAll(apiFuelsTask, userFuelsTask).ConfigureAwait(false);
                await SyncSortableEntitiesWithApiAsync(apiFuelsTask.Result, userFuelsTask.Result, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
            finally
            {
                _syncFuelsSemaphore.Release();
            }
        }

        public async Task SyncRadiiAsync(CancellationToken cancellationToken)
        {
            await _syncRadiiSemaphore.WaitAsync().ConfigureAwait(false);

            try
            {
                var radii = await _radiusUserStore.AllAsync(true, cancellationToken).ConfigureAwait(false);
                if (radii == null || !radii.Any())
                {
                    radii = _defaultRadii.Select(i => new UserRadius()
                    {
                        Id = i,
                        IsActive = true
                    }).ToList();

                    await UpsertUserRadiiAsync(radii, cancellationToken).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
            finally
            {
                _syncRadiiSemaphore.Release();
            }
        }

        public Task SyncAllAsync(CancellationToken cancellationToken)
            => Task.WhenAll(
                SyncBrandsAsync(cancellationToken),
                SyncFuelsAsync(cancellationToken),
                SyncRadiiAsync(cancellationToken));

        public async Task<IList<UserBrand>> GetUserBrandsAsync(CancellationToken cancellationToken)
        {
            var result = default(IList<UserBrand>);

            await _syncBrandsSemaphore.WaitAsync().ConfigureAwait(false);

            try
            {
                var brands = await _brandUserStore.AllAsync(true, cancellationToken).ConfigureAwait(false);
                result = brands
                    ?.OrderBy(i => i.SortOrder)
                    ?.ToList();
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
            finally
            {
                _syncBrandsSemaphore.Release();
            }

            return result ?? Array.Empty<UserBrand>();
        }

        public Task UpsertUserBrandsAsync(IList<UserBrand> brands, CancellationToken cancellationToken)
            => UpsertUserEntitiesAsync(brands, cancellationToken);

        public async Task<IList<UserFuel>> GetUserFuelsAsync(CancellationToken cancellationToken)
        {
            var result = default(IList<UserFuel>);

            await _syncFuelsSemaphore.WaitAsync().ConfigureAwait(false);

            try
            {
                var fuels = await _fuelUserStore.AllAsync(true, cancellationToken).ConfigureAwait(false);
                result = fuels
                    ?.OrderBy(i => i.SortOrder)
                    ?.ToList();
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
            finally
            {
                _syncFuelsSemaphore.Release();
            }

            return result ?? Array.Empty<UserFuel>();
        }

        public Task UpsertUserFuelsAsync(IList<UserFuel> fuels, CancellationToken cancellationToken)
            => UpsertUserEntitiesAsync(fuels, cancellationToken);

        public async Task<IList<UserRadius>> GetUserRadiiAsync(CancellationToken cancellationToken)
        {
            var result = default(IList<UserRadius>);

            await _syncRadiiSemaphore.WaitAsync().ConfigureAwait(false);

            try
            {
                var radii = await _radiusUserStore.AllAsync(true, cancellationToken).ConfigureAwait(false);
                result = radii
                    ?.OrderBy(i => i.SortOrder)
                    ?.ToList();
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
            finally
            {
                _syncRadiiSemaphore.Release();
            }

            return result ?? Array.Empty<UserRadius>();
        }

        public Task UpsertUserRadiiAsync(IList<UserRadius> radii, CancellationToken cancellationToken)
            => UpsertUserEntitiesAsync(radii, cancellationToken);

        public Task RemoveUserRadiiAsync(IList<UserRadius> radii, CancellationToken cancellationToken)
            => RemoveUserEntitiesAsync(radii, cancellationToken);

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
                     SortOrder = mue?.SortOrder ?? (userEntities.Count + apiEntities.IndexOf(ae)),
                     IsActive = mue?.IsActive ?? true,
                 }).ToList();

            var removedEntities = userEntities
                .Where(e => apiEntities.All(ae => ae.Id != e.Id))
                .ToList();

            var unmodified = userEntities
                .Except(removedEntities)
                .Where(e => !updatedEntities.Any(ue => ue.Id == e.Id))
                .ToList();

            var result = updatedEntities
                .Concat(unmodified)
                .OrderBy(e => e.SortOrder)
                .ToList();

            if (updatedEntities.Any() || removedEntities.Any())
            {
                // fix up sort order
                for (var i = 0; i < result.Count; i++)
                {
                    var e = result[i];
                    if (e.SortOrder != i)
                    {
                        // make sure we update the order
                        if (unmodified.Contains(e))
                            updatedEntities.Add(e);
                        result[i].SortOrder = i;
                    }
                }
            }

            await Task.WhenAll(
                UpsertUserEntitiesAsync(updatedEntities, cancellationToken),
                RemoveUserEntitiesAsync(removedEntities, cancellationToken)).ConfigureAwait(false);

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
                    MemoryCache.SetSliding(cacheKey, response, TimeSpan.FromMinutes(30));
            }

            return response;
        }

        private async Task<IList<SitePriceDto>> GetSitePriceDtosAsync(string queryString, CancellationToken cancellationToken)
        {
            const string sitePrices = "SitePrices";

            var uri = Path.Combine(Constants.ApiUrlBase, !string.IsNullOrEmpty(queryString) ? $"{sitePrices}?{queryString}" : sitePrices);
            var request = new HttpRequestMessage(HttpMethod.Get, uri);
            request.Headers.Add(Constants.AuthHeader, Constants.ApiKeySitePrices);
            using var response = await HttpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            using var s = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
            var result = DeserializeJsonFromStream<List<SitePriceDto>>(s);

            return result;
        }
    }
}