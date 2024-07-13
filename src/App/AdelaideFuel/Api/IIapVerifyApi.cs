using AdelaideFuel.Models;
using System.Threading;
using System.Threading.Tasks;

namespace AdelaideFuel.Api
{
    public interface IIapVerifyApi
    {
        Task<IapValidatedReceipt> AppleAsync(IapReceipt receipt, CancellationToken cancellationToken);

        Task<IapValidatedReceipt> GoogleAsync(IapReceipt receipt, CancellationToken cancellationToken);
    }
}