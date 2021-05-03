using AdelaideFuel.Services;
using Foundation;
using Polly;

namespace AdelaideFuel.iOS.Services
{
    [Preserve(AllMembers = true)]
    public class RetryPolicyService_iOS : IRetryPolicyService
    {
        public PolicyBuilder GetNativeNetRetryPolicy() =>
            Policy.Handle<System.IO.IOException>()
                  .Or<System.AggregateException>();
    }
}