using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace AdelaideFuel.Services
{
    public class BaseHttpService : BaseService
    {
        public BaseHttpService(ICacheService cacheService, ILogger logger) : base(cacheService, logger)
        {
        }

        protected readonly static HttpClient HttpClient = new HttpClient();

        protected static ValueTask<T> DeserializeJsonFromStreamAsync<T>(Stream stream)
        {
            if (stream is null || stream.CanRead == false)
                return default;
            return JsonSerializer.DeserializeAsync<T>(stream);
        }
    }
}