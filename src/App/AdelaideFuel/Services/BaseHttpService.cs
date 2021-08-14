using Newtonsoft.Json;
using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace AdelaideFuel.Services
{
    public class BaseHttpService : BaseService
    {
        private readonly static JsonSerializer JsonSerializer = JsonSerializer.Create(new JsonSerializerSettings()
        {
            DateTimeZoneHandling = DateTimeZoneHandling.Utc
        });

        public BaseHttpService(ICacheService cacheService, ILogger logger) : base(cacheService, logger)
        {
        }

        protected readonly static HttpClient HttpClient = new HttpClient() { Timeout = TimeSpan.FromSeconds(10) };

        protected async Task<bool> HeadAsync(string url, CancellationToken cancellationToken)
        {
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Head, url);
                using var response = await HttpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
                response.EnsureSuccessStatusCode();

                return true;
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }

            return false;
        }

        protected static T DeserializeJsonFromStream<T>(Stream stream)
        {
            if (stream == null || stream.CanRead == false)
                return default;

            using var sr = new StreamReader(stream);
            using var jtr = new JsonTextReader(sr);

            var result = JsonSerializer.Deserialize<T>(jtr);
            return result;
        }
    }
}