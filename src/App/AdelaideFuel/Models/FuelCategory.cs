using CommunityToolkit.Mvvm.ComponentModel;
using System.Diagnostics;

namespace AdelaideFuel.Models
{
    [DebuggerDisplay("{PriceCategory}, {Description}")]
    public class FuelCategory : ObservableObject
    {
        public FuelCategory(PriceCategory priceCategory)
        {
            PriceCategory = priceCategory;
        }

        public PriceCategory PriceCategory { get; private set; }

        private int _lowerBound;
        public int LowerBound
        {
            get => _lowerBound;
            set
            {
                if (SetProperty(ref _lowerBound, value))
                    UpdateDescription();
            }
        }

        private int _upperBound;
        public int UpperBound
        {
            get => _upperBound;
            set
            {
                if (SetProperty(ref _upperBound, value))
                    UpdateDescription();
            }
        }

        private string _description;
        public string Description
        {
            get => _description;
            private set => SetProperty(ref _description, value);
        }

        private void UpdateDescription()
        {
            var result = string.Empty;

            if (LowerBound == UpperBound)
                result = LowerBound.ToString();
            else if (LowerBound > 0 && UpperBound > 0)
                result = string.Format("{0} - {1}", LowerBound, UpperBound);
            else if (LowerBound > 0)
                result = string.Format("> {0}", LowerBound);
            else if (UpperBound > 0)
                result = string.Format("< {0}", UpperBound);

            Description = result;
        }
    }
}