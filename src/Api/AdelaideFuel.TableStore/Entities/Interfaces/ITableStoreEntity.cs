using Azure.Data.Tables;

namespace AdelaideFuel.TableStore
{
    public interface ITableStoreEntity : ITableEntity
    {
        bool IsActive { get; set; }
        bool IsDifferent(ITableStoreEntity entity);
    }
}