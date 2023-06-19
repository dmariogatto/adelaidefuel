using AdelaideFuel.Services;
using Google.UserMessagingPlatform;
using System;
using System.Linq;
using System.Threading.Tasks;
using UIKit;

namespace AdelaideFuel.iOS
{
    public static class UmpConsent
    {
        private static DebugSettings DebugSettings;

        public static ConsentStatus Status => ConsentInformation.SharedInstance.ConsentStatus;

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

        public static void Reset() => ConsentInformation.SharedInstance.Reset();

        public static Task<ConsentStatus> RequestAsync(bool underAge)
            => RequestAsync(underAge, DebugSettings);

        private static async Task<ConsentStatus> RequestAsync(bool underAge, DebugSettings debugSettings)
        {
            var parameters = new RequestParameters()
            {
                TagForUnderAgeOfConsent = underAge
            };

            if (debugSettings is not null)
            {
                parameters.DebugSettings = debugSettings;
            }

            try
            {
                await ConsentInformation.SharedInstance.RequestConsentInfoUpdateWithParametersAsync(parameters);
            }
            catch (Exception ex)
            {
                _ = DumbDebug();
                throw new ConsentInfoUpdateException(ex.Message, ex);
            }

            if (ConsentInformation.SharedInstance.FormStatus == FormStatus.Available)
            {
                var form = default(ConsentForm);

                try
                {
                    form = await ConsentForm.LoadWithCompletionHandlerAsync();
                }
                catch (Exception ex)
                {
                    throw new ConsentFormLoadException(ex.Message, ex);
                }

                if (form is null)
                    throw new ArgumentNullException(nameof(form), "ConsentForm is null");

                if (ConsentInformation.SharedInstance.ConsentStatus == ConsentStatus.Required)
                {
                    var vc = GetRootViewController() ?? throw new ArgumentNullException("viewController", "RootViewController is null");
                    try
                    {
                        await form.PresentFromViewControllerAsync(vc);
                    }
                    catch (Exception ex)
                    {
                        throw new ConsentFormPresentException(ex.Message, ex);
                    }
                }
            }

            return ConsentInformation.SharedInstance.ConsentStatus;
        }

        private static UIViewController GetRootViewController()
            => UIApplication.SharedApplication.Windows
                .Where(w => w.RootViewController is not null)
                .Select(w => w.RootViewController)
                .FirstOrDefault();

        private static async Task DumbDebug()
        {
            try
            {
                var form = await ConsentForm.LoadWithCompletionHandlerAsync();
            }
            catch (Exception ex)
            {
                IoC.Resolve<ILogger>().Error(new ConsentFormLoadException(ex.Message, ex));
            }
        }
    }
}
