using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace AdelaideFuel.Api
{
    public class FuelPriceAuthHeaderHandler : DelegatingHandler
    {
        private readonly string _subscriberToken;
        private readonly string _authHeader;

        public FuelPriceAuthHeaderHandler(string subscriberToken)
        {
            if (!Guid.TryParse(subscriberToken, out _)) throw new ArgumentException("Token must be a valid GUID!");

            _subscriberToken = subscriberToken;
            _authHeader = $"FPDAPI SubscriberToken={_subscriberToken}";
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var path = request.RequestUri.AbsolutePath;

            if (path.StartsWith("/Subscriber", StringComparison.Ordinal) || path.StartsWith("/Price", StringComparison.Ordinal))
                request.Headers.Add("Authorization", _authHeader);

            return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
        }
    }
}