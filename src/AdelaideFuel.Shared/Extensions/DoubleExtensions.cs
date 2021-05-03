using System;

namespace AdelaideFuel.Shared
{
    public static class DoubleExtensions
    {
        public static bool FuzzyEquals(this double initialValue, double value, double maximumDifferenceAllowed = 0.000001)
        {
            // Handle comparisons of floating point values that may not be exactly the same
            return Math.Abs(initialValue - value) < maximumDifferenceAllowed;
        }
    }
}