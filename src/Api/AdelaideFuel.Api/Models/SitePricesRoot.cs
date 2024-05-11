using AdelaideFuel.Shared;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace AdelaideFuel.Api
{
    public class SitePricesRoot
    {
        [JsonPropertyName("SitePrices")]
        public List<SitePriceDto> SitePrices { get; set; }
    }
}