using Newtonsoft.Json;

namespace AdelaideFuel.Shared
{
    public class BrandDto : IBrand, IFuelLookup
    {
        [JsonIgnore]
        public int Id => BrandId;

        [JsonProperty("BrandId")]
        public int BrandId { get; set; }

        [JsonProperty("Name")]
        public string Name { get; set; }
    }
}