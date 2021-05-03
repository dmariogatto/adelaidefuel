using Polly;

namespace AdelaideFuel.Services
{
    public interface IRetryPolicyService
    {
        /// <summary>
        /// Get retry policy to handle native web request exceptions
        /// </summary>
        PolicyBuilder GetNativeNetRetryPolicy();
    }
}