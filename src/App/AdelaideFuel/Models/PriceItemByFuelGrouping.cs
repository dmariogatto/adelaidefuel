using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace AdelaideFuel.Models
{
    public class PriceItemByFuelGrouping : Grouping<UserFuel, SiteFuelPriceItem>
    {
        public PriceItemByFuelGrouping(UserFuel key, IEnumerable<SiteFuelPriceItem> items) : base(key, items)
        {
        }

        public bool HasPrices => Items.Any(i => !i.IsClear);
        public void RefreshHasPrices() => OnPropertyChanged(new PropertyChangedEventArgs(nameof(HasPrices)));
    }
}