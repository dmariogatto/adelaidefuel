using AdelaideFuel.Functions.Services;
using AdelaideFuel.Shared;
using AdelaideFuel.TableStore.Entities;
using AdelaideFuel.TableStore.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
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

        private static readonly string[] ValidPostfix = ["2x.png", "3x.png"];

        private readonly ITableRepository<BrandEntity> _brandRepository;

        private readonly IBlobService _blobService;

        private readonly ILogger _logger;

        public Brands(
            ITableRepository<BrandEntity> brandRepository,
            IBlobService blobService,
            ILoggerFactory loggerFactory)
        {
            _brandRepository = brandRepository;

            _blobService = blobService;
            _logger = loggerFactory.CreateLogger<Brands>();
        }

        [Function(BrandFuncName)]
        public async Task<IActionResult> GetBrands(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "Brands")] HttpRequest req,
            CancellationToken ct)
        {
            var brands = await _brandRepository.GetAllEntitiesAsync(ct);
            var projected = brands
                .Where(s => s.IsActive)
                .OrderBy(s => s.SortOrder)
                .ThenBy(s => s.Name)
                .ThenBy(s => s.BrandId)
                .Select(s => s.ToBrand());
            return new JsonResult(projected);
        }

        [Function(BrandImgFuncName)]
        public async Task<IActionResult> GetBrandImg(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "Brand/Img/{fileName}")] HttpRequest req,
            string fileName,
            CancellationToken ct)
        {
            fileName = fileName?.ToLowerInvariant();

            if (!string.IsNullOrEmpty(fileName) &&
                ValidPostfix.Any(fileName.EndsWith) &&
                int.TryParse(fileName[..fileName.IndexOf("@")], out var brandId))
            {
                var basePath = Path.Combine("brands", "imgs");

                var blobImgPath = Path.Combine(basePath, fileName);
                if (!await _blobService.ExistsAsync(blobImgPath, ct))
                {
                    fileName = $"default{fileName[fileName.IndexOf("@")..]}";
                    blobImgPath = Path.Combine(basePath, fileName);
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