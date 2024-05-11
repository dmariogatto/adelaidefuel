using Polly;

namespace AdelaideFuel.Services
{
    public class RetryPolicyPlatformService : IRetryPolicyService
    {
        public PolicyBuilder GetNativeNetRetryPolicy() =>
            Policy.Handle<Java.Net.SocketException>()
                  .Or<Java.IO.IOException>();
    }
}