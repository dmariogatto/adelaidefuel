using AdelaideFuel.Shared;
using Microsoft.Azure.Cosmos.Table;
using System;
using System.Globalization;

namespace AdelaideFuel.TableStore.Entities
{
    public class SiteEntity : TableEntity, IEntity, ISite
    {
        public SiteEntity() { }

        public SiteEntity(ISite site)
        {
            BrandId = site.BrandId;
            SiteId = site.SiteId;

            Address = site.Address?.Trim();
            Name = site.Name?.Trim();
            Postcode = site.Postcode?.Trim();

            GeographicRegionLevel1 = site.GeographicRegionLevel1;
            GeographicRegionLevel2 = site.GeographicRegionLevel2;
            GeographicRegionLevel3 = site.GeographicRegionLevel3;
            GeographicRegionLevel4 = site.GeographicRegionLevel4;
            GeographicRegionLevel5 = site.GeographicRegionLevel5;

            Latitude = site.Latitude;
            Longitude = site.Longitude;

            LastModifiedUtc = site.LastModifiedUtc;

            GooglePlaceId = site.GooglePlaceId;

            MondayOpen = site.MondayOpen;
            MondayClose = site.MondayClose;
            TuesdayOpen = site.TuesdayOpen;
            TuesdayClose = site.TuesdayClose;
            WednesdayOpen = site.WednesdayOpen;
            WednesdayClose = site.WednesdayClose;
            ThursdayOpen = site.ThursdayOpen;
            ThursdayClose = site.ThursdayClose;
            FridayOpen = site.FridayOpen;
            FridayClose = site.FridayClose;
            SaturdayOpen = site.SaturdayOpen;
            SaturdayClose = site.SaturdayClose;
            SundayOpen = site.SundayOpen;
            SundayClose = site.SundayClose;
        }

        public int BrandId
        {
            get => int.TryParse(PartitionKey, out var id) ? id : -1;
            set => PartitionKey = value.ToString(CultureInfo.InvariantCulture);
        }

        public int SiteId
        {
            get => int.TryParse(RowKey, out var id) ? id : -1;
            set => RowKey = value.ToString(CultureInfo.InvariantCulture);
        }

        public string Address { get; set; }
        public string Name { get; set; }
        public string Postcode { get; set; }

        public int GeographicRegionLevel1 { get; set; }
        public int GeographicRegionLevel2 { get; set; }
        public int GeographicRegionLevel3 { get; set; }
        public int GeographicRegionLevel4 { get; set; }
        public int GeographicRegionLevel5 { get; set; }

        public double Latitude { get; set; }
        public double Longitude { get; set; }

        public DateTime LastModifiedUtc { get; set; }

        public string GooglePlaceId { get; set; }

        public string MondayOpen { get; set; }
        public string MondayClose { get; set; }
        public string TuesdayOpen { get; set; }
        public string TuesdayClose { get; set; }
        public string WednesdayOpen { get; set; }
        public string WednesdayClose { get; set; }
        public string ThursdayOpen { get; set; }
        public string ThursdayClose { get; set; }
        public string FridayOpen { get; set; }
        public string FridayClose { get; set; }
        public string SaturdayOpen { get; set; }
        public string SaturdayClose { get; set; }
        public string SundayOpen { get; set; }
        public string SundayClose { get; set; }

        public bool IsActive { get; set; }

        public bool IsDifferent(IEntity entity)
        {
            if (entity is SiteEntity other && Equals(other))
            {
                return
                    Address != other.Address ||
                    Name != other.Name ||
                    Postcode != other.Postcode ||
                    GeographicRegionLevel1 != other.GeographicRegionLevel1 ||
                    GeographicRegionLevel2 != other.GeographicRegionLevel2 ||
                    GeographicRegionLevel3 != other.GeographicRegionLevel3 ||
                    GeographicRegionLevel4 != other.GeographicRegionLevel4 ||
                    GeographicRegionLevel5 != other.GeographicRegionLevel5 ||
                    !Latitude.FuzzyEquals(other.Latitude) ||
                    !Longitude.FuzzyEquals(other.Longitude) ||
                    LastModifiedUtc != other.LastModifiedUtc ||
                    GooglePlaceId != other.GooglePlaceId ||
                    MondayOpen != other.MondayOpen ||
                    MondayClose != other.MondayClose ||
                    TuesdayOpen != other.TuesdayOpen ||
                    TuesdayClose != other.TuesdayClose ||
                    WednesdayOpen != other.WednesdayOpen ||
                    WednesdayClose != other.WednesdayClose ||
                    ThursdayOpen != other.ThursdayOpen ||
                    ThursdayClose != other.ThursdayClose ||
                    FridayOpen != other.FridayOpen ||
                    FridayClose != other.FridayClose ||
                    SaturdayOpen != other.SaturdayOpen ||
                    SaturdayClose != other.SaturdayClose ||
                    SundayOpen != other.SundayOpen ||
                    SundayClose != other.SundayClose ||
                    IsActive != other.IsActive;
            }

            return true;
        }

        public SiteDto ToSite() => new SiteDto()
        {
            BrandId = BrandId,
            SiteId = SiteId,
            Address = Address,
            Name = Name,
            Postcode = Postcode,
            GeographicRegionLevel1 = GeographicRegionLevel1,
            GeographicRegionLevel2 = GeographicRegionLevel2,
            GeographicRegionLevel3 = GeographicRegionLevel3,
            GeographicRegionLevel4 = GeographicRegionLevel4,
            GeographicRegionLevel5 = GeographicRegionLevel5,
            Latitude = Latitude,
            Longitude = Longitude,
            LastModifiedUtc = LastModifiedUtc,
            GooglePlaceId = GooglePlaceId,
            MondayOpen = MondayOpen,
            MondayClose = MondayClose,
            TuesdayOpen = TuesdayOpen,
            TuesdayClose = TuesdayClose,
            WednesdayOpen = WednesdayOpen,
            WednesdayClose = WednesdayClose,
            ThursdayOpen = ThursdayOpen,
            ThursdayClose = ThursdayClose,
            FridayOpen = FridayOpen,
            FridayClose = FridayClose,
            SaturdayOpen = SaturdayOpen,
            SaturdayClose = SaturdayClose,
            SundayOpen = SundayOpen,
            SundayClose = SundayClose,
        };

        public override string ToString()
            => Name;

        public override bool Equals(object obj)
        {
            return obj is SiteEntity other &&
                   PartitionKey == other.PartitionKey &&
                   RowKey == other.RowKey;
        }

        public override int GetHashCode() => (PartitionKey, RowKey).GetHashCode();
    }
}