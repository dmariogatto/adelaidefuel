using Foundation;
using Polly;

namespace AdelaideFuel.Services
{
    [Preserve(AllMembers = true)]
    public class RetryPolicyPlatformService : IRetryPolicyService
    {
        public PolicyBuilder GetNativeNetRetryPolicy() =>
            Policy.Handle<IOException>()
                  .Or<AggregateException>();
    }
}