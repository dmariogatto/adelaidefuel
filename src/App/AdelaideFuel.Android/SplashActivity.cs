using System;

using Android.App;
using Android.OS;
using Android.Content;
using AndroidX.AppCompat.App;

namespace AdelaideFuel.Droid
{
    [Activity(
        Label = "ShouldIFuel",
        Icon = "@mipmap/icon",
        RoundIcon = "@mipmap/icon_round",
        Theme = "@style/SplashTheme",
        NoHistory = true,
        MainLauncher = true)]
    public class SplashActivity : AppCompatActivity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            var intent = new Intent(this, typeof(MainActivity));
            StartActivity(intent);
            Finish();
        }
    }
}