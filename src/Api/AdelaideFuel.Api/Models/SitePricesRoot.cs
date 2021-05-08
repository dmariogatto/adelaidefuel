using AdelaideFuel.Shared;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace AdelaideFuel.Api
{
    public class SitePricesRoot
    {
        [JsonProperty("SitePrices")]
        public List<SitePriceDto> SitePrices { get; set; }
    }
}