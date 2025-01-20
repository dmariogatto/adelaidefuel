namespace AdelaideFuel.Maui.Helpers
{
    public static class GlobalExceptionHandler
    {
        // We'll route all unhandled exceptions through this one event.
        public static event UnhandledExceptionEventHandler UnhandledException;

        static GlobalExceptionHandler()
        {
            AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
            {
                UnhandledException?.Invoke(sender, args);
            };

#if IOS
            // For iOS and Mac Catalyst
            // Exceptions will flow through AppDomain.CurrentDomain.UnhandledException,
            // but we need to set UnwindNativeCode to get it to work correctly.
            //
            // See: https://github.com/xamarin/xamarin-macios/issues/15252

            ObjCRuntime.Runtime.MarshalManagedException += (_, args) =>
            {
                args.ExceptionMode = ObjCRuntime.MarshalManagedExceptionMode.UnwindNativeCode;
            };
#elif ANDROID
            // For Android:
            // All exceptions will flow through Android.Runtime.AndroidEnvironment.UnhandledExceptionRaiser,
            // and NOT through AppDomain.CurrentDomain.UnhandledException

            Android.Runtime.AndroidEnvironment.UnhandledExceptionRaiser += (sender, args) =>
            {
                args.Handled = true;
                UnhandledException?.Invoke(sender, new UnhandledExceptionEventArgs(args.Exception, true));
            };

            Java.Lang.Thread.DefaultUncaughtExceptionHandler = new CustomUncaughtExceptionHandler(e =>
                UnhandledException?.Invoke(null, new UnhandledExceptionEventArgs(e, true)));
#endif
        }
    }

#if ANDROID
    public class CustomUncaughtExceptionHandler(Action<Java.Lang.Throwable> callback)
        : Java.Lang.Object, Java.Lang.Thread.IUncaughtExceptionHandler
    {
        public void UncaughtException(Java.Lang.Thread t, Java.Lang.Throwable e)
        {
            callback(e);
        }
    }
#endif
}
