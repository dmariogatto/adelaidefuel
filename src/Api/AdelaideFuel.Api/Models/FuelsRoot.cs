using AdelaideFuel.Shared;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace AdelaideFuel.Api
{
    public class FuelsRoot
    {
        [JsonProperty("Fuels")]
        public List<FuelDto> Fuels { get; set; }
    }
}