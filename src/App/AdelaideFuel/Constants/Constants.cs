using AdelaideFuel.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Xamarin.Essentials;
using Xamarin.Essentials.Interfaces;

namespace AdelaideFuel
{
    public static class Constants
    {
        static Constants()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resName = assembly.GetManifestResourceNames()
                ?.FirstOrDefault(r => r.EndsWith("settings.json", StringComparison.OrdinalIgnoreCase));

            using var file = assembly.GetManifestResourceStream(resName);
            using var sr = new StreamReader(file);
            using var jtr = new JsonTextReader(sr);

            var j = JsonSerializer.Create().Deserialize<JObject>(jtr);

            ApiUrlBase = j.Value<string>(nameof(ApiUrlBase));
            ApiKeyBrands = j.Value<string>(nameof(ApiKeyBrands));
            ApiKeyFuels = j.Value<string>(nameof(ApiKeyFuels));
            ApiKeySites = j.Value<string>(nameof(ApiKeySites));
            ApiKeySitePrices = j.Value<string>(nameof(ApiKeySitePrices));
            ApiKeyBrandImg = j.Value<string>(nameof(ApiKeyBrandImg));
            AppCenterAndroidSecret = j.Value<string>(nameof(AppCenterAndroidSecret));
            AppCenterIosSecret = j.Value<string>(nameof(AppCenterIosSecret));
            AdMobPublisherId = j.Value<string>(nameof(AdMobPublisherId));
            AdMobPricesAndroidUnitId = j.Value<string>(nameof(AdMobPricesAndroidUnitId));
            AdMobPricesIosUnitId = j.Value<string>(nameof(AdMobPricesIosUnitId));
            AdMobMapAndroidUnitId = j.Value<string>(nameof(AdMobMapAndroidUnitId));
            AdMobMapIosUnitId = j.Value<string>(nameof(AdMobMapIosUnitId));
        }

        public const string Email = "outtaapps@gmail.com";
        public const string AndroidId = "com.dgatto.adelaidefuel";
        public const string AppleId = "1565760343";

        public const string PriceErrorFormUrl = "https://forms.sa.gov.au/#/form/6029c6a9ad9c5a1dd463e6db";

        public const string AuthHeader = "x-functions-key";

        public static readonly TimeZoneInfo AdelaideTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Australia/Adelaide");
        public static DateTime AdelaideNow => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, AdelaideTimeZone);

        public static readonly Coords AdelaideCenter = new Coords(-34.9241446714455, 138.599550649524);
        public static readonly Coords SaCenter = new Coords(-30.058333, 135.763333, 1025 * 1000);

        public static readonly TimeSpan AdPoolTime = TimeSpan.FromMinutes(5);

        public static readonly string ApiUrlBase;

        public static readonly string ApiKeyBrands;
        public static readonly string ApiKeyFuels;
        public static readonly string ApiKeySites;
        public static readonly string ApiKeySitePrices;
        public static readonly string ApiKeyBrandImg;

        public static readonly string AppCenterAndroidSecret;
        public static readonly string AppCenterIosSecret;

        public static readonly string AdMobPublisherId;
        public static readonly string AdMobPricesAndroidUnitId;
        public static readonly string AdMobPricesIosUnitId;
        public static readonly string AdMobMapAndroidUnitId;
        public static readonly string AdMobMapIosUnitId;

        public static string AppId => ValueForPlatform(AndroidId, AppleId);

        public static string AppCenterSecret => ValueForPlatform(AppCenterAndroidSecret, AppCenterIosSecret);

        public static string AdMobPricesUnitId => AdUnitId(ValueForPlatform(AdMobPricesAndroidUnitId, AdMobPricesIosUnitId));
        public static string AdMobMapUnitId => AdUnitId(ValueForPlatform(AdMobMapAndroidUnitId, AdMobMapIosUnitId));

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
            => $"{AdMobPublisherId}/{adUnitId}";
    }
}