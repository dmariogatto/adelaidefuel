using System;
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

        Task<AdConsentStatus> RequestAsync();
    }

    public class ConsentInfoUpdateException : Exception
    {
        public ConsentInfoUpdateException(string message) : base(message)
        {
        }

        public ConsentInfoUpdateException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }

    public class ConsentFormLoadException : Exception
    {
        public ConsentFormLoadException(string message) : base(message)
        {
        }

        public ConsentFormLoadException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }

    public class ConsentFormPresentException : Exception
    {
        public ConsentFormPresentException(string message) : base(message)
        {
        }

        public ConsentFormPresentException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
