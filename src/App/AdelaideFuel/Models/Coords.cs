using System;

namespace AdelaideFuel.Models
{
    public struct Coords
    {
        public Coords(double latitude, double longitude, double radiusMetres = 0, double bearing = 0)
        {
            Latitude = latitude;
            Longitude = longitude;
            RadiusMetres = radiusMetres;
            Bearing = bearing;
        }

        public double Latitude { get; }
        public double Longitude { get; }

        public double RadiusMetres { get; }
        public double Bearing { get; }

        public Coords WithRadius(double metres) =>
            new Coords(Latitude, Longitude, metres, Bearing);
        public Coords WithBearing(double bearing) =>
            new Coords(Latitude, Longitude, RadiusMetres, bearing);

        public override bool Equals(object obj) => obj is Coords position &&
            Latitude == position.Latitude &&
            Longitude == position.Longitude &&
            RadiusMetres == position.RadiusMetres &&
            Bearing == position.Bearing;

        public override int GetHashCode() => (Latitude, Longitude).GetHashCode();
        public static bool operator ==(Coords left, Coords right) => left.Equals(right);
        public static bool operator !=(Coords left, Coords right) => !(left == right);
    }
}