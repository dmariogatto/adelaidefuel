using AdelaideFuel.Shared;
using Microsoft.Azure.Cosmos.Table;
using System.Globalization;

namespace AdelaideFuel.TableStore.Entities
{
    public class BrandEntity : TableEntity, IEntity, IBrand
    {
        public BrandEntity() { }

        public BrandEntity(IBrand brand)
        {
            BrandId = brand.BrandId;
            Name = brand.Name;
        }

        public int BrandId
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
            if (entity is BrandEntity other && Equals(other))
            {
                return
                    IsActive != other.IsActive;
            }

            return true;
        }

        public BrandDto ToBrand() => new BrandDto()
        {
            BrandId = BrandId,
            Name = Name,
        };

        public override string ToString()
            => Name;

        public override bool Equals(object obj)
        {
            return obj is BrandEntity other &&
                   PartitionKey == other.PartitionKey &&
                   RowKey == other.RowKey;
        }

        public override int GetHashCode() => (PartitionKey, RowKey).GetHashCode();
    }
}