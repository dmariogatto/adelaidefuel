using AdelaideFuel.Shared;
using System;
using System.Globalization;
using System.Runtime.Serialization;

namespace AdelaideFuel.TableStore.Entities
{
    public class SitePriceExceptionLogEntity : BaseTableStoreEntity, ITableStoreEntity
    {
        public SitePriceExceptionLogEntity() { }

        public SitePriceExceptionLogEntity(
            int brandId,
            ISitePrice sitePrice,
            double adjustedPrice)
        {
            BrandId = brandId;
            SiteId = sitePrice.SiteId;
            FuelId = sitePrice.FuelId;
            TransactionDateUtc = sitePrice.TransactionDateUtc;
            OriginalPrice = sitePrice.Price;
            AdjustedPrice = adjustedPrice;
        }

        [IgnoreDataMember]
        public int BrandId
        {
            get => int.TryParse(PartitionKey, out var id) ? id : -1;
            set => PartitionKey = value.ToString(CultureInfo.InvariantCulture);
        }

        private int _siteId;
        public int SiteId
        {
            get => _siteId;
            set
            {
                _siteId = value;
                UpdateRowKey();
            }
        }

        private int _fuelId;
        public int FuelId
        {
            get => _fuelId;
            set
            {
                _fuelId = value;
                UpdateRowKey();
            }
        }

        private DateTime _transactionDateUtc;
        public DateTime TransactionDateUtc
        {
            get => _transactionDateUtc;
            set
            {
                _transactionDateUtc = value;
                UpdateRowKey();
            }
        }

        public double OriginalPrice { get; set; }
        public double AdjustedPrice { get; set; }

        public override bool IsDifferent(ITableStoreEntity entity)
        {
            if (entity is SitePriceExceptionLogEntity other && Equals(other))
            {
                return
                    BrandId != other.BrandId ||
                    SiteId != other.SiteId ||
                    FuelId != other.FuelId ||
                    !OriginalPrice.FuzzyEquals(other.OriginalPrice, 0.1) ||
                    !AdjustedPrice.FuzzyEquals(other.AdjustedPrice, 0.1) ||
                    IsActive != other.IsActive;
            }

            return true;
        }

        private void UpdateRowKey()
            => RowKey = $"{_siteId}_{_fuelId}_{_transactionDateUtc:yyyyMMddTHHmmss.fff}";

        public override string ToString()
            => $"{SiteId} | {FuelId} | {OriginalPrice:0.00} | {AdjustedPrice:0.00}";

        public override bool Equals(object obj)
        {
            return obj is SitePriceExceptionLogEntity other &&
                   PartitionKey == other.PartitionKey &&
                   RowKey == other.RowKey;
        }

        public override int GetHashCode() => (PartitionKey, RowKey).GetHashCode();
    }
}