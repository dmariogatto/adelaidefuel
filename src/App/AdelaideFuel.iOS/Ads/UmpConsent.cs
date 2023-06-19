using AdelaideFuel.Services;
using Foundation;
using Google.UserMessagingPlatform;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UIKit;

namespace AdelaideFuel.iOS
{
    public static class UmpConsent
    {
        internal static ConsentInformation Instance => ConsentInformation.SharedInstance;

        private static DebugSettings DebugSettings;

        public static void SetDebugSettings(string[] testDeviceIdentifiers, DebugGeography debugGeography)
        {
            var debugSettings = new DebugSettings();
            debugSettings.TestDeviceIdentifiers = testDeviceIdentifiers;
            debugSettings.Geography = debugGeography;
            DebugSettings = debugSettings;
        }

        public static void ResetDebugSettings()
        {
            DebugSettings = null;
        }

        public static void Reset() => Instance.Reset();

        public static Task<ConsentStatus> RequestAsync(bool underAge, CancellationToken cancellationToken)
            => RequestAsync(underAge, DebugSettings, cancellationToken);

        private static Task<ConsentStatus> RequestAsync(bool underAge, DebugSettings debugSettings, CancellationToken cancellationToken)
        {
            var tcs = new TaskCompletionSource<ConsentStatus>();

            try
            {
                var parameters = new RequestParameters()
                {
                    TagForUnderAgeOfConsent = underAge
                };

                if (debugSettings is not null)
                {
                    parameters.DebugSettings = debugSettings;
                }

                Instance.RequestConsentInfoUpdateWithParameters(
                    parameters,
                    (error) => RequestHandler(error, tcs, cancellationToken));
            }
            catch (Exception ex)
            {
                tcs.TrySetException(ex);
            }

            return tcs.Task;
        }

        private static void LoadForm(TaskCompletionSource<ConsentStatus> tcs, CancellationToken cancellationToken)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                ConsentForm.LoadWithCompletionHandler(
                    (form, error) => LoadFormHandler(form, error, tcs, cancellationToken));
            }
            catch (Exception ex)
            {
                tcs.TrySetException(ex);
            }
        }

        private static void RequestHandler(NSError error, TaskCompletionSource<ConsentStatus> tcs, CancellationToken cancellationToken)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (error is not null)
                    throw new ConsentInfoUpdateException(error.LocalizedDescription);

                if (Instance.FormStatus == FormStatus.Available)
                {
                    LoadForm(tcs, cancellationToken);
                }
                else
                {
                    tcs.TrySetResult(Instance.ConsentStatus);
                }
            }
            catch (Exception ex)
            {
                tcs.TrySetException(ex);
            }
        }

        private static void LoadFormHandler(ConsentForm form, NSError error, TaskCompletionSource<ConsentStatus> tcs, CancellationToken cancellationToken)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (error is not null)
                    throw new ConsentFormLoadException(error.LocalizedDescription);
                if (form is null)
                    throw new ArgumentNullException(nameof(form), "ConsentForm is null");

                if (Instance.ConsentStatus == ConsentStatus.Required)
                {
                    var vc = GetRootViewController() ?? throw new ArgumentNullException("viewController", "RootViewController is null");
                    form.PresentFromViewController(
                        vc,
                        (error) => PresentFormHandler(error, tcs, cancellationToken));
                }
                else
                {
                    tcs.TrySetResult(Instance.ConsentStatus);
                }
            }
            catch (Exception ex)
            {
                tcs.TrySetException(ex);
            }
        }

        private static void PresentFormHandler(NSError error, TaskCompletionSource<ConsentStatus> tcs, CancellationToken cancellationToken)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (error is not null)
                    throw new ConsentFormPresentException(error.LocalizedDescription);

                tcs.TrySetResult(Instance.ConsentStatus);
            }
            catch (Exception ex)
            {
                tcs.TrySetException(ex);
            }
        }

        private static UIViewController GetRootViewController()
            => UIApplication.SharedApplication.Windows
                .Where(w => w.RootViewController is not null)
                .Select(w => w.RootViewController)
                .FirstOrDefault();
    }
}
