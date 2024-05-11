using AdelaideFuel.Shared;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace AdelaideFuel.Api
{
    public class FuelsRoot
    {
        [JsonPropertyName("Fuels")]
        public List<FuelDto> Fuels { get; set; }
    }
}