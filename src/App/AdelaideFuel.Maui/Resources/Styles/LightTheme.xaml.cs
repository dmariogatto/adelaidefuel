namespace AdelaideFuel.Maui.Styles
{
    public partial class LightTheme : ResourceDictionary
    {
        public LightTheme()
        {
            InitializeComponent();

            if (DeviceInfo.Platform == DevicePlatform.Android)
            {
                Add(Styles.Keys.NavigationBarColor, Color.FromRgb(46, 125, 50));
                Add(Styles.Keys.NavigationBarTextColor, Colors.White);
            }
            else if (DeviceInfo.Platform == DevicePlatform.iOS)
            {
                Add(Styles.Keys.NavigationBarColor, Color.FromRgb(248, 248, 248));
                Add(Styles.Keys.NavigationBarTextColor, Colors.Black);
            }
            else
            {
                Add(Styles.Keys.NavigationBarColor, Color.FromRgb(46, 125, 50));
                Add(Styles.Keys.NavigationBarTextColor, Colors.White);
            }
        }
    }
}