using AdelaideFuel.Shared;
using AdelaideFuel.TableStore.Entities;
using AdelaideFuel.TableStore.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
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

        public Fuels(
            ITableRepository<FuelEntity> fuelRepository,
            ITableRepository<SitePriceEntity> sitePriceRepository)
        {
            _fuelRepository = fuelRepository;
            _sitePriceRepository = sitePriceRepository;
        }

        [FunctionName(nameof(Fuels))]
        public async Task<IList<FuelDto>> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "Fuels")] HttpRequest req,
            ILogger log,
            CancellationToken ct)
        {
            var fuels = await _fuelRepository.GetAllEntitiesAsync(ct);
            return fuels
                .Where(s => s.IsActive)
                .OrderBy(s => s.SortOrder)
                .ThenBy(s => s.Name)
                .ThenBy(s => s.FuelId)
                .Select(s => s.ToFuel()).ToList();
        }

        [FunctionName(nameof(EmptyFuels))]
        public async Task<IList<FuelDto>> EmptyFuels(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "Fuels/Empty")] HttpRequest req,
            ILogger log,
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
                .ToList();

            return emptyFuels.Select(i => i.ToFuel()).ToList();
        }
    }
}