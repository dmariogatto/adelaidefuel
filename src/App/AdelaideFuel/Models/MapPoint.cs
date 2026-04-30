using System;

namespace AdelaideFuel.Models
{
    public readonly struct MapPoint
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
            Y == point.Y;
        public override int GetHashCode() => HashCode.Combine(X, Y);
        public static bool operator ==(MapPoint left, MapPoint right) => left.Equals(right);
        public static bool operator !=(MapPoint left, MapPoint right) => !(left == right);
    }
}