using AdelaideFuel.Shared;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace AdelaideFuel.Api
{
    public class SitesRoot
    {
        [JsonProperty("S")]
        public List<SiteDto> Sites { get; set; }
    }
}
