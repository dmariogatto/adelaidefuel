using Polly;
using System.Net;
using System.Net.Http;

namespace AdelaideFuel.Services
{
    public class RetryPolicyFactory : IRetryPolicyFactory
    {
        private readonly IRetryPolicyService _retryPolicyService;

        public RetryPolicyFactory(IRetryPolicyService retryPolicyService)
        {
            _retryPolicyService = retryPolicyService;
        }

        public PolicyBuilder GetNetRetryPolicy() =>
            _retryPolicyService.GetNativeNetRetryPolicy()
                .Or<HttpRequestException>(e => !e.StatusCode.HasValue || CanRetryStatusCode(e.StatusCode.Value));

        private static bool CanRetryStatusCode(HttpStatusCode statusCode) =>
            statusCode != HttpStatusCode.TooManyRequests &&
            statusCode != HttpStatusCode.BadRequest &&
            statusCode != HttpStatusCode.NotFound;
    }
}