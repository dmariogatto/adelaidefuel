using Newtonsoft.Json;

namespace AdelaideFuel.Shared
{
    public class GeographicRegionDto : IGeographicRegion
    {
        [JsonProperty("GeoRegionLevel")]
        public int GeoRegionLevel { get; set; }

        [JsonProperty("GeoRegionId")]
        public int GeoRegionId { get; set; }

        [JsonProperty("Name")]
        public string Name { get; set; }

        [JsonProperty("Abbrev")]
        public string Abbrev { get; set; }

        [JsonProperty("GeoRegionParentId")]
        public int? GeoRegionParentId { get; set; }
    }
}
