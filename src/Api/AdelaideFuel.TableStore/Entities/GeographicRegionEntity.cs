using AdelaideFuel.Shared;
using Microsoft.Azure.Cosmos.Table;
using System.Globalization;

namespace AdelaideFuel.TableStore.Entities
{
    public class GeographicRegionEntity : TableEntity, IEntity, IGeographicRegion
    {
        public GeographicRegionEntity() { }

        public GeographicRegionEntity(IGeographicRegion geographicRegion)
        {
            GeoRegionLevel = geographicRegion.GeoRegionLevel;
            GeoRegionId = geographicRegion.GeoRegionId;
            Name = geographicRegion.Name;
            Abbrev = geographicRegion.Abbrev;
            GeoRegionParentId = geographicRegion.GeoRegionParentId;
        }

        public int GeoRegionLevel
        {
            get => int.TryParse(PartitionKey, out var id) ? id : -1;
            set => PartitionKey = value.ToString(CultureInfo.InvariantCulture);
        }

        public int GeoRegionId
        {
            get => int.TryParse(RowKey, out var id) ? id : -1;
            set => RowKey = value.ToString(CultureInfo.InvariantCulture);
        }

        public string Name { get; set; }
        public string Abbrev { get; set; }
        public int? GeoRegionParentId { get; set; }

        public bool IsActive { get; set; }

        public bool IsDifferent(IEntity entity)
        {
            if (entity is GeographicRegionEntity other && Equals(other))
            {
                return
                    Name != other.Name ||
                    Abbrev != other.Abbrev ||
                    GeoRegionParentId != other.GeoRegionParentId ||
                    IsActive != other.IsActive;
            }

            return true;
        }

        public GeographicRegionDto ToGeographicRegion() => new GeographicRegionDto()
        {
            GeoRegionLevel = GeoRegionLevel,
            GeoRegionId = GeoRegionId,
            Name = Name,
            Abbrev = Abbrev,
            GeoRegionParentId = GeoRegionParentId,
        };

        public override string ToString()
            => Name;

        public override bool Equals(object obj)
        {
            return obj is GeographicRegionEntity other &&
                   PartitionKey == other.PartitionKey &&
                   RowKey == other.RowKey;
        }

        public override int GetHashCode() => (PartitionKey, RowKey).GetHashCode();
    }
}