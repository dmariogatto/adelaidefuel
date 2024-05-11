using AdelaideFuel.Shared;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace AdelaideFuel.Api
{
    public class BrandsRoot
    {
        [JsonPropertyName("Brands")]
        public List<BrandDto> Brands { get; set; }
    }
}