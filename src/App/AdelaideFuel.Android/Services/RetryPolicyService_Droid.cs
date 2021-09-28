using AdelaideFuel.Services;
using Android.Runtime;
using Polly;

namespace AdelaideFuel.Droid.Services
{
    [Preserve(AllMembers = true)]
    public class RetryPolicyService_Droid : IRetryPolicyService
    {
        public PolicyBuilder GetNativeNetRetryPolicy() =>
            Policy.Handle<Java.Net.SocketException>()
                  .Or<Java.Net.SocketException>()
                  .Or<Java.IO.IOException>();
    }
}