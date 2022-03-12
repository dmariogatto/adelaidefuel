using AdelaideFuel.Models;
using Xamarin.Essentials;

namespace AdelaideFuel
{
    public static class CoordsExtensions
    {
        public static Location ToLocation(this Coords coords)
            => new Location(coords.Latitude, coords.Longitude);
    }
}