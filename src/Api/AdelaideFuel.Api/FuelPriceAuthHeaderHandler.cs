using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace AdelaideFuel.Api
{
    public class FuelPriceAuthHeaderHandler : DelegatingHandler
    {
        private readonly string _subscriberToken;

        public FuelPriceAuthHeaderHandler(string subscriberToken)
        {
            if (!Guid.TryParse(subscriberToken, out _)) throw new ArgumentException($"Token must be a valid GUID!");

            _subscriberToken = subscriberToken;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var path = request.RequestUri.AbsolutePath;
            if (path.StartsWith("/Subscriber") || path.StartsWith("/Price"))
                request.Headers.Add("Authorization", $"FPDAPI SubscriberToken={_subscriberToken}");

            return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
        }
    }
}