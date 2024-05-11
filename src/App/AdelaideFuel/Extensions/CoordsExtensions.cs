using AdelaideFuel.Models;
using Microsoft.Maui.Devices.Sensors;

namespace AdelaideFuel
{
    public static class CoordsExtensions
    {
        public static Location ToLocation(this Coords coords)
            => new Location(coords.Latitude, coords.Longitude);
    }
}