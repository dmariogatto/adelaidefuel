using AdelaideFuel.UI;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Runtime;
using System.Collections.Generic;
using Xamarin.Forms;

[assembly: ResolutionGroupName("AdelaideFuel.Effects")]

namespace AdelaideFuel.Droid
{
    [Activity(
        Label = "ShouldIFuel",
        Icon = "@mipmap/icon",
        RoundIcon = "@mipmap/icon_round",
        Theme = "@style/MainTheme",
        ScreenOrientation = ScreenOrientation.Portrait,
        ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation)]
    public class MainActivity : global::Xamarin.Forms.Platform.Android.FormsAppCompatActivity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            TabLayoutResource = Resource.Layout.Tabbar;
            ToolbarResource = Resource.Layout.Toolbar;

            base.OnCreate(savedInstanceState);

            FFImageLoading.Forms.Platform.CachedImageRenderer.Init(true);
            Sharpnado.HorizontalListView.Droid.SharpnadoInitializer.Initialize();
            Acr.UserDialogs.UserDialogs.Init(this);
            AiForms.Renderers.Droid.SettingsViewInit.Init();
            Xamarin.FormsBetterMaps.Init(this, savedInstanceState, new Xamarin.Forms.BetterMaps.MapCache());
            Android.Gms.Ads.MobileAds.Initialize(ApplicationContext);

            Xamarin.Essentials.Platform.Init(this, savedInstanceState);
            global::Xamarin.Forms.Forms.Init(this, savedInstanceState);

#if DEBUG
            var testDevices = new List<string>()
            {
                Android.Gms.Ads.AdRequest.DeviceIdEmulator
            };

            Android.Gms.Ads.MobileAds.RequestConfiguration = new Android.Gms.Ads.RequestConfiguration.Builder()
                .SetTestDeviceIds(testDevices)
                .Build();
#endif

            Xamarin.FormsBetterMaps.SetLightThemeAsset("map.style.light.json");
            Xamarin.FormsBetterMaps.SetDarkThemeAsset("map.style.dark.json");

            var formsApp = new App();
            LoadApplication(formsApp);
        }

        protected override void OnActivityResult(int requestCode, Result resultCode, Intent data)
        {
            base.OnActivityResult(requestCode, resultCode, data);
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Android.Content.PM.Permission[] grantResults)
        {
            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }
    }
}