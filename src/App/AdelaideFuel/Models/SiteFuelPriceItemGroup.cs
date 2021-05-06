using MvvmHelpers;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace AdelaideFuel.Models
{
    public class SiteFuelPriceItemGroup : Grouping<UserFuel, SiteFuelPriceItem>
    {
        public SiteFuelPriceItemGroup(UserFuel key, IEnumerable<SiteFuelPriceItem> items) : base(key, items)
        {
        }

        public bool HasPrices => Items.Any(i => !i.IsClear);
        public void RefreshHasPrices() => OnPropertyChanged(new PropertyChangedEventArgs(nameof(HasPrices)));
    }
}