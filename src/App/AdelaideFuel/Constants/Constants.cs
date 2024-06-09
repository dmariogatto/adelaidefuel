using AdelaideFuel.Models;
using Microsoft.Maui.Devices;
using System;
using System.Linq;
using System.Text.Json;

namespace AdelaideFuel
{
    public static class Constants
    {
        static Constants()
        {
            var assembly = typeof(Constants).Assembly;
            var resName = assembly.GetManifestResourceNames()
                ?.FirstOrDefault(r => r.EndsWith("settings.json", StringComparison.OrdinalIgnoreCase));

            using var stream = assembly.GetManifestResourceStream(resName);
            Values = JsonSerializer.Deserialize<Settings>(stream);
        }

        public const string Email = "outtaapps@gmail.com";

        private const string AndroidId = "com.dgatto.adelaidefuel";
        private const string AppleId = "1565760343";

        public const string FuelInfoForMotoristsUrl = "https://www.sa.gov.au/topics/driving-and-transport/fuel-pricing/fuel-pricing-information-for-motorists";
        public const string PriceErrorFormUrl = "https://forms.sa.gov.au/#/form/6029c6a9ad9c5a1dd463e6db";

        public const string PrivacyUrl = "https://dgatto.com/privacy/";
        public const string TermsOfUseUrl = "https://dgatto.com/terms-of-use/";

        public const string AuthHeader = "x-functions-key";

        public static double OutOfStockPriceInCents = 999.9;

        public static readonly Coords AdelaideCenter = new Coords(-34.9241446714455, 138.599550649524);
        public static readonly Coords SaCenter = new Coords(-30.058333, 135.763333, 1025 * 1000);

        public static readonly TimeSpan AdPoolTime = TimeSpan.FromMinutes(5);

        public static string ApiUrlBase => Values.ApiUrlBase;
        public static string ApiUrlIapBase => Values.ApiUrlIapBase;
        public static string SubscriptionProductId => Values.SubscriptionProductId;

        public static string ApiKeyBrands => Values.ApiKeyBrands;
        public static string ApiKeyFuels => Values.ApiKeyFuels;
        public static string ApiKeySites => Values.ApiKeySites;
        public static string ApiKeySitePrices => Values.ApiKeySitePrices;
        public static string ApiKeyBrandImg => Values.ApiKeyBrandImg;

        public static string AppId => ValueForPlatform(AndroidId, AppleId);
        public static string SharedGroupName => ValueForPlatform(AppId, string.Empty);
        public static string SentryDsn => Values.SentryDsn;

        public static string AdMobPricesUnitId => AdUnitId(ValueForPlatform(Values.AdMobPricesAndroidUnitId, Values.AdMobPricesIosUnitId));
        public static string AdMobMapUnitId => AdUnitId(ValueForPlatform(Values.AdMobMapAndroidUnitId, Values.AdMobMapIosUnitId));

        private static Lazy<DevicePlatform> Platform => new Lazy<DevicePlatform>(() => IoC.Resolve<IDeviceInfo>().Platform);
        private static string ValueForPlatform(string android, string ios)
        {
            if (Platform.Value == DevicePlatform.Android)
                return android;
            if (Platform.Value == DevicePlatform.iOS)
                return ios;

            return string.Empty;
        }

        private static string AdUnitId(string adUnitId)
            => $"{Values.AdMobPublisherId}/{adUnitId}";

        private static readonly Settings Values;

        private record Settings
        {
            public string ApiUrlBase { get; init; }
            public string ApiUrlIapBase { get; init; }
            public string SubscriptionProductId { get; init; }
            public string ApiKeyBrands { get; init; }
            public string ApiKeyFuels { get; init; }
            public string ApiKeySites { get; init; }
            public string ApiKeySitePrices { get; init; }
            public string ApiKeyBrandImg { get; init; }
            public string SentryDsn { get; init; }
            public string AdMobPublisherId { get; init; }
            public string AdMobPricesAndroidUnitId { get; init; }
            public string AdMobPricesIosUnitId { get; init; }
            public string AdMobMapAndroidUnitId { get; init; }
            public string AdMobMapIosUnitId { get; init; }
        }
    }
}