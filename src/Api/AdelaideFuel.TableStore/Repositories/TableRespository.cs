using AdelaideFuel.TableStore.Models;
using Azure;
using Azure.Data.Tables;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AdelaideFuel.TableStore.Repositories
{
    public class TableRepository<T> : ITableRepository<T> where T : class, ITableStoreEntity, new()
    {
        private const int MaxBatchCount = 100;

        private readonly TableClient _tableClient;

        public TableRepository(TableStorageOptions options)
        {
            if (string.IsNullOrEmpty(options?.AzureWebJobsStorage))
                throw new ArgumentException(nameof(TableStorageOptions.AzureWebJobsStorage));

            _tableClient = new TableClient(options.AzureWebJobsStorage, typeof(T).Name);
        }

        public Task CreateIfNotExistsAsync(CancellationToken cancellationToken)
        {
            return _tableClient.CreateIfNotExistsAsync(cancellationToken);
        }

        public Task<IList<T>> GetPartitionAsync(string partitionKey, CancellationToken cancellationToken)
        {
            var query = _tableClient.QueryAsync<T>(i => i.PartitionKey == partitionKey);
            return ExecuteQueryAsync(query, cancellationToken);
        }

        public Task<IList<T>> GetPartitionsAsync(IList<string> partitionKeys, CancellationToken cancellationToken)
        {
            if (!partitionKeys.Any())
                return Task.FromResult<IList<T>>(Array.Empty<T>());

            var sb = new StringBuilder();
            for (var i = 0; i < partitionKeys.Count; i++)
            {
                var pk = partitionKeys[i];

                sb.AppendFormat("(PartitionKey eq '{0}')", pk);

                if (i < partitionKeys.Count - 1)
                    sb.AppendFormat(" or ");
            }

            var query = _tableClient.QueryAsync<T>(filter: sb.ToString());
            return ExecuteQueryAsync(query, cancellationToken);
        }

        public async Task<T> GetEntityAsync(string partitionKey, string rowKey, CancellationToken cancellationToken)
        {
            var resp = await _tableClient.GetEntityAsync<T>(partitionKey, rowKey).ConfigureAwait(false);
            return resp.Value;
        }

        public Task<IList<T>> GetEntitiesAsync(IList<(string partitionKey, string rowKey)> keys, CancellationToken cancellationToken)
        {
            if (!keys.Any())
                return Task.FromResult<IList<T>>(Array.Empty<T>());

            var sb = new StringBuilder();
            for (var i = 0; i < keys.Count; i++)
            {
                var k = keys[i];

                sb.Append("(");
                sb.AppendFormat("(PartitionKey eq '{0}')", k.partitionKey);
                sb.Append(" and ");
                sb.AppendFormat("(RowKey eq '{0}')", k.rowKey);
                sb.Append(")");

                if (i < keys.Count - 1)
                    sb.AppendFormat(" or ");
            }

            var query = _tableClient.QueryAsync<T>(filter: sb.ToString());
            return ExecuteQueryAsync(query, cancellationToken);
        }

        public Task<IList<T>> GetAllEntitiesAsync(CancellationToken cancellationToken)
        {
            var query = _tableClient.QueryAsync<T>();
            return ExecuteQueryAsync(query, cancellationToken);
        }

        public Task DeleteAsync(IEnumerable<T> entities, ILogger logger, CancellationToken cancellationToken)
            => BatchActionAsync(entities, TableTransactionActionType.Delete, logger, cancellationToken);

        public Task InsertOrReplaceBulkAsync(IEnumerable<T> entities, ILogger logger, CancellationToken cancellationToken)
            => BatchActionAsync(entities, TableTransactionActionType.UpsertReplace, logger, cancellationToken);

        public async Task<IList<T>> ExecuteQueryAsync(AsyncPageable<T> asyncQuery, CancellationToken cancellationToken)
        {
            var results = new List<T>();

            await foreach (var page in asyncQuery.AsPages().ConfigureAwait(false))
            {
                cancellationToken.ThrowIfCancellationRequested();
                results.AddRange(page.Values);
            }

            return results;
        }

        public Task<(IList<T>, int)> SyncPartitionsWithDeactivateAsync(Dictionary<string, List<T>> newEntities, ILogger logger, CancellationToken cancellationToken, bool simulate = false)
            => SyncPartitionsAsync(newEntities, logger, false, simulate, cancellationToken);

        public Task<(IList<T>, int)> SyncPartitionsWithDeleteAsync(Dictionary<string, List<T>> newEntities, ILogger logger, CancellationToken cancellationToken, bool simulate = false)
            => SyncPartitionsAsync(newEntities, logger, true, simulate, cancellationToken);

        private async Task<(IList<T>, int)> SyncPartitionsAsync(Dictionary<string, List<T>> newEntities, ILogger logger, bool deleteDeactivations, bool simulate, CancellationToken cancellationToken)
        {
            var entityWatch = new System.Diagnostics.Stopwatch();
            entityWatch.Start();

            // Faster to bring everything into memory
            var allEntities = await GetAllEntitiesAsync(cancellationToken).ConfigureAwait(false);
            var groupedEntities = allEntities
                .GroupBy(s => s.PartitionKey)
                .ToDictionary(g => g.Key, g => g.Select(i => i).ToList());

            var allPartitions = groupedEntities.Keys.Union(newEntities.Keys);

            var batchActions = new Dictionary<string, IList<TableTransactionAction>>();
            foreach (var p in allPartitions)
            {
                var oldPartition = groupedEntities.ContainsKey(p) ? groupedEntities[p] : new List<T>(0);
                var newPartition = newEntities.ContainsKey(p) ? newEntities[p] : new List<T>(0);
                batchActions[p] = GetActionsToSyncPartition(p, oldPartition, newPartition, deleteDeactivations, logger);
            }

            entityWatch.Stop();

            var opsWatch = new System.Diagnostics.Stopwatch();
            opsWatch.Start();

            var batchCount = 1;
            foreach (var ba in batchActions)
            {
                logger.LogInformation($"Batch {batchCount++} of {batchActions.Count} for '{_tableClient.Name}'");

                if (!simulate)
                {
                    for (var i = 0; i < ba.Value.Count; i += MaxBatchCount)
                    {
                        var batch = ba.Value.Skip(i).Take(MaxBatchCount);
                        var result = await _tableClient.SubmitTransactionAsync(batch, cancellationToken).ConfigureAwait(false);
                    }
                }
            }

            opsWatch.Stop();

            logger.LogInformation($"Completed batch actions for '{_tableClient.Name}' in {opsWatch.ElapsedMilliseconds}ms");

            var changes = batchActions.SelectMany(ba => ba.Value.Select(i => i.Entity)).OfType<T>().ToList();
            return (changes, batchActions.Sum(ba => ba.Value.Count));
        }

        private async Task BatchActionAsync(IEnumerable<T> entities, TableTransactionActionType actionType, ILogger logger, CancellationToken cancellationToken)
        {
            logger.LogInformation($"Creating batch actions for '{_tableClient.Name}'");

            var batchActions = entities
                .GroupBy(a => a.PartitionKey)
                .ToDictionary(g => g.Key, g => g.Select(i => new TableTransactionAction(actionType, i)).ToList());

            var batchCount = 1;
            foreach (var ba in batchActions)
            {
                logger.LogInformation($"Batch {batchCount++} of {batchActions.Count} for '{_tableClient.Name}'");
                for (var i = 0; i < ba.Value.Count; i += MaxBatchCount)
                {
                    var batch = ba.Value.Skip(i).Take(MaxBatchCount);
                    var result = await _tableClient.SubmitTransactionAsync(batch, cancellationToken).ConfigureAwait(false);
                }
            }
        }

        private IList<TableTransactionAction> GetActionsToSyncPartition(string partition, IList<T> oldPartition, IList<T> newPartition, bool deleteDeactivations, ILogger logger)
        {
            var batchActions = new List<TableTransactionAction>();

            try
            {
                if (oldPartition.Count == 0 && newPartition.Count == 0)
                {
                    logger.LogInformation($"{typeof(T).Name}... empty partition '{partition}'");
                }
                else
                {
                    var entitiesToAdd = newPartition.Except(oldPartition).ToList();
                    var entitiesExisting = newPartition.Intersect(oldPartition).ToList();
                    var entitiesToRemove = oldPartition.Except(entitiesExisting).ToList();

                    var additions = new List<ITableStoreEntity>();
                    var updates = new List<ITableStoreEntity>();
                    var deactivations = new List<ITableStoreEntity>();

                    if (entitiesToAdd.Count > 0 ||
                        entitiesExisting.Count > 0 ||
                        entitiesToRemove.Count > 0)
                    {
                        foreach (var e in entitiesToAdd)
                        {
                            // Save the new entity
                            var newEntity = newPartition.Single(i => i.Equals(e));
                            newEntity.IsActive = true;
                            batchActions.Add(new TableTransactionAction(TableTransactionActionType.UpsertReplace, newEntity));

                            additions.Add(newEntity);
                        }

                        foreach (var e in entitiesExisting)
                        {
                            // Get the new entity and compare
                            var currentEntity = oldPartition.Single(i => i.Equals(e));
                            var newEntity = newPartition.Single(i => i.Equals(e));
                            newEntity.IsActive = true;
                            if (currentEntity.IsDifferent(newEntity))
                            {
                                batchActions.Add(new TableTransactionAction(TableTransactionActionType.UpsertReplace, newEntity));
                                updates.Add(newEntity);
                            }
                        }

                        foreach (var e in entitiesToRemove)
                        {
                            // Get the old entity and set it to unactive if required
                            var currentEntity = oldPartition.Single(i => i.Equals(e));
                            if (currentEntity.IsActive)
                            {
                                currentEntity.IsActive = false;

                                batchActions.Add(new TableTransactionAction(
                                    deleteDeactivations
                                    ? TableTransactionActionType.Delete
                                    : TableTransactionActionType.UpsertReplace,
                                    currentEntity));

                                deactivations.Add(currentEntity);
                            }
                        }
                    }

                    if (additions.Any())
                    {
                        logger.LogInformation($"# Additions '{typeof(T).Name}' partiton '{partition}'");
                    }

                    if (updates.Any())
                    {
                        logger.LogInformation($"# Updates '{typeof(T).Name}' partiton '{partition}'");
                    }

                    if (deactivations.Any())
                    {
                        logger.LogInformation($"# Deactivations '{typeof(T).Name}' partiton '{partition}'");
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Error processing partition '{partition}', for entity '{typeof(T).Name}'");
            }

            return batchActions;
        }
    }
}