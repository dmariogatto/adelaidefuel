using AdelaideFuel.Services;
using Foundation;
using Google.UserMessagingPlatform;
using ObjCRuntime;
using System;
using System.Linq;
using System.Threading.Tasks;
using UIKit;

namespace AdelaideFuel.iOS
{
    public static class UmpConsent
    {
        private static DebugSettings DebugSettings;
        private static TaskCompletionSource<ConsentStatus> Tcs;

        internal static ConsentInformation Instance => ConsentInformation.SharedInstance;

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

        public static Task<ConsentStatus> RequestAsync(bool underAge)
            => RequestAsync(underAge, DebugSettings);

        private static Task<ConsentStatus> RequestAsync(bool underAge, DebugSettings debugSettings)
        {
            if (Tcs?.Task is Task<ConsentStatus> task && !task.IsCompleted)
                return task;

            Tcs = new TaskCompletionSource<ConsentStatus>();

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
                    RequestHandler);
            }
            catch (Exception ex)
            {
                Tcs.TrySetException(ex);
            }

            return Tcs.Task;
        }

        private static void LoadForm()
        {
            try
            {
                ConsentForm.LoadWithCompletionHandler(LoadFormHandler);
            }
            catch (Exception ex)
            {
                Tcs.TrySetException(ex);
            }
        }

        [MonoPInvokeCallback(typeof(ConsentInformationUpdateCompletionHandler))]
        private static void RequestHandler(NSError error)
        {
            try
            {
                if (error is not null)
                    throw new ConsentInfoUpdateException(error.LocalizedDescription);

                if (Instance.FormStatus == FormStatus.Available)
                {
                    LoadForm();
                }
                else
                {
                    Tcs.TrySetResult(Instance.ConsentStatus);
                }
            }
            catch (Exception ex)
            {
                Tcs.TrySetException(ex);
            }
        }

        [MonoPInvokeCallback(typeof(ConsentFormLoadCompletionHandler))]
        private static void LoadFormHandler(ConsentForm form, NSError error)
        {
            try
            {
                if (error is not null)
                    throw new ConsentFormLoadException(error.LocalizedDescription);
                if (form is null)
                    throw new ArgumentNullException(nameof(form), "ConsentForm is null");

                if (Instance.ConsentStatus == ConsentStatus.Required)
                {
                    var vc = GetRootViewController() ?? throw new ArgumentNullException("viewController", "RootViewController is null");
                    form.PresentFromViewController(vc, PresentFormHandler);
                }
                else
                {
                    Tcs.TrySetResult(Instance.ConsentStatus);
                }
            }
            catch (Exception ex)
            {
                Tcs.TrySetException(ex);
            }
        }

        [MonoPInvokeCallback(typeof(ConsentFormPresentCompletionHandler))]
        private static void PresentFormHandler(NSError error)
        {
            try
            {
                if (error is not null)
                    throw new ConsentFormPresentException(error.LocalizedDescription);

                Tcs.TrySetResult(Instance.ConsentStatus);
            }
            catch (Exception ex)
            {
                Tcs.TrySetException(ex);
            }
        }

        private static UIViewController GetRootViewController()
            => UIApplication.SharedApplication.Windows
                .Where(w => w.RootViewController is not null)
                .Select(w => w.RootViewController)
                .FirstOrDefault();
    }
}
