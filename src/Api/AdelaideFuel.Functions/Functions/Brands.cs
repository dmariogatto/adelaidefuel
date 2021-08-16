using AdelaideFuel.Functions.Services;
using AdelaideFuel.Shared;
using AdelaideFuel.TableStore.Entities;
using AdelaideFuel.TableStore.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AdelaideFuel.Functions
{
    public class Brands
    {
        private const string BrandFuncName = nameof(Brands);
        private const string BrandImgFuncName = "BrandImg";

        private static readonly string[] ValidPostfix = new[] { "2x.png", "3x.png" };

        private readonly ITableRepository<BrandEntity> _brandRepository;

        private readonly IBlobService _blobService;

        public Brands(
            ITableRepository<BrandEntity> brandRepository,
            IBlobService blobService)
        {
            _brandRepository = brandRepository;

            _blobService = blobService;
        }

        [FunctionName(BrandFuncName)]
        public async Task<IList<BrandDto>> GetBrands(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "Brands")] HttpRequest req,
            ILogger log,
            CancellationToken ct)
        {
            var brands = await _brandRepository.GetAllEntitiesAsync(ct);
            return brands
                .Where(s => s.IsActive)
                .OrderBy(s => s.SortOrder)
                .ThenBy(s => s.Name)
                .ThenBy(s => s.BrandId)
                .Select(s => s.ToBrand()).ToList();
        }

        [FunctionName(BrandImgFuncName)]
        public async Task<IActionResult> GetBrandImg(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "Brand/Img/{fileName}")] HttpRequest req,
            string fileName,
            ILogger log,
            CancellationToken ct)
        {
            fileName = fileName?.ToLowerInvariant();

            if (!string.IsNullOrEmpty(fileName) &&
                ValidPostfix.Any(i => fileName.EndsWith(i)) &&
                int.TryParse(fileName.Substring(0, fileName.IndexOf("@")), out var brandId))
            {
                var blobImgPath = Path.Combine("brands", "imgs", fileName);
                if (!await _blobService.ExistsAsync(blobImgPath, ct))
                {
                    fileName = $"default{fileName.Substring(fileName.IndexOf("@"))}";
                    blobImgPath = Path.Combine("brands", "imgs", fileName);
                }

                using var stream = await _blobService.OpenReadAsync(blobImgPath, ct);
                using var memoryStream = new MemoryStream();
                await stream.CopyToAsync(memoryStream, ct);

                return new CachedFileContentResult(memoryStream.ToArray(), "image/png", TimeSpan.FromDays(5))
                {
                    FileDownloadName = fileName
                };
            }

            return new NotFoundResult();
        }
    }
}