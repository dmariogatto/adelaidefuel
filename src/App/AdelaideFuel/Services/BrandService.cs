using AdelaideFuel.Api;
using Microsoft.Maui.Devices;
using Microsoft.Maui.Networking;
using Microsoft.Maui.Storage;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AdelaideFuel.Services
{
    public class BrandService : BaseService, IBrandService
    {
        private readonly static TimeSpan CacheExpireTimeSpan = TimeSpan.FromDays(7);

        private readonly IConnectivity _connectivity;

        private readonly IAdelaideFuelApi _fuelApi;

        private readonly bool _isHighDensity;
        private readonly string _cachePath;
        private readonly string _imgPathFormat;

        private readonly SemaphoreSlim _imageSemaphore = new SemaphoreSlim(1, 1);

        public BrandService(
            IConnectivity connectivity,
            IDeviceDisplay deviceDisplay,
            IFileSystem fileSystem,
            IAdelaideFuelApi adelaideFuelApi,
            ICacheService cacheService,
            ILogger logger) : base(cacheService, logger)
        {
            _connectivity = connectivity;

            _fuelApi = adelaideFuelApi;

            _isHighDensity = deviceDisplay.MainDisplayInfo.Density >= 3;
            _cachePath = Path.Combine(fileSystem.CacheDirectory, "brand_imgs");
            _imgPathFormat = Path.Combine(_cachePath, "{0}.png");

            Directory.CreateDirectory(_cachePath);
        }

        public string GetBrandImagePath(int brandId)
        {
            var path = string.Format(_imgPathFormat, brandId);
            _ = GetBrandImagePathAsync(brandId, CancellationToken.None);
            return File.Exists(path) ? path : string.Empty;
        }

        public async Task<string> GetBrandImagePathAsync(int brandId, CancellationToken cancellationToken)
        {
            var result = await GetBrandImagePathsAsync([brandId], cancellationToken).ConfigureAwait(false);
            return result.TryGetValue(brandId, out var path) ? path : string.Empty;
        }

        public async Task<IReadOnlyDictionary<int, string>> GetBrandImagePathsAsync(IReadOnlyList<int> brandIds, CancellationToken cancellationToken)
        {
            var result = brandIds.ToDictionary(i => i, i => string.Format(_imgPathFormat, i));

            await _imageSemaphore.WaitAsync().ConfigureAwait(false);

            try
            {
                var expired = result
                    .Where(i => IsBrandImageExpired(i.Value))
                    .ToDictionary(i => i.Key, i => i.Value);

                await Parallel.ForEachAsync(expired, cancellationToken, async (kv, ct) =>
                {
                    if (_connectivity.NetworkAccess != NetworkAccess.Internet)
                        return;

                    try
                    {
                        var imgBytes = await _fuelApi.GetBrandImageAsync(Constants.ApiKeyBrandImg, kv.Key, _isHighDensity, ct).ConfigureAwait(false);
                        if (imgBytes?.Length > 0)
                        {
                            await File.WriteAllBytesAsync(kv.Value, imgBytes, ct).ConfigureAwait(false);
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Error(ex);
                        File.Delete(kv.Value);
                    }
                }).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
            finally
            {
                _imageSemaphore.Release();
            }

            foreach (var kv in result)
            {
                if (!File.Exists(kv.Value))
                {
                    result[kv.Key] = string.Empty;
                }
            }

            return result;
        }

        private bool IsBrandImageExpired(string imgPath)
            => !File.Exists(imgPath) || File.GetCreationTimeUtc(imgPath) + CacheExpireTimeSpan < DateTime.UtcNow;
    }
}