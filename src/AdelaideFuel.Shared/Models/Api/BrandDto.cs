using System.Text.Json.Serialization;

namespace AdelaideFuel.Shared
{
    public class BrandDto : IBrand, IFuelLookup
    {
        [JsonIgnore]
        public int Id => BrandId;

        [JsonPropertyName("BrandId")]
        public int BrandId { get; set; }

        [JsonPropertyName("Name")]
        public string Name { get; set; }
    }
}