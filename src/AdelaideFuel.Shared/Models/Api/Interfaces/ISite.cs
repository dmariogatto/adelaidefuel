using System;

namespace AdelaideFuel.Shared
{
    public interface ISite
    {
        int SiteId { get; set; }

        string Address { get; set; }

        string Name { get; set; }

        int BrandId { get; set; }

        string Postcode { get; set; }

        int GeographicRegionLevel1 { get; set; }

        int GeographicRegionLevel2 { get; set; }

        int GeographicRegionLevel3 { get; set; }

        int GeographicRegionLevel4 { get; set; }

        int GeographicRegionLevel5 { get; set; }

        double Latitude { get; set; }

        double Longitude { get; set; }

        DateTime LastModifiedUtc { get; set; }

        string GooglePlaceId { get; set; }

        string MondayOpen { get; set; }

        string MondayClose { get; set; }

        string TuesdayOpen { get; set; }

        string TuesdayClose { get; set; }

        string WednesdayOpen { get; set; }

        string WednesdayClose { get; set; }

        string ThursdayOpen { get; set; }

        string ThursdayClose { get; set; }

        string FridayOpen { get; set; }

        string FridayClose { get; set; }

        string SaturdayOpen { get; set; }

        string SaturdayClose { get; set; }

        string SundayOpen { get; set; }

        string SundayClose { get; set; }
    }
}
