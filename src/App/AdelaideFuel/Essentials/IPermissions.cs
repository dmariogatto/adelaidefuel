using Microsoft.Maui.ApplicationModel;
using System.Threading.Tasks;

namespace AdelaideFuel.Essentials
{
    public interface IPermissions
    {
        Task<PermissionStatus> CheckStatusAsync<TPermission>() where TPermission : Permissions.BasePermission, new();
        Task<PermissionStatus> RequestAsync<TPermission>() where TPermission : Permissions.BasePermission, new();
        bool ShouldShowRationale<TPermission>() where TPermission : Permissions.BasePermission, new();
    }
}