using System;

namespace AdelaideFuel.Shared
{
    public interface IFuel
    {
        int FuelId { get; set; }

        string Name { get; set; }
    }
}