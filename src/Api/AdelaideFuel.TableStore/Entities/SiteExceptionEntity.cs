using Microsoft.Azure.Cosmos.Table;
using System.Globalization;

namespace AdelaideFuel.TableStore.Entities
{
    public class SiteExceptionEntity : TableEntity, IEntity
    {
        public SiteExceptionEntity() { }

        public SiteExceptionEntity(int brandId, int siteId, string name)
        {
            BrandId = brandId;
            SiteId = siteId;
            Name = name;
        }

        public int BrandId
        {
            get => int.TryParse(PartitionKey, out var id) ? id : -1;
            set => PartitionKey = value.ToString(CultureInfo.InvariantCulture);
        }

        public int SiteId
        {
            get => int.TryParse(RowKey, out var id) ? id : -1;
            set => RowKey = value.ToString(CultureInfo.InvariantCulture);
        }

        public string Name { get; set; }

        public bool IsActive { get; set; } = true;

        public bool IsDifferent(IEntity entity)
        {
            if (entity is SiteExceptionEntity other && Equals(other))
            {
                return
                    BrandId != other.BrandId ||
                    SiteId != other.SiteId ||
                    Name != other.Name ||
                    IsActive != other.IsActive;
            }

            return true;
        }

        public override string ToString()
            => Name;

        public override bool Equals(object obj)
        {
            return obj is SiteExceptionEntity other &&
                   PartitionKey == other.PartitionKey &&
                   RowKey == other.RowKey;
        }

        public override int GetHashCode() => (PartitionKey, RowKey).GetHashCode();
    }
}