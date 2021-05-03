using Microsoft.Azure.Cosmos.Table;

namespace AdelaideFuel.TableStore
{
    public interface IEntity : ITableEntity
    {
        bool IsActive { get; set; }
        bool IsDifferent(IEntity entity);
    }
}