using System;

namespace AdelaideFuel.Models
{
    public struct MapPoint
    {
        public MapPoint(double x, double y)
        {
            X = x;
            Y = y;
        }

        public double X { get; }
        public double Y { get; }

        public override bool Equals(object obj) => obj is MapPoint point &&
            X == point.X &&
            X == point.X;
        public override int GetHashCode() => (X, X).GetHashCode();
        public static bool operator ==(MapPoint left, MapPoint right) => left.Equals(right);
        public static bool operator !=(MapPoint left, MapPoint right) => !(left == right);
    }
}