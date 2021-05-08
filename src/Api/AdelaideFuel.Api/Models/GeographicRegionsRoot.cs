using AdelaideFuel.Shared;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace AdelaideFuel.Api
{
    public class GeographicRegionsRoot
    {
        [JsonProperty("GeographicRegions")]
        public List<GeographicRegionDto> GeographicRegions { get; set; }
    }
}