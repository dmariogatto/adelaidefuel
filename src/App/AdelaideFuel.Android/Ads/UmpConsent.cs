using AdelaideFuel.Services;
using Android.Runtime;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xamarin.Essentials;
using Xamarin.Google.UserMesssagingPlatform;
using static Xamarin.Google.UserMesssagingPlatform.UserMessagingPlatform;
using CancellationToken = System.Threading.CancellationToken;
using Exception = System.Exception;

namespace AdelaideFuel.Droid
{
    public static class UmpConsent
    {
        private static IConsentInformation _instance;
        internal static IConsentInformation Instance
            => _instance ?? UserMessagingPlatform.GetConsentInformation(Platform.AppContext);

        private static ConsentDebugSettings DebugSettings;

        public static void SetDebugSettings(string[] testDeviceHashedIds, int debugGeography)
        {
            var debugSettings = new ConsentDebugSettings.Builder(Platform.AppContext)
                .SetDebugGeography(debugGeography);

            if (testDeviceHashedIds?.Any() == true)
            {
                foreach (var id in testDeviceHashedIds)
                    debugSettings = debugSettings.AddTestDeviceHashedId(id);
            }

            DebugSettings = debugSettings.Build();
        }

        public static void ResetDebugSettings()
        {
            DebugSettings = null;
        }

        public static void Reset() => Instance.Reset();

        public static Task<int> RequestAsync(bool underAge, CancellationToken cancellationToken)
            => RequestAsync(underAge, DebugSettings, cancellationToken);

        private static Task<int> RequestAsync(bool underAge, ConsentDebugSettings debugSettings, CancellationToken cancellationToken)
        {
            var tcs = new TaskCompletionSource<int>();

            try
            {
                var parameters = new ConsentRequestParameters.Builder()
                    .SetTagForUnderAgeOfConsent(underAge);

                if (debugSettings is not null)
                {
                    parameters = parameters.SetConsentDebugSettings(debugSettings);
                }

                var listner = new ConsentListener(tcs, cancellationToken);
                Instance.RequestConsentInfoUpdate(
                    Platform.CurrentActivity,
                    parameters.Build(),
                    listner,
                    listner);
            }
            catch (Exception ex)
            {
                tcs.TrySetException(ex);
            }

            return tcs.Task;
        }

        private class ConsentListener :
            Java.Lang.Object,
            IConsentInformationOnConsentInfoUpdateSuccessListener,
            IConsentInformationOnConsentInfoUpdateFailureListener,
            IOnConsentFormLoadSuccessListener,
            IOnConsentFormLoadFailureListener,
            IConsentFormOnConsentFormDismissedListener
        {
            private readonly TaskCompletionSource<int> _tcs;
            private readonly CancellationToken _cancellationToken;

            public ConsentListener(TaskCompletionSource<int> tcs, CancellationToken cancellationToken)
            {
                _tcs = tcs;
                _cancellationToken = cancellationToken;
            }

            public ConsentListener(IntPtr handle, JniHandleOwnership transfer)
                : base(handle, transfer)
            {
            }

            public void OnConsentInfoUpdateSuccess()
            {
                try
                {
                    _cancellationToken.ThrowIfCancellationRequested();

                    if (Instance.IsConsentFormAvailable)
                        UserMessagingPlatform.LoadConsentForm(Platform.AppContext, this, this);
                }
                catch (Exception ex)
                {
                    _tcs.TrySetException(ex);
                }
            }

            public void OnConsentInfoUpdateFailure(FormError p0)
            {
                try
                {
                    _cancellationToken.ThrowIfCancellationRequested();

                    throw new ConsentInfoUpdateException(p0.Message);
                }
                catch (Exception ex)
                {
                    _tcs.TrySetException(ex);
                }
            }

            public void OnConsentFormLoadSuccess(IConsentForm p0)
            {
                try
                {
                    _cancellationToken.ThrowIfCancellationRequested();

                    if (p0 is null)
                        throw new ArgumentNullException(nameof(p0), "ConsentForm is null");

                    if (Instance.ConsentStatus == ConsentInformationConsentStatus.Required)
                    {
                        p0.Show(Platform.CurrentActivity, this);
                    }
                    else
                    {
                        _tcs.TrySetResult(Instance.ConsentStatus);
                    }
                }
                catch (Exception ex)
                {
                    _tcs.TrySetException(ex);
                }
            }

            public void OnConsentFormLoadFailure(FormError p0)
            {
                try
                {
                    _cancellationToken.ThrowIfCancellationRequested();

                    throw new ConsentFormLoadException(p0.Message);
                }
                catch (Exception ex)
                {
                    _tcs.TrySetException(ex);
                }
            }

            public void OnConsentFormDismissed(FormError p0)
            {
                try
                {
                    _cancellationToken.ThrowIfCancellationRequested();

                    if (p0 is not null)
                        throw new ConsentFormPresentException(p0.Message);

                    _tcs.TrySetResult(Instance.ConsentStatus);
                }
                catch (Exception ex)
                {
                    _tcs.TrySetException(ex);
                }
            }
        }
    }
}
