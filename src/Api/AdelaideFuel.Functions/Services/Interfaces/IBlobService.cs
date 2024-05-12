using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace AdelaideFuel.Functions.Services
{
    public interface IBlobService
    {
        Task SerialiseAsync<T>(T data, string localFilePath, CancellationToken cancellationToken);
        Task<T> DeserialiseAsync<T>(string localFilePath, CancellationToken cancellationToken);

        Task<Stream> OpenReadAsync(string localFilePath, CancellationToken cancellationToken);
        Task<Stream> OpenWriteAsync(string localFilePath, CancellationToken cancellationToken);

        Task WriteAllTextAsync(string data, string localFilePath, CancellationToken cancellationToken);
        Task<string> ReadAllTextAsync(string localFilePath, CancellationToken cancellationToken);

        Task<bool> ExistsAsync(string localFilePath, CancellationToken cancellationToken);
        Task DeleteAsync(string localFilePath, CancellationToken cancellationToken);
    }
}