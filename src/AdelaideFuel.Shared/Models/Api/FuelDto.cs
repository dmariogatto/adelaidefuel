using System.Text.Json.Serialization;

namespace AdelaideFuel.Shared
{
    public class FuelDto : IFuel, IFuelLookup
    {
        [JsonIgnore]
        public int Id => FuelId;

        [JsonPropertyName("FuelId")]
        public int FuelId { get; set; }

        [JsonPropertyName("Name")]
        public string Name { get; set; }
    }
}