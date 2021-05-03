using AdelaideFuel.Services;
using AdelaideFuel.UI.Services;
using Android.App;
using Android.Runtime;
using System;

[assembly: MetaData("com.google.android.gms.version", Value = "@integer/google_play_services_version")]
namespace AdelaideFuel.Droid
{
#if DEBUG
    [Application(Debuggable = true)]
#else
[Application(Debuggable = false)]
#endif
    public class MainApplication : Application
    {
        public MainApplication(IntPtr handle, JniHandleOwnership transer)
            : base(handle, transer)
        {
        }

        public override void OnCreate()
        {
            base.OnCreate();

            IoC.RegisterSingleton<ILocalise, LocaliseService_Droid>();
            IoC.RegisterSingleton<IEnvironmentService, EnvironmentService_Droid>();
            IoC.RegisterSingleton<IRendererService, RendererService_Droid>();
            IoC.RegisterSingleton<IRetryPolicyService, RetryPolicyService_Droid>();
        }
    }
}