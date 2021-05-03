using AdelaideFuel.Shared;

namespace AdelaideFuel.TableStore.Entities
{
    public class SitePriceArchiveEntity : SitePriceEntity
    {
        public SitePriceArchiveEntity() { }

        public SitePriceArchiveEntity(int brandId, ISitePrice sitePrice) : base(brandId, sitePrice)
        {
        }
    }
}