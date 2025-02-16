using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace AdelaideFuel.Services
{
    public interface IBrandService
    {
        string GetBrandImagePath(int brandId);
        Task<string> GetBrandImagePathAsync(int brandId, CancellationToken cancellationToken);
        Task<IReadOnlyDictionary<int, string>> GetBrandImagePathsAsync(IReadOnlyList<int> brandIds, bool preferCache, CancellationToken cancellationToken);
    }
}