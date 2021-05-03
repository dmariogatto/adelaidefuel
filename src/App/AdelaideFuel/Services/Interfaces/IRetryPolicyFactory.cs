using Polly;

namespace AdelaideFuel.Services
{
    public interface IRetryPolicyFactory
    {
        PolicyBuilder GetNetRetryPolicy();
    }
}