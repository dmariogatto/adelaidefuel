using Microsoft.Azure.Cosmos.Table;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace AdelaideFuel.TableStore.Repositories
{
    public interface ITableRepository<T> where T : IEntity, new()
    {
        Task CreateIfNotExistsAsync(CancellationToken cancellationToken);

        Task<IList<T>> GetPartitionAsync(string partitionKey, CancellationToken cancellationToken);
        Task<IList<T>> GetPartitionsAsync(IList<string> partitionKeys, CancellationToken cancellationToken);

        Task<T> GetEntityAsync(string partitionKey, string rowKey, CancellationToken cancellationToken);
        Task<IList<T>> GetEntitiesAsync(IList<(string partitionKey, string rowKey)> keys, CancellationToken cancellationToken);
        Task<IList<T>> GetAllEntitiesAsync(CancellationToken cancellationToken);

        Task DeleteAsync(IEnumerable<T> entities, ILogger logger, CancellationToken cancellationToken);

        Task InsertOrReplaceBulkAsync(IEnumerable<T> entities, ILogger logger, CancellationToken cancellationToken);

        Task<IList<T>> ExecuteQueryAsync(TableQuery<T> query, CancellationToken cancellationToken);

        Task<(IList<T> changes, int ops)> SyncPartitionsWithDeactivateAsync(Dictionary<string, List<T>> newEntities, ILogger logger, CancellationToken cancellationToken, bool simulate = false);
        Task<(IList<T> changes, int ops)> SyncPartitionsWithDeleteAsync(Dictionary<string, List<T>> newEntities, ILogger logger, CancellationToken cancellationToken, bool simulate = false);
    }
}