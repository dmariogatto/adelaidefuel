using Polly;
using Refit;
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
                .Or<ApiException>(e => e.StatusCode != HttpStatusCode.BadRequest &&
                                       e.StatusCode != HttpStatusCode.NotFound &&
                                       e.StatusCode != HttpStatusCode.InternalServerError)
                .Or<HttpRequestException>();
    }
}