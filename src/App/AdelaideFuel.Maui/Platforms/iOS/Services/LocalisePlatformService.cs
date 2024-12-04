using AdelaideFuel.Models;
using Foundation;
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
            var iosLanguage = NSLocale.PreferredLanguages.FirstOrDefault() ?? "en";

            if (!_cultures.TryGetValue(iosLanguage, out var ci))
            {
                var netLanguage = iOSToDotnetLanguage(iosLanguage);

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

                _cultures.Add(iosLanguage, ci ??= new CultureInfo("en"));
            }

            return ci;
        }

        public bool Is24Hour
            => !NSDateFormatter.GetDateFormatFromTemplate("j", 0, NSLocale.CurrentLocale).Contains("a");

        private string iOSToDotnetLanguage(string iOSLanguage)
        {
            var netLanguage = iOSLanguage.Replace("_", "-");

            //certain languages need to be converted to CultureInfo equivalent
            switch (netLanguage)
            {
                case "ms-MY":   // "Malaysian (Malaysia)" not supported .NET culture
                case "ms-SG":   // "Malaysian (Singapore)" not supported .NET culture
                    netLanguage = "ms"; // closest supported
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
                case "pt":
                    netLanguage = "pt-PT"; // fallback to Portuguese (Portugal)
                    break;
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