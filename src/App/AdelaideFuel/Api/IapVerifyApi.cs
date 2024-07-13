using AdelaideFuel.Models;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace AdelaideFuel.Api
{
    public class IapVerifyApi : BaseApi, IIapVerifyApi
    {
        public IapVerifyApi(HttpClient httpClient) : base(httpClient)
        {
        }

        public Task<IapValidatedReceipt> AppleAsync(IapReceipt receipt, CancellationToken cancellationToken)
        {
            return PostAsync<IapReceipt, IapValidatedReceipt>("Apple", receipt, cancellationToken);
        }

        public Task<IapValidatedReceipt> GoogleAsync(IapReceipt receipt, CancellationToken cancellationToken)
        {
            return PostAsync<IapReceipt, IapValidatedReceipt>("Google", receipt, cancellationToken);
        }
    }
}