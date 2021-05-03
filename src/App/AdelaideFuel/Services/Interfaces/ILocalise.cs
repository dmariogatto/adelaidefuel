using System.Globalization;

namespace AdelaideFuel.Services
{
    public interface ILocalise
    {
        CultureInfo GetCurrentCultureInfo();
        void SetLocale(CultureInfo ci);

        bool Is24Hour { get; }
    }

}