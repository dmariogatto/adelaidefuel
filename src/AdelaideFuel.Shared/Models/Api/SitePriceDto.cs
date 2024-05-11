using System;
using System.Text.Json.Serialization;

namespace AdelaideFuel.Shared
{
    public class SitePriceDto : ISitePrice
    {
        [JsonPropertyName("BrandId")]
        public int BrandId { get; set; }

        [JsonPropertyName("SiteId")]
        public int SiteId { get; set; }

        [JsonPropertyName("FuelId")]
        public int FuelId { get; set; }

        [JsonPropertyName("CollectionMethod")]
        public string CollectionMethod { get; set; }

        [JsonPropertyName("TransactionDateUtc"), JsonConverter(typeof(DateUtcJsonConverter))]
        public DateTime TransactionDateUtc { get; set; }

        [JsonPropertyName("Price")]
        public double Price { get; set; }

        public double PriceInCents() => Price / 10d;
    }
}