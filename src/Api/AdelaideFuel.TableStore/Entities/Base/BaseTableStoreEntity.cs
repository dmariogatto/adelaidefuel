using Azure;
using System;

namespace AdelaideFuel.TableStore
{
    public abstract class BaseTableStoreEntity : ITableStoreEntity
    {
        public BaseTableStoreEntity() { }

        public BaseTableStoreEntity(string partitionKey, string rowKey)
        {
            PartitionKey = partitionKey;
            RowKey = rowKey;
        }

        public string PartitionKey { get; set; }
        public string RowKey { get; set; }

        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }

        public bool IsActive { get; set; }

        public abstract bool IsDifferent(ITableStoreEntity entity);
    }
}