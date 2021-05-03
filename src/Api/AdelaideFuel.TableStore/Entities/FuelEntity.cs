using AdelaideFuel.Shared;
using Microsoft.Azure.Cosmos.Table;
using System.Globalization;

namespace AdelaideFuel.TableStore.Entities
{
    public class FuelEntity : TableEntity, IEntity, IFuel
    {
        public FuelEntity() { }

        public FuelEntity(IFuel fuel)
        {
            FuelId = fuel.FuelId;
            Name = fuel.Name;
        }

        public int FuelId
        {
            get => int.TryParse(PartitionKey, out var id) ? id : -1;
            set => PartitionKey = value.ToString(CultureInfo.InvariantCulture);
        }

        public string Name
        {
            get => System.Web.HttpUtility.UrlDecode(RowKey);
            set => RowKey = System.Web.HttpUtility.UrlEncode(value);
        }

        public int SortOrder { get; set; } = int.MaxValue;

        public bool IsActive { get; set; }

        public bool IsDifferent(IEntity entity)
        {
            if (entity is FuelEntity other && Equals(other))
            {
                return
                    IsActive != other.IsActive;
            }

            return true;
        }

        public FuelDto ToFuel() => new FuelDto()
        {
            FuelId = FuelId,
            Name = Name,
        };

        public override string ToString()
            => Name;

        public override bool Equals(object obj)
        {
            return obj is FuelEntity other &&
                   PartitionKey == other.PartitionKey &&
                   RowKey == other.RowKey;
        }

        public override int GetHashCode() => (PartitionKey, RowKey).GetHashCode();
    }
}