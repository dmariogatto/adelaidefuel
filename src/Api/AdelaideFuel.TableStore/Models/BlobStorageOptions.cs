using System;

namespace AdelaideFuel.TableStorage.Models
{
    public class BlobStorageOptions
    {
        public string AzureWebJobsStorage { get; set; }
        public string BlobContainerName { get; set; }
    }
}