using System.Threading.Tasks;

namespace AdelaideFuel.Services
{
    public interface IUserNativeService : IUserNativeReadOnlyService
    {
        Task<bool> SyncUserBrandsAsync();
        Task<bool> SyncUserFuelsAsync();
    }
}