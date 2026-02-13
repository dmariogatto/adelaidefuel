using AdelaideFuel.Services;
using AdelaideFuel.ViewModels;

namespace AdelaideFuel.Maui.Views
{
    public partial class SettingsTab : BaseTabView<SettingsViewModel>
    {
        public SettingsTab() : base()
        {
            InitializeComponent();

            if (DeviceInfo.Current.Platform == DevicePlatform.iOS && !IoC.Resolve<IEnvironmentService>().NativeDarkMode)
            {
                // hide on iOS 12 and below
                AccessibilitySection.Remove(ThemePicker);
            }
        }

        private void BuildOnTapped(object sender, EventArgs e)
        {
            ViewModel.BuildTappedCommand.Execute(null);
        }

        private void OnAdInspectorClicked(object sender, EventArgs e)
        {
#if ANDROID
            global::Android.Gms.Ads.MobileAds.OpenAdInspector(Platform.AppContext, new AdInspectorListener());
#elif IOS
            Cats.Google.MobileAds.MobileAds.SharedInstance.PresentAdInspectorFromViewController(
                Platform.GetCurrentUIViewController(),
                error =>
                {
                    if (!string.IsNullOrEmpty(error?.LocalizedDescription))
                        System.Diagnostics.Debug.WriteLine(error.LocalizedDescription);
                });
#endif
        }

    }

#if ANDROID
    public class AdInspectorListener : Java.Lang.Object, global::Android.Gms.Ads.IOnAdInspectorClosedListener
    {
        public AdInspectorListener(IntPtr handle, global::Android.Runtime.JniHandleOwnership transfer) : base(handle, transfer) { }
        public AdInspectorListener() { }

        public void OnAdInspectorClosed(global::Android.Gms.Ads.AdInspectorError p0)
        {
            if (!string.IsNullOrEmpty(p0?.Message))
                System.Diagnostics.Debug.WriteLine(p0.Message);
        }
    }
#endif
}