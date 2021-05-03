using System;

namespace AdelaideFuel.Models
{
    public interface IMapPin
    {
        string Label { get; }
        string Description { get; }

        Coords Position { get; }

        int ZIndex { get; }
    }
}