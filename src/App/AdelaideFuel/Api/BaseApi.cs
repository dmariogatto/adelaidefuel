using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace AdelaideFuel.Api
{
    public abstract class BaseApi
    {
        protected readonly HttpClient HttpClient;

        public BaseApi(HttpClient httpClient)
        {
            HttpClient = httpClient;
        }

        protected async Task<TResponse> GetAsync<TResponse>(string requestUri, CancellationToken cancellationToken, string functionKey = "")
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, requestUri);

            if (!string.IsNullOrEmpty(functionKey))
                request.Headers.Add(Constants.AuthHeader, functionKey);

            using var response = await HttpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            var result = await JsonSerializer.DeserializeAsync<TResponse>(stream, cancellationToken: cancellationToken).ConfigureAwait(false);
            return result;
        }

        protected async Task<TResponse> PostAsync<TRequest, TResponse>(string requestUri, TRequest body, CancellationToken cancellationToken, string functionKey = "")
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, requestUri)
            {
                Content = JsonContent.Create(body)
            };

            if (!string.IsNullOrEmpty(functionKey))
                request.Headers.Add(Constants.AuthHeader, functionKey);

            using var response = await HttpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            var result = await JsonSerializer.DeserializeAsync<TResponse>(stream, cancellationToken: cancellationToken).ConfigureAwait(false);
            return result;
        }
    }
}