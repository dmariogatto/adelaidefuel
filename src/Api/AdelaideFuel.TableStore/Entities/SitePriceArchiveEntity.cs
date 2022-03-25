using AdelaideFuel.Shared;

namespace AdelaideFuel.TableStore.Entities
{
    public class SitePriceArchiveEntity : SitePriceEntity
    {
        public SitePriceArchiveEntity() { }

        public SitePriceArchiveEntity(int brandId, ISitePrice sitePrice) : base(brandId, sitePrice)
        {
        }

        protected override void UpdateRowKey()
            => RowKey = $"{SiteId}_{FuelId}_{TransactionDateUtc:yyyyMMddTHHmmss.fff}";
    }
}