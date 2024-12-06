using AdelaideFuel.Models;
using Android.Text.Format;
using System.Globalization;

namespace AdelaideFuel.Services
{
    public class LocalisePlatformService : ILocalise
    {
        private readonly Dictionary<string, CultureInfo> _cultures = new Dictionary<string, CultureInfo>(StringComparer.Ordinal);

        public void SetLocale(CultureInfo ci)
        {
            Thread.CurrentThread.CurrentCulture = ci;
            Thread.CurrentThread.CurrentUICulture = ci;
        }

        public CultureInfo GetCurrentCultureInfo()
        {
            var androidLanguage = Java.Util.Locale.Default.ToLanguageTag();

            if (!_cultures.TryGetValue(androidLanguage, out var ci))
            {
                var netLanguage = AndroidToDotnetLanguage(androidLanguage);

                try
                {
                    ci = new CultureInfo(netLanguage);
                }
                catch (CultureNotFoundException ex)
                {
                    System.Diagnostics.Debug.WriteLine(ex);
                }

                if (ci is null)
                {
                    // locale not valid .NET culture (eg. "en-ES" : English in Spain)
                    // fallback to first characters, in this case "en"
                    try
                    {
                        var fallback = ToDotnetFallbackLanguage(new PlatformCulture(netLanguage));
                        ci = new CultureInfo(fallback);
                    }
                    catch (CultureNotFoundException ex)
                    {
                        System.Diagnostics.Debug.WriteLine(ex);
                    }
                }

                _cultures.Add(androidLanguage, ci ??= new CultureInfo("en"));
            }

            return ci;
        }

        public bool Is24Hour
            => DateFormat.Is24HourFormat(Platform.AppContext);

        private string AndroidToDotnetLanguage(string androidLanguage)
        {
            var netLanguage = androidLanguage.Replace("_", "-");

            //certain languages need to be converted to CultureInfo equivalent
            switch (netLanguage)
            {
                case "ms-BN":   // "Malaysian (Brunei)" not supported .NET culture
                case "ms-MY":   // "Malaysian (Malaysia)" not supported .NET culture
                case "ms-SG":   // "Malaysian (Singapore)" not supported .NET culture
                    netLanguage = "ms"; // closest supported
                    break;
                case "in-ID":  // "Indonesian (Indonesia)" has different code in  .NET
                    netLanguage = "id-ID"; // correct code for .NET
                    break;
                case "gsw-CH":  // "Schwiizertüütsch (Swiss German)" not supported .NET culture
                    netLanguage = "de-CH"; // closest supported
                    break;
                    // add more application-specific cases here (if required)
                    // ONLY use cultures that have been tested and known to work
            }

            return netLanguage;
        }

        private string ToDotnetFallbackLanguage(PlatformCulture platCulture)
        {
            var netLanguage = platCulture.LanguageCode; // use the first part of the identifier (two chars, usually);

            switch (platCulture.LanguageCode)
            {
                case "gsw":
                    netLanguage = "de-CH"; // equivalent to German (Switzerland) for this app
                    break;
                    // add more application-specific cases here (if required)
                    // ONLY use cultures that have been tested and known to work
            }

            return netLanguage;
        }
    }
}