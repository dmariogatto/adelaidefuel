using AdelaideFuel.Shared;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace AdelaideFuel.Api
{
    public class BrandsRoot
    {
        [JsonProperty("Brands")]
        public List<BrandDto> Brands { get; set; }
    }
}
