using System;
using System.Text.Json.Serialization;

namespace AdelaideFuel.Shared
{
    public class SiteDto : ISite
    {
        [JsonPropertyName("S")]
        public int SiteId { get; set; }

        [JsonPropertyName("A")]
        public string Address { get; set; }

        [JsonPropertyName("N")]
        public string Name { get; set; }

        [JsonPropertyName("B")]
        public int BrandId { get; set; }

        [JsonPropertyName("P")]
        public string Postcode { get; set; }

        [JsonPropertyName("G1")]
        public int GeographicRegionLevel1 { get; set; }

        [JsonPropertyName("G2")]
        public int GeographicRegionLevel2 { get; set; }

        [JsonPropertyName("G3")]
        public int GeographicRegionLevel3 { get; set; }

        [JsonPropertyName("G4")]
        public int GeographicRegionLevel4 { get; set; }

        [JsonPropertyName("G5")]
        public int GeographicRegionLevel5 { get; set; }

        [JsonPropertyName("Lat")]
        public double Latitude { get; set; }

        [JsonPropertyName("Lng")]
        public double Longitude { get; set; }

        [JsonPropertyName("M"), JsonConverter(typeof(DateUtcJsonConverter))]
        public DateTime LastModifiedUtc { get; set; }

        [JsonPropertyName("GPI")]
        public string GooglePlaceId { get; set; }

        [JsonPropertyName("MO")]
        public string MondayOpen { get; set; }

        [JsonPropertyName("MC")]
        public string MondayClose { get; set; }

        [JsonPropertyName("TO")]
        public string TuesdayOpen { get; set; }

        [JsonPropertyName("TC")]
        public string TuesdayClose { get; set; }

        [JsonPropertyName("WO")]
        public string WednesdayOpen { get; set; }

        [JsonPropertyName("WC")]
        public string WednesdayClose { get; set; }

        [JsonPropertyName("THO")]
        public string ThursdayOpen { get; set; }

        [JsonPropertyName("THC")]
        public string ThursdayClose { get; set; }

        [JsonPropertyName("FO")]
        public string FridayOpen { get; set; }

        [JsonPropertyName("FC")]
        public string FridayClose { get; set; }

        [JsonPropertyName("SO")]
        public string SaturdayOpen { get; set; }

        [JsonPropertyName("SC")]
        public string SaturdayClose { get; set; }

        [JsonPropertyName("SUO")]
        public string SundayOpen { get; set; }

        [JsonPropertyName("SUC")]
        public string SundayClose { get; set; }
    }
}