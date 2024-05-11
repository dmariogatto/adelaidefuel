using AdelaideFuel.Shared;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace AdelaideFuel.Api
{
    public class GeographicRegionsRoot
    {
        [JsonPropertyName("GeographicRegions")]
        public List<GeographicRegionDto> GeographicRegions { get; set; }
    }
}