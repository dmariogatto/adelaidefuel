using MvvmHelpers;
using System.Collections.Generic;

namespace AdelaideFuel.Models
{
    public class SiteFuelPriceItemGroup : Grouping<UserFuel, SiteFuelPriceItem>
    {
        public SiteFuelPriceItemGroup(UserFuel key, IEnumerable<SiteFuelPriceItem> items) : base(key, items)
        {
        }
    }
}