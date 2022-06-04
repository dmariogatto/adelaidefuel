using AdelaideFuel.Models;
using Refit;
using System.Threading;
using System.Threading.Tasks;

namespace AdelaideFuel.Api
{
    public interface IIapVerifyApi
    {
        [Post("/Apple")]
        Task<IapValidatedReceipt> AppleAsync([Body] IapReceipt receipt, CancellationToken cancellationToken);

        [Post("/Google")]
        Task<IapValidatedReceipt> GoogleAsync([Body] IapReceipt receipt, CancellationToken cancellationToken);
    }
}