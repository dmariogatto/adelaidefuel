using AdelaideFuel.Services;

namespace AdelaideFuel.Maui.Extensions;

public static class EnvironmentServiceExtensions
{
    public static bool IsMacCatalyst(this IEnvironmentService _)
    {
        try
        {
            var selector = new ObjCRuntime.Selector("isMacCatalystApp");
            var processInfo = Foundation.NSProcessInfo.ProcessInfo;

            if (processInfo.RespondsToSelector(selector))
            {
                var ptr = NativeMethods.objc_msgSend(processInfo.Handle, selector.Handle);
                return ptr != IntPtr.Zero;
            }
            
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex);
        }

        return false;
    }

    private static class NativeMethods
    {
        [System.Runtime.InteropServices.DllImport("/usr/lib/libobjc.dylib", EntryPoint = "objc_msgSend")]
        internal static extern IntPtr objc_msgSend(IntPtr receiver, IntPtr selector);
    }
}