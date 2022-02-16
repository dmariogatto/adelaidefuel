using Newtonsoft.Json;
using System.IO;
using System.Net.Http;

namespace AdelaideFuel.Services
{
    public class BaseHttpService : BaseService
    {
        public BaseHttpService(ICacheService cacheService, ILogger logger) : base(cacheService, logger)
        {
        }

        protected readonly static HttpClient HttpClient = new HttpClient();

        protected static T DeserializeJsonFromStream<T>(Stream stream)
        {
            if (stream == null || stream.CanRead == false)
                return default;

            using var sr = new StreamReader(stream);
            using var jtr = new JsonTextReader(sr);

            var result = JsonSerializer.CreateDefault().Deserialize<T>(jtr);
            return result;
        }
    }
}