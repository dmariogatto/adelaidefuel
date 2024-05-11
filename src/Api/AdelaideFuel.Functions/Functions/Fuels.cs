using AdelaideFuel.Shared;
using AdelaideFuel.TableStore.Entities;
using AdelaideFuel.TableStore.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AdelaideFuel.Functions
{
    public class Fuels
    {
        private readonly ITableRepository<FuelEntity> _fuelRepository;
        private readonly ITableRepository<SitePriceEntity> _sitePriceRepository;

        private readonly ILogger _logger;

        public Fuels(
            ITableRepository<FuelEntity> fuelRepository,
            ITableRepository<SitePriceEntity> sitePriceRepository,
            ILoggerFactory loggerFactory)
        {
            _fuelRepository = fuelRepository;
            _sitePriceRepository = sitePriceRepository;
            _logger = loggerFactory.CreateLogger<Fuels>();
        }

        [Function(nameof(Fuels))]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "Fuels")] HttpRequest req,
            CancellationToken ct)
        {
            var fuels = await _fuelRepository.GetAllEntitiesAsync(ct);
            var projected = fuels
                .Where(s => s.IsActive)
                .OrderBy(s => s.SortOrder)
                .ThenBy(s => s.Name)
                .ThenBy(s => s.FuelId)
                .Select(s => s.ToFuel());
            return new JsonResult(projected);
        }

        [Function(nameof(EmptyFuels))]
        public async Task<IActionResult> EmptyFuels(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "Fuels/Empty")] HttpRequest req,
            CancellationToken ct)
        {
            var fuelsTask = _fuelRepository.GetAllEntitiesAsync(ct);
            var sitePricesTask = _sitePriceRepository.GetAllEntitiesAsync(ct);

            await Task.WhenAll(fuelsTask, sitePricesTask);

            var activeFuelIds = new HashSet<int>(sitePricesTask.Result
                .Where(i => i.IsActive)
                .Select(i => i.FuelId));

            var emptyFuels = fuelsTask.Result
                .Where(i => i.IsActive && !activeFuelIds.Contains(i.FuelId))
                .Select(i => i.ToFuel());

            return new JsonResult(emptyFuels);
        }
    }
}