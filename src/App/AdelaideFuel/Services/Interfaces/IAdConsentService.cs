using System;
using System.Threading;
using System.Threading.Tasks;

namespace AdelaideFuel.Services
{
    public enum AdConsentStatus
    {
        Unknown,
        Required,
        NotRequired,
        Obtained,
    }

    public interface IAdConsentService
    {
        AdConsentStatus Status { get; }
        bool CanServeAds { get; }

        Task<AdConsentStatus> RequestAsync(CancellationToken cancellationToken);
    }

    public class ConsentInfoUpdateException : Exception
    {
        public ConsentInfoUpdateException(string message) : base(message)
        {
        }
    }

    public class ConsentFormLoadException : Exception
    {
        public ConsentFormLoadException(string message) : base(message)
        {
        }
    }

    public class ConsentFormPresentException : Exception
    {
        public ConsentFormPresentException(string message) : base(message)
        {
        }
    }
}
