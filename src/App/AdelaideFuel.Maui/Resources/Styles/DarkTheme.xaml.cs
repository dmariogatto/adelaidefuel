namespace AdelaideFuel.Maui.Styles
{
    public partial class DarkTheme : ResourceDictionary
    {
        public DarkTheme()
        {
            InitializeComponent();

            if (DeviceInfo.Platform == DevicePlatform.Android)
            {
                Add(Styles.Keys.NavigationBarColor, Color.FromRgb(24, 24, 24));
                Add(Styles.Keys.NavigationBarTextColor, Colors.White);
            }
            else if (DeviceInfo.Platform == DevicePlatform.iOS)
            {
                Add(Styles.Keys.NavigationBarColor, Colors.Black);
                Add(Styles.Keys.NavigationBarTextColor, Colors.White);
            }
            else
            {
                Add(Styles.Keys.NavigationBarColor, Colors.Black);
                Add(Styles.Keys.NavigationBarTextColor, Colors.White);
            }
        }
    }
}