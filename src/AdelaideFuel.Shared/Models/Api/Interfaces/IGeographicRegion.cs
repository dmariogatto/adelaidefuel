using System;

namespace AdelaideFuel.Shared
{
    public interface IGeographicRegion
    {
        int GeoRegionLevel { get; set; }

        int GeoRegionId { get; set; }

        string Name { get; set; }

        string Abbrev { get; set; }

        int? GeoRegionParentId { get; set; }
    }
}