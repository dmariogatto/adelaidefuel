using Newtonsoft.Json;
using System;

namespace AdelaideFuel.Shared
{
    public class SitePriceDto : ISitePrice
    {
        [JsonProperty("SiteId")]
        public int SiteId { get; set; }

        [JsonProperty("FuelId")]
        public int FuelId { get; set; }

        [JsonProperty("CollectionMethod")]
        public string CollectionMethod { get; set; }

        [JsonProperty("TransactionDateUtc")]
        public DateTime TransactionDateUtc { get; set; }

        [JsonProperty("Price")]
        public double Price { get; set; }

        public double PriceInCents() => Price / 10d;
    }
}
