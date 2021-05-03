using Microsoft.Azure.Cosmos.Table;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AdelaideFuel.TableStore.Repositories
{
    public class TableRepository<T> : ITableRepository<T> where T : IEntity, new()
    {
        private readonly CloudTable _cloudTable;

        public TableRepository(CloudTableClient tableClient)
        {
            if (tableClient == null)
                throw new ArgumentNullException(nameof(tableClient));

            var tableName = typeof(T).Name;

            _cloudTable = tableClient.GetTableReference(tableName)
                ?? throw new NullReferenceException($"Reference to table '{tableName}' cannot be null!");
        }

        public Task CreateIfNotExistsAsync(CancellationToken cancellationToken)
        {
            return _cloudTable.CreateIfNotExistsAsync(cancellationToken);
        }

        public Task<IList<T>> GetPartitionAsync(string partitionKey, CancellationToken cancellationToken)
        {
            var query = new TableQuery<T>();
            query = query.Where(
                    TableQuery.GenerateFilterCondition(
                        nameof(TableEntity.PartitionKey),
                        QueryComparisons.Equal,
                        partitionKey));
            return ExecuteQueryAsync(query, cancellationToken);
        }

        public Task<IList<T>> GetPartitionsAsync(IEnumerable<string> partitionKeys, CancellationToken cancellationToken)
        {
            var query = new TableQuery<T>();

            var combined = string.Empty;
            foreach (var pk in partitionKeys)
            {
                var predicate = TableQuery.GenerateFilterCondition(
                        nameof(TableEntity.PartitionKey),
                        QueryComparisons.Equal,
                        pk);
                combined = !string.IsNullOrEmpty(combined)
                    ? TableQuery.CombineFilters(
                        combined,
                        TableOperators.Or,
                        predicate)
                    : predicate;
            }

            query = query.Where(combined);
            return ExecuteQueryAsync(query, cancellationToken);
        }

        public async Task<T> GetEntityAsync(string partitionKey, string rowKey, CancellationToken cancellationToken)
        {
            var retrieveOp = TableOperation.Retrieve<T>(partitionKey, rowKey);
            var result = await _cloudTable.ExecuteAsync(retrieveOp, cancellationToken)
                .ConfigureAwait(false);
            return (T)result.Result;
        }

        public Task<IList<T>> GetAllEntitiesAsync(CancellationToken cancellationToken)
        {
            var query = new TableQuery<T>();
            return ExecuteQueryAsync(query, cancellationToken);
        }

        public Task DeleteAsync(IEnumerable<T> entities, ILogger logger, CancellationToken cancellationToken)
            => BatchOpsAsync(entities, (bo, e) => bo.Delete(e), logger, cancellationToken);

        public Task InsertOrReplaceBulkAsync(IEnumerable<T> entities, ILogger logger, CancellationToken cancellationToken)
            => BatchOpsAsync(entities, (bo, e) => bo.InsertOrReplace(e), logger, cancellationToken);

        public async Task<IList<T>> ExecuteQueryAsync(TableQuery<T> query, CancellationToken cancellationToken)
        {
            var results = new List<T>();
            // Initialize continuation token to start from the beginning of the table.
            var continuationToken = default(TableContinuationToken);

            do
            {
                // Retrieve a segment (1000 entities)
                var tableQueryResult = await _cloudTable.ExecuteQuerySegmentedAsync(query, continuationToken)
                    .ConfigureAwait(false);
                // Assign the new continuation token to tell the service where to
                // continue on the next iteration (or null if it has reached the end)
                continuationToken = tableQueryResult.ContinuationToken;
                results.AddRange(tableQueryResult.Results);
            } while (continuationToken != null && (cancellationToken == default || !cancellationToken.IsCancellationRequested));

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

            var batchOps = new List<TableBatchOperation>();
            foreach (var p in allPartitions)
            {
                var oldPartition = groupedEntities.ContainsKey(p) ? groupedEntities[p] : new List<T>(0);
                var newPartition = newEntities.ContainsKey(p) ? newEntities[p] : new List<T>(0);
                batchOps.AddRange(ProcessPartition(p, oldPartition, newPartition, deleteDeactivations, logger));
            }

            entityWatch.Stop();

            var opsWatch = new System.Diagnostics.Stopwatch();
            opsWatch.Start();

            foreach (var bo in batchOps)
            {
                logger.LogInformation($"Batch op {batchOps.IndexOf(bo) + 1} of {batchOps.Count} for '{_cloudTable.Name}'");

                if (!simulate)
                {
                    var result = await _cloudTable.ExecuteBatchAsync(bo, cancellationToken)
                        .ConfigureAwait(false);
                }
            }

            opsWatch.Stop();

            logger.LogInformation($"Completed batch ops for '{_cloudTable.Name}' in {opsWatch.ElapsedMilliseconds}ms");

            var changes = batchOps.SelectMany(bo => bo.Select(o => o.Entity)).OfType<T>().ToList();
            return (changes, batchOps.Sum(bo => bo.Count));
        }

        private async Task BatchOpsAsync(IEnumerable<T> entities, Action<TableBatchOperation, T> batchOpAction, ILogger logger, CancellationToken cancellationToken)
        {
            var batchOps = new List<TableBatchOperation>();

            logger.LogInformation($"Creating batch ops for '{_cloudTable.Name}'");

            foreach (var g in entities.GroupBy(a => a.PartitionKey))
            {
                var batchOp = new TableBatchOperation();
                foreach (var e in g)
                {
                    batchOpAction.Invoke(batchOp, e);

                    // Maximum operations in a batch
                    if (batchOp.Count == 100)
                    {
                        batchOps.Add(batchOp);
                        batchOp = new TableBatchOperation();
                    }
                }

                // Batch can only contain operations in the same partition
                if (batchOp.Count > 0)
                {
                    batchOps.Add(batchOp);
                }
            }

            // Prevents "storage conflict" in Azure, as the table can take a few seconds to actually be created
            while (!await _cloudTable.ExistsAsync().ConfigureAwait(false)) ;

            foreach (var bo in batchOps)
            {
                logger.LogInformation($"Batch {batchOps.IndexOf(bo) + 1} of {batchOps.Count} for '{_cloudTable.Name}'");
                var result = await _cloudTable.ExecuteBatchAsync(bo, cancellationToken)
                    .ConfigureAwait(false);
            }
        }

        private IList<TableBatchOperation> ProcessPartition(string partition, IList<T> oldPartition, IList<T> newPartition, bool deleteDeactivations, ILogger logger)
        {
            var batchOps = new List<TableBatchOperation>();

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

                    var additions = new List<IEntity>();
                    var updates = new List<IEntity>();
                    var deactivations = new List<IEntity>();

                    if (entitiesToAdd.Count > 0 ||
                        entitiesExisting.Count > 0 ||
                        entitiesToRemove.Count > 0)
                    {
                        var batchOp = new TableBatchOperation();

                        foreach (var e in entitiesToAdd)
                        {
                            // Save the new entity
                            var newEntity = newPartition.Single(i => i.Equals(e));
                            newEntity.IsActive = true;
                            batchOp.InsertOrReplace(newEntity);

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
                                batchOp.InsertOrReplace(newEntity);

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

                                if (deleteDeactivations)
                                    batchOp.Delete(currentEntity);
                                else
                                    batchOp.InsertOrReplace(currentEntity);

                                deactivations.Add(currentEntity);
                            }
                        }

                        if (batchOp.Count > 100)
                        {
                            // Max 100 ops in a batch
                            for (var i = 0; i < batchOp.Count; i += 100)
                            {
                                var ops = batchOp.Skip(i).Take(100);
                                var bo = new TableBatchOperation();
                                foreach (var op in ops)
                                {
                                    bo.Add(op);
                                }
                                batchOps.Add(bo);
                            }
                        }
                        else if (batchOp.Count > 0)
                        {
                            batchOps.Add(batchOp);
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

            return batchOps;
        }
    }
}