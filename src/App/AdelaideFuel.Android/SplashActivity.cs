using Android.App;
using Android.Content;
using Android.OS;
using AndroidX.AppCompat.App;

namespace AdelaideFuel.Droid
{
    [Activity(
        Label = "ShouldIFuel",
        Icon = "@mipmap/icon",
        RoundIcon = "@mipmap/icon_round",
        Theme = "@style/SplashTheme",
        MainLauncher = true,
        NoHistory = true)]
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