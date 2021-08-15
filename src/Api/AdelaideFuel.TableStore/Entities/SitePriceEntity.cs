using AdelaideFuel.Shared;
using Microsoft.Azure.Cosmos.Table;
using System;
using System.Globalization;

namespace AdelaideFuel.TableStore.Entities
{
    public class SitePriceEntity : TableEntity, IEntity, ISitePrice
    {
        public SitePriceEntity() { }

        public SitePriceEntity(int brandId, ISitePrice sitePrice)
        {
            BrandId = brandId;
            SiteId = sitePrice.SiteId;
            FuelId = sitePrice.FuelId;
            CollectionMethod = sitePrice.CollectionMethod;
            TransactionDateUtc = sitePrice.TransactionDateUtc;
            Price = sitePrice.Price;
        }

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

        public string CollectionMethod { get; set; }
        public double Price { get; set; }

        public bool IsActive { get; set; }

        public bool IsDifferent(IEntity entity)
        {
            if (entity is SitePriceEntity other && Equals(other))
            {
                return
                    CollectionMethod != other.CollectionMethod ||
                    !Price.FuzzyEquals(other.Price, 0.1) ||
                    IsActive != other.IsActive;
            }

            return true;
        }

        public SitePriceDto ToSitePrice() => new SitePriceDto()
        {
            BrandId = BrandId,
            SiteId = SiteId,
            FuelId = FuelId,
            CollectionMethod = CollectionMethod,
            TransactionDateUtc = TransactionDateUtc,
            Price = Price,
        };

        private void UpdateRowKey()
            => RowKey = $"{_siteId}_{_fuelId}_{_transactionDateUtc:yyyyMMddTHHmmss.fff}";

        public override string ToString()
            => $"{SiteId} | {FuelId} | {Price:0.00}";

        public override bool Equals(object obj)
        {
            return obj is SitePriceEntity other &&
                   PartitionKey == other.PartitionKey &&
                   RowKey == other.RowKey;
        }

        public override int GetHashCode() => (PartitionKey, RowKey).GetHashCode();
    }
}