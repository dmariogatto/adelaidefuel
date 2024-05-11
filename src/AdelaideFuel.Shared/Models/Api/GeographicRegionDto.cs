using System.Text.Json.Serialization;

namespace AdelaideFuel.Shared
{
    public class GeographicRegionDto : IGeographicRegion
    {
        [JsonPropertyName("GeoRegionLevel")]
        public int GeoRegionLevel { get; set; }

        [JsonPropertyName("GeoRegionId")]
        public int GeoRegionId { get; set; }

        [JsonPropertyName("Name")]
        public string Name { get; set; }

        [JsonPropertyName("Abbrev")]
        public string Abbrev { get; set; }

        [JsonPropertyName("GeoRegionParentId")]
        public int? GeoRegionParentId { get; set; }
    }
}