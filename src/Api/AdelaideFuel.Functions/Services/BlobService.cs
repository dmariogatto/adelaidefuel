using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Specialized;
using Newtonsoft.Json;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace AdelaideFuel.Functions.Services
{
    public class BlobService : IBlobService
    {
        private const string ContainerName = "adelaidefuel";

        private readonly BlobServiceClient _blobServiceClient;

        public BlobService(string connectionString)
        {
            _blobServiceClient = new BlobServiceClient(connectionString);
        }

        public async Task SerialiseAsync<T>(T data, string localFilePath, CancellationToken cancellationToken)
        {
            var container = await GetBlobContainerAsync(cancellationToken).ConfigureAwait(false);
            var blob = container.GetBlockBlobClient(localFilePath);

            using var writer = new StreamWriter(await blob.OpenWriteAsync(true).ConfigureAwait(false));
            using var jw = new JsonTextWriter(writer);
            JsonSerializer.CreateDefault().Serialize(jw, data);
        }

        public async Task<T> DeserialiseAsync<T>(string localFilePath, CancellationToken cancellationToken)
        {
            var result = default(T);

            var container = await GetBlobContainerAsync(cancellationToken).ConfigureAwait(false);
            var blob = container.GetBlobClient(localFilePath);

            if (await blob.ExistsAsync(cancellationToken).ConfigureAwait(false))
            {
                using var reader = new StreamReader(await blob.OpenReadAsync().ConfigureAwait(false));
                using var jr = new JsonTextReader(reader);
                result = JsonSerializer.CreateDefault().Deserialize<T>(jr);
            }

            return result;
        }

        public async Task<Stream> OpenReadAsync(string localFilePath, CancellationToken cancellationToken)
        {
            var container = await GetBlobContainerAsync(cancellationToken).ConfigureAwait(false);
            var blob = container.GetBlobClient(localFilePath);

            if (await blob.ExistsAsync(cancellationToken).ConfigureAwait(false))
            {
                return await blob.OpenReadAsync().ConfigureAwait(false);
            }

            return Stream.Null;
        }

        public async Task<Stream> OpenWriteAsync(string localFilePath, CancellationToken cancellationToken)
        {
            var container = await GetBlobContainerAsync(cancellationToken).ConfigureAwait(false);
            var blob = container.GetBlockBlobClient(localFilePath);
            return await blob.OpenWriteAsync(true).ConfigureAwait(false);
        }

        public async Task WriteAllTextAsync(string data, string localFilePath, CancellationToken cancellationToken)
        {
            var container = await GetBlobContainerAsync(cancellationToken).ConfigureAwait(false);
            var blob = container.GetBlockBlobClient(localFilePath);

            using var writer = new StreamWriter(await blob.OpenWriteAsync(true).ConfigureAwait(false));
            await writer.WriteAsync(data).ConfigureAwait(false);
        }

        public async Task<string> ReadAllTextAsync(string localFilePath, CancellationToken cancellationToken)
        {
            var result = string.Empty;

            var container = await GetBlobContainerAsync(cancellationToken).ConfigureAwait(false);
            var blob = container.GetBlobClient(localFilePath);

            if (await blob.ExistsAsync(cancellationToken).ConfigureAwait(false))
            {
                using var reader = new StreamReader(await blob.OpenReadAsync().ConfigureAwait(false));
                result = await reader.ReadToEndAsync().ConfigureAwait(false);
            }

            return result;
        }

        public async Task<bool> ExistsAsync(string localFilePath, CancellationToken cancellationToken)
        {
            var container = await GetBlobContainerAsync(cancellationToken).ConfigureAwait(false);
            var blob = container.GetBlobClient(localFilePath);
            return await blob.ExistsAsync(cancellationToken).ConfigureAwait(false);
        }

        public async Task DeleteAsync(string localFilePath, CancellationToken cancellationToken)
        {
            var container = await GetBlobContainerAsync(cancellationToken).ConfigureAwait(false);
            var blob = container.GetBlobClient(localFilePath);
            await blob.DeleteIfExistsAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        private async Task<BlobContainerClient> GetBlobContainerAsync(CancellationToken cancellationToken = default)
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(ContainerName);
            await containerClient.CreateIfNotExistsAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
            return containerClient;
        }
    }
}
