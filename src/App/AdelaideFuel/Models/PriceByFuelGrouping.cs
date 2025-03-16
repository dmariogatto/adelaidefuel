using System.Collections.Generic;

namespace AdelaideFuel.Models
{
    public class PriceByFuelGrouping : Grouping<UserFuel, SiteFuelPrice>
    {
        public PriceByFuelGrouping(UserFuel key, IEnumerable<SiteFuelPrice> items) : base(key, items)
        {
        }
    }
}