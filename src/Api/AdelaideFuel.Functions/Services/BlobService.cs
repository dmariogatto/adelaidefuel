using AdelaideFuel.Functions.Models;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Specialized;
using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace AdelaideFuel.Functions.Services
{
    public class BlobService : IBlobService
    {
        private readonly BlobServiceClient _blobServiceClient;
        private readonly string _blobContainerName;

        public BlobService(BlobStorageOptions options)
        {
            if (string.IsNullOrEmpty(options?.AzureWebJobsStorage))
                throw new ArgumentException(nameof(BlobStorageOptions.AzureWebJobsStorage));
            if (string.IsNullOrEmpty(options?.BlobContainerName))
                throw new ArgumentException(nameof(BlobStorageOptions.AzureWebJobsStorage));

            _blobServiceClient = new BlobServiceClient(options.AzureWebJobsStorage);
            _blobContainerName = options.BlobContainerName;
        }

        public async Task SerialiseAsync<T>(T data, string localFilePath, CancellationToken cancellationToken)
        {
            var container = await GetBlobContainerAsync(cancellationToken).ConfigureAwait(false);
            var blob = container.GetBlockBlobClient(localFilePath);

            using var stream = await blob.OpenWriteAsync(true, cancellationToken: cancellationToken).ConfigureAwait(false);
            await JsonSerializer.SerializeAsync(stream, data).ConfigureAwait(false);
        }

        public async Task<T> DeserialiseAsync<T>(string localFilePath, CancellationToken cancellationToken)
        {
            var result = default(T);

            var container = await GetBlobContainerAsync(cancellationToken).ConfigureAwait(false);
            var blob = container.GetBlobClient(localFilePath);

            if (await blob.ExistsAsync(cancellationToken).ConfigureAwait(false))
            {
                using var stream = await blob.OpenReadAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
                result = await JsonSerializer.DeserializeAsync<T>(stream).ConfigureAwait(false);
            }

            return result;
        }

        public async Task<Stream> OpenReadAsync(string localFilePath, CancellationToken cancellationToken)
        {
            var container = await GetBlobContainerAsync(cancellationToken).ConfigureAwait(false);
            var blob = container.GetBlobClient(localFilePath);

            if (await blob.ExistsAsync(cancellationToken).ConfigureAwait(false))
            {
                return await blob.OpenReadAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
            }

            return Stream.Null;
        }

        public async Task<Stream> OpenWriteAsync(string localFilePath, CancellationToken cancellationToken)
        {
            var container = await GetBlobContainerAsync(cancellationToken).ConfigureAwait(false);
            var blob = container.GetBlockBlobClient(localFilePath);
            return await blob.OpenWriteAsync(true, cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        public async Task WriteAllTextAsync(string data, string localFilePath, CancellationToken cancellationToken)
        {
            var container = await GetBlobContainerAsync(cancellationToken).ConfigureAwait(false);
            var blob = container.GetBlockBlobClient(localFilePath);

            using var writer = new StreamWriter(await blob.OpenWriteAsync(true, cancellationToken: cancellationToken).ConfigureAwait(false));
            await writer.WriteAsync(data).ConfigureAwait(false);
        }

        public async Task<string> ReadAllTextAsync(string localFilePath, CancellationToken cancellationToken)
        {
            var result = string.Empty;

            var container = await GetBlobContainerAsync(cancellationToken).ConfigureAwait(false);
            var blob = container.GetBlobClient(localFilePath);

            if (await blob.ExistsAsync(cancellationToken).ConfigureAwait(false))
            {
                using var reader = new StreamReader(await blob.OpenReadAsync(cancellationToken: cancellationToken).ConfigureAwait(false));
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
            var containerClient = _blobServiceClient.GetBlobContainerClient(_blobContainerName);
            await containerClient.CreateIfNotExistsAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
            return containerClient;
        }
    }
}