using Newtonsoft.Json;

namespace AdelaideFuel.Shared
{
    public class FuelDto : IFuel, IFuelLookup
    {
        public int Id => FuelId;

        [JsonProperty("FuelId")]
        public int FuelId { get; set; }

        [JsonProperty("Name")]
        public string Name { get; set; }
    }
}
