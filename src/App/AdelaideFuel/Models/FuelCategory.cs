using MvvmHelpers;

namespace AdelaideFuel.Models
{
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
                {
                    OnPropertyChanged(nameof(Description));
                }
            }
        }

        private int _upperBound;
        public int UpperBound
        {
            get => _upperBound;
            set
            {
                if (SetProperty(ref _upperBound, value))
                {
                    OnPropertyChanged(nameof(Description));
                }
            }
        }

        public string Description
        {
            get
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

                return result;
            }
        }
    }
}