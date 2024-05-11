using AdelaideFuel.Shared;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace AdelaideFuel.Api
{
    public class SitesRoot
    {
        [JsonPropertyName("S")]
        public List<SiteDto> Sites { get; set; }
    }
}