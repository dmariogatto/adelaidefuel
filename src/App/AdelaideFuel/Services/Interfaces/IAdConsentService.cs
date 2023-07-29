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
        event EventHandler<AdConsentStatusChangedEventArgs> AdConsentStatusChanged;

        AdConsentStatus Status { get; }
        bool ShouldRequest { get; }
        bool CanServeAds { get; }

        Task<AdConsentStatus> RequestAsync();
    }

    public class AdConsentStatusChangedEventArgs : EventArgs
    {
        public bool OldCanServeAds { get; }
        public bool NewCanServeAds { get; }

        public AdConsentStatus OldStatus { get; }
        public AdConsentStatus NewStatus { get; }

        public AdConsentStatusChangedEventArgs(
            bool oldCanServeAds,
            bool newCanServeAds,
            AdConsentStatus oldStatus,
            AdConsentStatus newStatus)
        {
            OldCanServeAds = oldCanServeAds;
            NewCanServeAds = newCanServeAds;
            OldStatus = oldStatus;
            NewStatus = newStatus;
        }
    }

    public abstract class ConsentException : Exception
    {
        public ConsentException(string message) : base(message)
        {
        }

        public ConsentException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }

    public class ConsentInfoUpdateException : ConsentException
    {
        public ConsentInfoUpdateException(string message) : base(message)
        {
        }

        public ConsentInfoUpdateException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }

    public class ConsentFormLoadException : ConsentException
    {
        public ConsentFormLoadException(string message) : base(message)
        {
        }

        public ConsentFormLoadException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }

    public class ConsentFormPresentException : ConsentException
    {
        public ConsentFormPresentException(string message) : base(message)
        {
        }

        public ConsentFormPresentException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
