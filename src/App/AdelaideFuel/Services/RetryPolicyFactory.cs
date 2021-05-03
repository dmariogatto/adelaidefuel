using Polly;
using Refit;
using System;
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
                .Or<ApiException>()
                .Or<HttpRequestException>()
                .Or<WebException>()
                .Or<OperationCanceledException>();
    }
}