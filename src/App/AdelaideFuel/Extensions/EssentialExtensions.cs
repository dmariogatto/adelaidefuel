using AdelaideFuel.Essentials;
using AdelaideFuel.Localisation;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.ApplicationModel.Communication;
using Microsoft.Maui.Devices;
using Microsoft.Maui.Devices.Sensors;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static Microsoft.Maui.ApplicationModel.Permissions;

namespace AdelaideFuel
{
    public static class EssentialExtensions
    {
        private static readonly Lazy<IPermissions> Permissions = new Lazy<IPermissions>(() => IoC.Resolve<IPermissions>());

        public static double HeightScaled(this IDeviceDisplay deviceDisplay)
            => deviceDisplay.MainDisplayInfo.Height / deviceDisplay.MainDisplayInfo.Density;
        public static double WidthScaled(this IDeviceDisplay deviceDisplay)
            => deviceDisplay.MainDisplayInfo.Width / deviceDisplay.MainDisplayInfo.Density;
        public static bool IsSmall(this IDeviceDisplay deviceDisplay)
            => deviceDisplay.HeightScaled() <= 640 || deviceDisplay.WidthScaled() <= 320;

        public static async Task<PermissionStatus> CheckAndRequestAsync<T>(this IPermissions permissions) where T : BasePermission, new()
        {
            var status = await permissions.CheckStatusAsync<T>().ConfigureAwait(false);

            if (status != PermissionStatus.Granted)
            {
                status = await permissions.RequestAsync<T>().ConfigureAwait(false);
            }

            return status;
        }

        public static async Task<Location> GetLocationAsync(this IGeolocation geolocation, CancellationToken cancellationToken)
        {
            var location = default(Location);

            try
            {
                var status = await Permissions.Value.CheckAndRequestAsync<Permissions.LocationWhenInUse>()
                    .ConfigureAwait(false);
                if (status == PermissionStatus.Granted)
                {
                    location = await geolocation.GetLastKnownLocationAsync().ConfigureAwait(false) ??
                        await geolocation.GetLocationAsync(
                            new GeolocationRequest(GeolocationAccuracy.Medium, TimeSpan.FromSeconds(2.5)),
                            cancellationToken).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
            }

            return location;
        }

        public static EmailMessage GetFeedbackEmailMessage(this IEmail email, IAppInfo appInfo, IDeviceInfo deviceInfo)
        {
            var builder = new StringBuilder();
            builder.AppendLine($"App: {appInfo.VersionString} | {appInfo.BuildString}");
            builder.AppendLine($"OS: {deviceInfo.Platform} | {deviceInfo.VersionString}");
            builder.AppendLine($"Device: {deviceInfo.Manufacturer} | {deviceInfo.Model}");
            builder.AppendLine();
            builder.AppendLine(string.Format(Resources.ItemComma, Resources.AddYourMessageBelow));
            builder.AppendLine("----");
            builder.AppendLine();

            var message = new EmailMessage
            {
                Subject = string.Format(Resources.FeedbackSubjectItem, deviceInfo.Platform),
                Body = builder.ToString(),
                To = new List<string>(1) { Constants.Email },
            };

            return message;
        }
    }
}