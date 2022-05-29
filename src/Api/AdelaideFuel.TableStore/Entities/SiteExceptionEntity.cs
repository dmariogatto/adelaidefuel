using System.Globalization;
using System.Runtime.Serialization;

namespace AdelaideFuel.TableStore.Entities
{
    public class SiteExceptionEntity : BaseTableStoreEntity, ITableStoreEntity
    {
        public SiteExceptionEntity() { }

        public SiteExceptionEntity(int brandId, int siteId, string name)
        {
            BrandId = brandId;
            SiteId = siteId;
            Name = name;
        }

        [IgnoreDataMember]
        public int BrandId
        {
            get => int.TryParse(PartitionKey, out var id) ? id : -1;
            set => PartitionKey = value.ToString(CultureInfo.InvariantCulture);
        }

        [IgnoreDataMember]
        public int SiteId
        {
            get => int.TryParse(RowKey, out var id) ? id : -1;
            set => RowKey = value.ToString(CultureInfo.InvariantCulture);
        }

        public string Name { get; set; }

        public override bool IsDifferent(ITableStoreEntity entity)
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