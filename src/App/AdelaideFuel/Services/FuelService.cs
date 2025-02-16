using AdelaideFuel.Api;
using AdelaideFuel.Essentials;
using AdelaideFuel.Models;
using AdelaideFuel.Shared;
using AdelaideFuel.Storage;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Devices.Sensors;
using Microsoft.Maui.Networking;
using Polly;
using Polly.Retry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AdelaideFuel.Services
{
    public class FuelService : BaseService, IFuelService
    {
        private readonly static TimeSpan CacheExpireTimeSpan = TimeSpan.FromHours(12);
        private readonly static TimeSpan CachePricesExpireTimeSpan = TimeSpan.FromMinutes(60);

        private readonly IReadOnlyList<int> _defaultRadii = [1, 3, 5, 10, 25, 50, int.MaxValue];

        private readonly IAppClock _clock;

        private readonly IConnectivity _connectivity;
        private readonly IGeolocation _geolocation;
        private readonly IPermissions _permissions;

        private readonly IAppPreferences _appPrefs;

        private readonly IAdelaideFuelApi _fuelApi;
        private readonly IStoreFactory _storeFactory;

        private readonly IBrandService _brandService;

        private readonly IUserStore<UserBrand> _brandUserStore;
        private readonly IUserStore<UserFuel> _fuelUserStore;
        private readonly IUserStore<UserRadius> _radiusUserStore;

        private readonly AsyncRetryPolicy _retryPolicy;

        private readonly SemaphoreSlim _syncBrandsSemaphore = new SemaphoreSlim(1, 1);
        private readonly SemaphoreSlim _syncFuelsSemaphore = new SemaphoreSlim(1, 1);
        private readonly SemaphoreSlim _syncRadiiSemaphore = new SemaphoreSlim(1, 1);
        private readonly SemaphoreSlim _syncAllSemaphore = new SemaphoreSlim(1, 1);

        public FuelService(
            IAppClock clock,
            IConnectivity connectivity,
            IGeolocation geolocation,
            IPermissions permissions,
            IAppPreferences appPrefs,
            IAdelaideFuelApi adelaideFuelApi,
            IBrandService brandService,
            IStoreFactory storeFactory,
            ICacheService cacheService,
            IRetryPolicyFactory retryPolicyFactory,
            ILogger logger) : base(cacheService, logger)
        {
            _clock = clock;

            _connectivity = connectivity;
            _geolocation = geolocation;
            _permissions = permissions;

            _appPrefs = appPrefs;

            _fuelApi = adelaideFuelApi;
            _storeFactory = storeFactory;

            _brandService = brandService;

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

        public async Task<IReadOnlyList<BrandDto>> GetBrandsAsync(CancellationToken cancellationToken)
        {
            var brands = await GetResponseAsync(CacheKey(), ct => _fuelApi.GetBrandsAsync(Constants.ApiKeyBrands, ct), cancellationToken).ConfigureAwait(false);
            return brands ?? [];
        }

        public async Task<IReadOnlyList<FuelDto>> GetFuelsAsync(CancellationToken cancellationToken)
        {
            var fuels = await GetResponseAsync(CacheKey(), ct => _fuelApi.GetFuelsAsync(Constants.ApiKeyFuels, ct), cancellationToken).ConfigureAwait(false);
            return fuels ?? [];
        }

        public async Task<IReadOnlyList<SiteDto>> GetSitesAsync(CancellationToken cancellationToken)
        {
            var sites = await GetResponseAsync(CacheKey(), ct => _fuelApi.GetSitesAsync(Constants.ApiKeySites, ct), cancellationToken).ConfigureAwait(false);
            return sites ?? [];
        }

        public async Task<IReadOnlyList<SiteDto>> GetSitesAsync(int brandId, CancellationToken cancellationToken)
        {
            if (brandId < 0) throw new ArgumentOutOfRangeException(nameof(brandId));

            var sites = await GetResponseAsync(CacheKey(brandId), ct => _fuelApi.GetSitesAsync(Constants.ApiKeySites, ct, brandId), cancellationToken).ConfigureAwait(false);
            return sites ?? [];
        }

        public async Task<(IReadOnlyList<SiteFuelPrice> prices, DateTime modifiedUtc)> GetSitePricesAsync(CancellationToken cancellationToken)
        {
            var userBrandsTask = GetUserBrandsAsync(cancellationToken);
            var userFuelsTask = GetUserFuelsAsync(cancellationToken);

            await Task.WhenAll(userBrandsTask, userFuelsTask).ConfigureAwait(false);

            var brandIds = userBrandsTask.Result.Where(i => i.IsActive).Select(i => i.Id).ToList();
            var fuelIds = userFuelsTask.Result.Where(i => i.IsActive).Select(i => i.Id).ToList();

            var cacheKey = CacheKey(string.Join(',', brandIds.Concat(new[] { -1 }).Concat(fuelIds)), nameof(GetSitePricesAsync));
            var lastCheckCacheKey = CacheKey("last_check", nameof(GetSitePricesAsync));

            (IReadOnlyList<SiteFuelPrice> prices, DateTime modifiedUtc) result =
                (Array.Empty<SiteFuelPrice>(), DateTime.MinValue);

            async Task<bool> isOutOfDateAsync(string lastCheckCacheKey, DateTime modifiedUtc, CancellationToken ct)
            {
                if (MemoryCache.TryGetValue(lastCheckCacheKey, out DateTime lastCheck) &&
                    (_clock.UtcNow - lastCheck) < TimeSpan.FromMinutes(3))
                    return false;

                var newModifiedUtc = DateTime.MaxValue;

                try
                {
                    newModifiedUtc = _connectivity.NetworkAccess == NetworkAccess.Internet
                        ? await _retryPolicy.ExecuteAsync(
                              (ct) => _fuelApi.GetSitePricesModifiedUtcAsync(Constants.ApiKeySitePrices, ct),
                              ct).ConfigureAwait(false)
                        : DateTime.MinValue;
                }
                catch (Exception ex)
                {
                    Logger.Error(ex);
                }

                var hasChanged = newModifiedUtc > modifiedUtc;

                if (!hasChanged)
                    MemoryCache.SetAbsolute(lastCheckCacheKey, _clock.UtcNow, CachePricesExpireTimeSpan);

                return hasChanged;
            }

            if (!MemoryCache.TryGetValue(cacheKey, out result) ||
                await isOutOfDateAsync(lastCheckCacheKey, result.modifiedUtc, cancellationToken).ConfigureAwait(false))
            {
                var diskCache = _storeFactory.GetCacheStore<IReadOnlyList<SiteFuelPrice>>();

                if (_connectivity.NetworkAccess != NetworkAccess.Internet)
                {
                    result.prices = diskCache.Get(cacheKey, true) ?? [];
                    if (result.prices.Any())
                        result.modifiedUtc = result.prices.Max(i => i.ModifiedUtc);
                }
                else
                {
                    var sites = default(IReadOnlyList<SiteDto>);
                    var prices = default(IReadOnlyList<SitePriceDto>);
                    var newModifiedUtc = DateTime.MinValue;

                    try
                    {
                        var sitesTask = GetSitesAsync(cancellationToken);
                        var pricesTask = _retryPolicy.ExecuteAsync(
                            (ct) => _fuelApi.GetSitePricesAsync(Constants.ApiKeySitePrices, brandIds, fuelIds, ct),
                            cancellationToken);

                        await Task.WhenAll(sitesTask, pricesTask).ConfigureAwait(false);

                        sites = sitesTask.Result;
                        (prices, newModifiedUtc) = pricesTask.Result;
                    }
                    catch (Exception ex)
                    {
                        Logger.Error(ex);
                    }

                    if (!cancellationToken.IsCancellationRequested && sites?.Any() == true && prices?.Any() == true)
                    {
                        result.prices =
                            (from sp in prices
                             join s in sites on sp.SiteId equals s.SiteId
                             join b in userBrandsTask.Result on s.BrandId equals b.Id
                             join f in userFuelsTask.Result on sp.FuelId equals f.Id
                             where b.IsActive && f.IsActive
                             orderby f.SortOrder, sp.Price, b.SortOrder, s.Name
                             select new SiteFuelPrice(b, f, s, sp)).ToList();
                        result.modifiedUtc = newModifiedUtc;

                        if (result.prices.Any())
                        {
                            MemoryCache.SetAbsolute(cacheKey, result, CachePricesExpireTimeSpan);
                            MemoryCache.SetAbsolute(lastCheckCacheKey, _clock.UtcNow, CachePricesExpireTimeSpan);

                            diskCache.Upsert(cacheKey, result.prices, TimeSpan.FromDays(3));
                        }
                    }
                }
            }

            result.prices ??= [];

            return result;
        }

        public async Task<(IReadOnlyList<SiteFuelPriceItemGroup> groups, Location location, DateTime modifiedUtc)> GetFuelPricesByRadiusAsync(CancellationToken cancellationToken)
        {
            async Task<Location> getLocationAsync(CancellationToken ct)
            {
                var location = default(Location);

                try
                {
                    var status = await _permissions.CheckStatusAsync<Permissions.LocationWhenInUse>().ConfigureAwait(false);
                    if (status == PermissionStatus.Granted)
                    {
                        location ??= await _geolocation.GetLastKnownLocationAsync().ConfigureAwait(false);
                        location = await _geolocation.GetLocationAsync(
                            new GeolocationRequest(GeolocationAccuracy.Medium, TimeSpan.FromSeconds(6.5)), ct).ConfigureAwait(false);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(ex);
                }

                return location;
            }

            var locTask = getLocationAsync(cancellationToken);
            var userFuelsTask = GetUserFuelsAsync(cancellationToken);
            var userRadiiTask = GetUserRadiiAsync(cancellationToken);
            var pricesTask = GetSitePricesAsync(cancellationToken);

            var (prices, modifiedUtc) = await pricesTask.ConfigureAwait(false);

            var fuelGroups = new List<SiteFuelPriceItemGroup>();
            var gpsLocation = default(Location);

            if (prices?.Any() == true)
            {
                await Task.WhenAll(locTask, userFuelsTask, userRadiiTask).ConfigureAwait(false);

                var userFuels = userFuelsTask.Result.Where(i => i.IsActive).ToDictionary(i => i.Id, i => i);
                var userRadii = userRadiiTask.Result.Where(i => i.IsActive).ToList();
                gpsLocation = locTask.Result;

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

                var distanceLocation = gpsLocation ?? Constants.AdelaideCenter.ToLocation();
                var fuelPriceData = new SortedSet<SiteFuelPriceAndDistance>(new SiteFuelPriceAndDistanceComparer());
                foreach (var fp in prices.Where(i => i.PriceInCents != Constants.OutOfStockPriceInCents))
                {
                    var distanceKm = distanceLocation.CalculateDistance(fp.Latitude, fp.Longitude, DistanceUnits.Kilometers);
                    var radiusKm = getRadiusKm(distanceKm, userRadii);
                    fuelPriceData.Add(new SiteFuelPriceAndDistance(fp, distanceKm, radiusKm));
                }

                var currentGroup = default(SiteFuelPriceItemGroup);
                var currentRadius = -1;
                var currentCheapest = double.MaxValue;

                foreach (var fpd in fuelPriceData)
                {
                    var currentFuelId = fpd.Price.FuelId;

                    if (currentGroup?.Key?.Id != currentFuelId && userFuels.ContainsKey(currentFuelId))
                    {
                        var fuel = userFuels[currentFuelId];

                        currentGroup = new SiteFuelPriceItemGroup(fuel, Array.Empty<SiteFuelPriceItem>());
                        currentRadius = -1;
                        currentCheapest = double.MaxValue;

                        fuelGroups.Add(currentGroup);
                    }

                    if (currentGroup?.Key?.Id == currentFuelId &&
                        currentRadius != fpd.RadiusKm &&
                        fpd.Price.PriceInCents < currentCheapest)
                    {
                        currentRadius = fpd.RadiusKm;
                        currentCheapest = Math.Min(currentCheapest, fpd.Price.PriceInCents);

                        currentGroup.Add(new SiteFuelPriceItem(fpd.Price)
                        {
                            LastKnowDistanceKm = fpd.DistanceKm,
                            RadiusKm = fpd.RadiusKm
                        });
                    }
                }
            }

            return (fuelGroups, gpsLocation, modifiedUtc);
        }

        public async Task<bool> SyncBrandsAsync(CancellationToken cancellationToken)
        {
            var success = false;

            await _syncBrandsSemaphore.WaitAsync().ConfigureAwait(false);

            try
            {
                var apiBrands = await GetBrandsAsync(cancellationToken).ConfigureAwait(false);
                var userBrands = _brandUserStore.All();

                if (apiBrands?.Any() == true)
                {
                    SyncSortableEntitiesWithApi(apiBrands, userBrands);
                    success = true;

                    _ = _brandService.GetBrandImagePathsAsync(apiBrands.Select(i => i.Id).ToList(), false, CancellationToken.None);
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
            finally
            {
                _syncBrandsSemaphore.Release();
            }

            return success;
        }

        public async Task<bool> SyncFuelsAsync(CancellationToken cancellationToken)
        {
            var success = false;

            await _syncFuelsSemaphore.WaitAsync().ConfigureAwait(false);

            try
            {
                var apiFuels = await GetFuelsAsync(cancellationToken).ConfigureAwait(false);
                var userFuels = _fuelUserStore.All();

                if (apiFuels?.Any() == true)
                {
                    SyncSortableEntitiesWithApi(apiFuels, userFuels);
                    success = true;
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
            finally
            {
                _syncFuelsSemaphore.Release();
            }

            return success;
        }

        public async Task<bool> SyncRadiiAsync(CancellationToken cancellationToken)
        {
            var success = false;

            await _syncRadiiSemaphore.WaitAsync().ConfigureAwait(false);

            try
            {
                var radii = _radiusUserStore.All();
                if (radii is null || !radii.Any())
                {
                    radii = _defaultRadii.Select(i => new UserRadius()
                    {
                        Id = i,
                        IsActive = true
                    }).ToList();

                    await UpsertUserRadiiAsync(radii, cancellationToken).ConfigureAwait(false);
                }

                success = true;
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
            finally
            {
                _syncRadiiSemaphore.Release();
            }

            return success;
        }

        public async Task<bool> SyncAllAsync(CancellationToken cancellationToken)
        {
            var success = false;

            await _syncAllSemaphore.WaitAsync().ConfigureAwait(false);

            try
            {
                var today = _clock.Today;

#if DEBUG
                today = DateTime.MaxValue;
#endif

                if (today > _appPrefs.LastDateSynced)
                {
                    var tasks = new[]
                    {
                        SyncBrandsAsync(cancellationToken),
                        SyncFuelsAsync(cancellationToken),
                        SyncRadiiAsync(cancellationToken)
                    };

                    await Task.WhenAll(tasks).ConfigureAwait(false);
                    success = tasks.All(t => t.Result);

                    if (success)
                        _appPrefs.LastDateSynced = today;
                }
                else
                {
                    success = true;
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
            finally
            {
                _syncAllSemaphore.Release();
            }

            return success;
        }

        public async Task<IReadOnlyList<UserBrand>> GetUserBrandsAsync(CancellationToken cancellationToken)
        {
            var result = default(IReadOnlyList<UserBrand>);

            await _syncBrandsSemaphore.WaitAsync().ConfigureAwait(false);

            try
            {
                var brands = _brandUserStore.All();
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

            return result ?? [];
        }

        public Task<int> UpsertUserBrandsAsync(IReadOnlyList<UserBrand> brands, CancellationToken cancellationToken)
            => Task.FromResult(UpsertUserEntities(brands));

        public async Task<IReadOnlyList<UserFuel>> GetUserFuelsAsync(CancellationToken cancellationToken)
        {
            var result = default(IReadOnlyList<UserFuel>);

            await _syncFuelsSemaphore.WaitAsync().ConfigureAwait(false);

            try
            {
                var fuels = _fuelUserStore.All();
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

            return result ?? [];
        }

        public Task<int> UpsertUserFuelsAsync(IReadOnlyList<UserFuel> fuels, CancellationToken cancellationToken)
            => Task.FromResult(UpsertUserEntities(fuels));

        public async Task<IReadOnlyList<UserRadius>> GetUserRadiiAsync(CancellationToken cancellationToken)
        {
            var result = default(IReadOnlyList<UserRadius>);

            await _syncRadiiSemaphore.WaitAsync().ConfigureAwait(false);

            try
            {
                var radii = _radiusUserStore.All();
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

            return result ?? [];
        }

        public Task<int> UpsertUserRadiiAsync(IReadOnlyList<UserRadius> radii, CancellationToken cancellationToken)
            => Task.FromResult(UpsertUserEntities(radii));

        public Task<int> RemoveUserRadiiAsync(IReadOnlyList<UserRadius> radii, CancellationToken cancellationToken)
            => Task.FromResult(RemoveUserEntities(radii));

        private IReadOnlyList<T> SyncSortableEntitiesWithApi<T, V>(IReadOnlyList<V> apiEntities, IReadOnlyList<T> userEntities)
            where V : class, IFuelLookup
            where T : class, IUserSortableEntity, new()
        {
            var userLookup = userEntities.ToDictionary(i => i.Id, i => i);
            var apiLookup = apiEntities.ToDictionary(i => i.Id, i => i);

            var toUpsert = new HashSet<T>();
            var toRemove = new HashSet<T>();
            var unmodified = new HashSet<T>();

            foreach (var i in apiEntities)
            {
                if (userLookup.TryGetValue(i.Id, out var existing) && i.Name != existing.Name)
                {
                    toUpsert.Add(new T()
                    {
                        Id = i.Id,
                        Name = i.Name,
                        SortOrder = existing.SortOrder,
                        IsActive = existing.IsActive
                    });
                }
                else if (existing is not null)
                {
                    unmodified.Add(existing);
                }
                else
                {
                    toUpsert.Add(new T()
                    {
                        Id = i.Id,
                        Name = i.Name,
                        SortOrder = userEntities.Count + toUpsert.Count,
                        IsActive = true
                    });
                }
            }

            foreach (var i in userEntities)
            {
                if (!apiLookup.ContainsKey(i.Id))
                    toRemove.Add(i);
            }

            var result = toUpsert
                .Concat(unmodified)
                .OrderBy(e => e.SortOrder)
                .ToList();

            if (toUpsert.Any() || toRemove.Any())
            {
                // fix up sort order
                for (var i = 0; i < result.Count; i++)
                {
                    var e = result[i];
                    if (e.SortOrder != i)
                    {
                        e.SortOrder = i;
                        toUpsert.Add(e);
                    }
                }
            }

            UpsertUserEntities(toUpsert);
            RemoveUserEntities(toRemove);

            return result;
        }

        private int UpsertUserEntities<T>(IEnumerable<T> entities) where T : class, IUserEntity
            => entities?.Any() == true
               ? _storeFactory.GetUserStore<T>().UpsertRange(entities)
               : 0;

        private int RemoveUserEntities<T>(IEnumerable<T> entities) where T : class, IUserEntity
            => entities?.Any() == true
               ? _storeFactory.GetUserStore<T>().RemoveRange(entities)
               : 0;

        private async Task<TResponse> GetResponseAsync<TResponse>(string cacheKey, Func<CancellationToken, Task<TResponse>> apiRequest, CancellationToken cancellationToken, TimeSpan? diskCacheTime = null)
            where TResponse : class
        {
            if (string.IsNullOrEmpty(cacheKey)) throw new ArgumentException("Cache key cannot be empty!");

            // Cache LVL1
            if (!MemoryCache.TryGetValue(cacheKey, out TResponse response))
            {
                var diskCache = _storeFactory.GetCacheStore<TResponse>();
                response = diskCache.Get(cacheKey, _connectivity.NetworkAccess != NetworkAccess.Internet);

                if (response is null && _connectivity.NetworkAccess == NetworkAccess.Internet)
                {
                    try
                    {
                        response = await _retryPolicy.ExecuteAsync(apiRequest, cancellationToken).ConfigureAwait(false);
                        if (!cancellationToken.IsCancellationRequested && response is not null)
                            diskCache.Upsert(cacheKey, response, diskCacheTime ?? CacheExpireTimeSpan);
                    }
                    catch (Exception ex)
                    {
                        Logger.Error(ex);
                    }
                }

                if (response is not null)
                    MemoryCache.SetSliding(cacheKey, response, TimeSpan.FromMinutes(30));
            }

            return response;
        }
    }
}