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
        Theme = "@style/SplashTheme",
        LaunchMode = LaunchMode.SingleTask,
        ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation,
        Exported = false)]
    [IntentFilter(
        new[] { Xamarin.Essentials.Platform.Intent.ActionAppAction },
        Categories = new[] { Intent.CategoryDefault })]
    public class MainActivity : global::Xamarin.Forms.Platform.Android.FormsAppCompatActivity
    {
        private static App FormsApp;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            TabLayoutResource = Resource.Layout.Tabbar;
            ToolbarResource = Resource.Layout.Toolbar;

            base.OnCreate(savedInstanceState);

            FFImageLoading.Forms.Platform.CachedImageRenderer.Init(true);
            Sharpnado.CollectionView.Droid.Initializer.Initialize();
            Acr.UserDialogs.UserDialogs.Init(this);
            AiForms.Renderers.Droid.SettingsViewInit.Init();
            Xamarin.FormsBetterMaps.Init(this, savedInstanceState, new Xamarin.Forms.BetterMaps.MapCache());
            Android.Gms.Ads.MobileAds.Initialize(ApplicationContext);

            Xamarin.Essentials.Platform.Init(this, savedInstanceState);
            global::Xamarin.Forms.Forms.Init(this, savedInstanceState);

            if (Device.Idiom == TargetIdiom.Phone)
                RequestedOrientation = ScreenOrientation.UserPortrait;

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

            LoadApplication(FormsApp ??= new App());

            SetTheme(Resource.Style.MainTheme);
        }

        protected override void OnResume()
        {
            base.OnResume();

            Xamarin.Essentials.Platform.OnResume(this);
        }

        protected override void OnNewIntent(Intent intent)
        {
            base.OnNewIntent(intent);

            Xamarin.Essentials.Platform.OnNewIntent(intent);
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Permission[] grantResults)
        {
            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }
    }
}