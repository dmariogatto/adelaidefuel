using AdelaideFuel.Api;
using AdelaideFuel.Models;
using AdelaideFuel.Shared;
using AdelaideFuel.Storage;
using Polly;
using Polly.Retry;
using Refit;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.Essentials;
using Xamarin.Essentials.Interfaces;

namespace AdelaideFuel.Services
{
    public class FuelService : BaseHttpService, IFuelService
    {
        private readonly static TimeSpan CacheExpireTimeSpan = TimeSpan.FromHours(12);
        private readonly static TimeSpan CachePricesExpireTimeSpan = TimeSpan.FromMinutes(60);

        private readonly int[] _defaultRadii = new[] { 1, 3, 5, 10, 25, 50, int.MaxValue };

        private readonly IConnectivity _connectivity;
        private readonly IGeolocation _geolocation;
        private readonly IPermissions _permissions;

        private readonly IAppPreferences _appPrefs;

        private readonly IAdelaideFuelApi _fuelApi;
        private readonly IStoreFactory _storeFactory;

        private readonly IStore<UserBrand> _brandUserStore;
        private readonly IStore<UserFuel> _fuelUserStore;
        private readonly IStore<UserRadius> _radiusUserStore;

        private readonly AsyncRetryPolicy _retryPolicy;

        private readonly SemaphoreSlim _syncBrandsSemaphore = new SemaphoreSlim(1, 1);
        private readonly SemaphoreSlim _syncFuelsSemaphore = new SemaphoreSlim(1, 1);
        private readonly SemaphoreSlim _syncRadiiSemaphore = new SemaphoreSlim(1, 1);
        private readonly SemaphoreSlim _syncAllSemaphore = new SemaphoreSlim(1, 1);

        public FuelService(
            IConnectivity connectivity,
            IGeolocation geolocation,
            IPermissions permissions,
            IAppPreferences appPrefs,
            IAdelaideFuelApi adelaideFuelApi,
            IStoreFactory storeFactory,
            ICacheService cacheService,
            IRetryPolicyFactory retryPolicyFactory,
            ILogger logger) : base(cacheService, logger)
        {
            _connectivity = connectivity;
            _geolocation = geolocation;
            _permissions = permissions;

            _appPrefs = appPrefs;

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

        public async Task<(IList<SiteFuelPrice> prices, DateTime modifiedUtc)> GetSitePricesAsync(CancellationToken cancellationToken)
        {
            var userBrandsTask = GetUserBrandsAsync(cancellationToken);
            var userFuelsTask = GetUserFuelsAsync(cancellationToken);

            await Task.WhenAll(userBrandsTask, userFuelsTask).ConfigureAwait(false);

            var brandIds = userBrandsTask.Result.Where(i => i.IsActive).Select(i => i.Id).ToList();
            var fuelIds = userFuelsTask.Result.Where(i => i.IsActive).Select(i => i.Id).ToList();

            var keyBuilder = new StringBuilder();

            if (brandIds.Count > 0)
            {
                keyBuilder.Append("brandIds=");
                keyBuilder.AppendJoin(",", brandIds);
            }

            if (fuelIds.Count > 0)
            {
                if (keyBuilder.Length > 0) keyBuilder.Append("&");
                keyBuilder.Append("fuelIds=");
                keyBuilder.AppendJoin(",", fuelIds);
            }

            var queryString = keyBuilder.ToString();
            var cacheKey = CacheKey(queryString, nameof(GetSitePricesAsync));
            var lastCheckCacheKey = CacheKey("last_check", nameof(GetSitePricesAsync));

            (IList<SiteFuelPrice> prices, DateTime modifiedUtc) result =
                (Array.Empty<SiteFuelPrice>(), DateTime.MinValue);

            async Task<bool> isOutOfDateAsync(string lastCheckCacheKey, DateTime modifiedUtc, CancellationToken ct)
            {
                if (MemoryCache.TryGetValue(lastCheckCacheKey, out DateTime lastCheck) &&
                    (DateTime.UtcNow - lastCheck) < TimeSpan.FromMinutes(3))
                    return false;

                var newModifiedUtc = DateTime.MaxValue;

                try
                {
                    (_, newModifiedUtc) = _connectivity.NetworkAccess == NetworkAccess.Internet
                        ? await _retryPolicy.ExecuteAsync(
                              (ct) => RequestSitePriceAsync(HttpMethod.Head, string.Empty, ct),
                              ct).ConfigureAwait(false)
                        : (null, DateTime.MinValue);
                }
                catch (Exception ex)
                {
                    Logger.Error(ex);
                }

                var hasChanged = newModifiedUtc > modifiedUtc;

                if (!hasChanged)
                    MemoryCache.SetAbsolute(lastCheckCacheKey, DateTime.UtcNow, CachePricesExpireTimeSpan);

                return hasChanged;
            }

            if (!MemoryCache.TryGetValue(cacheKey, out result) ||
                await isOutOfDateAsync(lastCheckCacheKey, result.modifiedUtc, cancellationToken).ConfigureAwait(false))
            {
                var diskCache = _storeFactory.GetCacheStore<IList<SiteFuelPrice>>();

                if (_connectivity.NetworkAccess != NetworkAccess.Internet)
                {
                    result.prices = await diskCache.GetAsync(cacheKey, true, cancellationToken).ConfigureAwait(false) ?? Array.Empty<SiteFuelPrice>();
                    if (result.prices.Any())
                        result.modifiedUtc = result.prices.Max(i => i.ModifiedUtc);
                }
                else
                {
                    var sites = default(IList<SiteDto>);
                    var prices = default(IList<SitePriceDto>);
                    var newModifiedUtc = DateTime.MinValue;

                    try
                    {
                        var sitesTask = GetSitesAsync(cancellationToken);
                        var pricesTask = _retryPolicy.ExecuteAsync(
                            (ct) => RequestSitePriceAsync(HttpMethod.Get, queryString, ct),
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
                            MemoryCache.SetAbsolute(lastCheckCacheKey, DateTime.UtcNow, CachePricesExpireTimeSpan);

                            _ = diskCache.UpsertAsync(cacheKey, result.prices, TimeSpan.FromDays(3), default);
                        }
                    }
                }
            }

            result.prices ??= Array.Empty<SiteFuelPrice>();

            return result;
        }

        public async Task<(IList<SiteFuelPriceItemGroup> groups, Location location, DateTime modifiedUtc)> GetFuelPricesByRadiusAsync(CancellationToken cancellationToken)
        {
            async Task<Location> getLocationAsync(CancellationToken ct)
            {
                var location = default(Location);

                try
                {
                    var status = await _permissions.CheckStatusAsync<Permissions.LocationWhenInUse>().ConfigureAwait(false);
                    if (status == PermissionStatus.Granted)
                    {
                        location = await _geolocation.GetLocationAsync(
                            new GeolocationRequest(GeolocationAccuracy.Medium, TimeSpan.FromSeconds(5)), ct).ConfigureAwait(false);
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
                var apiBrandsTask = GetBrandsAsync(cancellationToken);
                var userBrandsTask = _brandUserStore.AllAsync(true, cancellationToken);

                await Task.WhenAll(apiBrandsTask, userBrandsTask).ConfigureAwait(false);

                if (apiBrandsTask.Result?.Any() == true)
                {
                    await SyncSortableEntitiesWithApiAsync(apiBrandsTask.Result, userBrandsTask.Result, cancellationToken).ConfigureAwait(false);
                    success = true;
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
                var apiFuelsTask = GetFuelsAsync(cancellationToken);
                var userFuelsTask = _fuelUserStore.AllAsync(true, cancellationToken);

                await Task.WhenAll(apiFuelsTask, userFuelsTask).ConfigureAwait(false);

                if (apiFuelsTask.Result?.Any() == true)
                {
                    await SyncSortableEntitiesWithApiAsync(apiFuelsTask.Result, userFuelsTask.Result, cancellationToken).ConfigureAwait(false);
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
                var radii = await _radiusUserStore.AllAsync(true, cancellationToken).ConfigureAwait(false);
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
                var today = DateTime.Today;

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

        public Task<int> UpsertUserBrandsAsync(IList<UserBrand> brands, CancellationToken cancellationToken)
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

        public Task<int> UpsertUserFuelsAsync(IList<UserFuel> fuels, CancellationToken cancellationToken)
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

        public Task<int> UpsertUserRadiiAsync(IList<UserRadius> radii, CancellationToken cancellationToken)
            => UpsertUserEntitiesAsync(radii, cancellationToken);

        public Task<int> RemoveUserRadiiAsync(IList<UserRadius> radii, CancellationToken cancellationToken)
            => RemoveUserEntitiesAsync(radii, cancellationToken);

        private async Task<IList<T>> SyncSortableEntitiesWithApiAsync<T, V>(IList<V> apiEntities, IList<T> userEntities, CancellationToken cancellationToken)
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

            await Task.WhenAll(
                UpsertUserEntitiesAsync(toUpsert, cancellationToken),
                RemoveUserEntitiesAsync(toRemove, cancellationToken)).ConfigureAwait(false);

            return result;
        }

        private Task<int> UpsertUserEntitiesAsync<T>(ICollection<T> entities, CancellationToken cancellationToken) where T : class, IUserEntity
            => entities?.Any() == true
               ? _storeFactory
                    .GetUserStore<T>()
                    .UpsertRangeAsync(entities.Select(e => (e.Id.ToString(CultureInfo.InvariantCulture), e)).ToList(), TimeSpan.MaxValue, cancellationToken)
               : Task.FromResult(0);

        private Task<int> RemoveUserEntitiesAsync<T>(ICollection<T> entities, CancellationToken cancellationToken) where T : class, IUserEntity
            => entities?.Any() == true
               ? _storeFactory
                    .GetUserStore<T>()
                    .RemoveRangeAsync(entities.Select(e => e.Id.ToString(CultureInfo.InvariantCulture)).ToList(), cancellationToken)
               : Task.FromResult(0);

        private async Task<TResponse> GetResponseAsync<TResponse>(string cacheKey, Func<CancellationToken, Task<TResponse>> apiRequest, CancellationToken cancellationToken, TimeSpan? diskCacheTime = null)
            where TResponse : class
        {
            if (string.IsNullOrEmpty(cacheKey)) throw new ArgumentException("Cache key cannot be empty!");

            // Cache LVL1
            if (!MemoryCache.TryGetValue(cacheKey, out TResponse response))
            {
                var diskCache = _storeFactory.GetCacheStore<TResponse>();
                response = await diskCache.GetAsync(cacheKey, _connectivity.NetworkAccess != NetworkAccess.Internet, cancellationToken).ConfigureAwait(false);

                if (response is null && _connectivity.NetworkAccess == NetworkAccess.Internet)
                {
                    try
                    {
                        response = await _retryPolicy.ExecuteAsync(apiRequest, cancellationToken).ConfigureAwait(false);
                        if (!cancellationToken.IsCancellationRequested && response is not null)
                            // Cache regardless, no CT token
                            await diskCache.UpsertAsync(cacheKey, response, diskCacheTime ?? CacheExpireTimeSpan, default).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        var url = ex switch
                        {
                            ApiException apiEx => apiEx.Uri.ToString(),
                            _ => string.Empty
                        };

                        Logger.Error(ex, !string.IsNullOrEmpty(url)
                            ? new Dictionary<string, string>() { { nameof(url), url } }
                            : null);
                    }
                }

                if (response is not null)
                    MemoryCache.SetSliding(cacheKey, response, TimeSpan.FromMinutes(30));
            }

            return response;
        }

        private async Task<(IList<SitePriceDto> prices, DateTime modifiedUtc)> RequestSitePriceAsync(HttpMethod httpMethod, string queryString, CancellationToken ct)
        {
            const string sitePrices = "SitePrices";

            if (httpMethod != HttpMethod.Head && httpMethod != HttpMethod.Get)
                throw new ArgumentOutOfRangeException(nameof(httpMethod));

            var uri = Path.Combine(Constants.ApiUrlBase, !string.IsNullOrEmpty(queryString) ? $"{sitePrices}?{queryString}" : sitePrices);
            var request = new HttpRequestMessage(httpMethod, uri);
            request.Headers.Add(Constants.AuthHeader, Constants.ApiKeySitePrices);
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromSeconds(10));
            using var response = await HttpClient.SendAsync(request, cts.Token).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            var modifiedUtc = response.Content.Headers.LastModified is not null
                ? response.Content.Headers.LastModified.Value.UtcDateTime
                : DateTime.UtcNow;

            var result = default(IList<SitePriceDto>);
            if (httpMethod == HttpMethod.Get)
            {
                using var s = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
                result = DeserializeJsonFromStream<List<SitePriceDto>>(s);
            }

            return (result, modifiedUtc);
        }
    }
}