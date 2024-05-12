using Microsoft.Maui.ApplicationModel;
using System.Threading.Tasks;

namespace AdelaideFuel.Essentials
{
    public class PermissionsImplementation : IPermissions
    {
        public PermissionsImplementation()
        {
        }

        public Task<PermissionStatus> CheckStatusAsync<TPermission>()
            where TPermission : Permissions.BasePermission, new()
            => Permissions.CheckStatusAsync<TPermission>();

        public Task<PermissionStatus> RequestAsync<TPermission>() where TPermission : Permissions.BasePermission, new()
            => Permissions.RequestAsync<TPermission>();

        public bool ShouldShowRationale<TPermission>() where TPermission : Permissions.BasePermission, new()
            => Permissions.ShouldShowRationale<TPermission>();
    }
}