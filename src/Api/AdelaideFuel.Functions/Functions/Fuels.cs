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

        public Fuels(
            ITableRepository<FuelEntity> fuelRepository)
        {
            _fuelRepository = fuelRepository;
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
                .ThenBy(s => s.FuelId)
                .Select(s => s.ToFuel()).ToList();
        }
    }
}
