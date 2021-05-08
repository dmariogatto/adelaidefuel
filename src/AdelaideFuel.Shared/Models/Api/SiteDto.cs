using Newtonsoft.Json;
using System;

namespace AdelaideFuel.Shared
{
    public class SiteDto : ISite
    {
        [JsonProperty("S")]
        public int SiteId { get; set; }

        [JsonProperty("A")]
        public string Address { get; set; }

        [JsonProperty("N")]
        public string Name { get; set; }

        [JsonProperty("B")]
        public int BrandId { get; set; }

        [JsonProperty("P")]
        public string Postcode { get; set; }

        [JsonProperty("G1")]
        public int GeographicRegionLevel1 { get; set; }

        [JsonProperty("G2")]
        public int GeographicRegionLevel2 { get; set; }

        [JsonProperty("G3")]
        public int GeographicRegionLevel3 { get; set; }

        [JsonProperty("G4")]
        public int GeographicRegionLevel4 { get; set; }

        [JsonProperty("G5")]
        public int GeographicRegionLevel5 { get; set; }

        [JsonProperty("Lat")]
        public double Latitude { get; set; }

        [JsonProperty("Lng")]
        public double Longitude { get; set; }

        [JsonProperty("M")]
        public DateTime LastModifiedUtc { get; set; }

        [JsonProperty("GPI")]
        public string GooglePlaceId { get; set; }

        [JsonProperty("MO")]
        public string MondayOpen { get; set; }

        [JsonProperty("MC")]
        public string MondayClose { get; set; }

        [JsonProperty("TO")]
        public string TuesdayOpen { get; set; }

        [JsonProperty("TC")]
        public string TuesdayClose { get; set; }

        [JsonProperty("WO")]
        public string WednesdayOpen { get; set; }

        [JsonProperty("WC")]
        public string WednesdayClose { get; set; }

        [JsonProperty("THO")]
        public string ThursdayOpen { get; set; }

        [JsonProperty("THC")]
        public string ThursdayClose { get; set; }

        [JsonProperty("FO")]
        public string FridayOpen { get; set; }

        [JsonProperty("FC")]
        public string FridayClose { get; set; }

        [JsonProperty("SO")]
        public string SaturdayOpen { get; set; }

        [JsonProperty("SC")]
        public string SaturdayClose { get; set; }

        [JsonProperty("SUO")]
        public string SundayOpen { get; set; }

        [JsonProperty("SUC")]
        public string SundayClose { get; set; }
    }
}