using System;

namespace AdelaideFuel.Shared
{
    public interface ISitePrice
    {
        int SiteId { get; set; }

        int FuelId { get; set; }

        string CollectionMethod { get; set; }

        DateTime TransactionDateUtc { get; set; }

        double Price { get; set; }
    }
}