using System;

namespace AdelaideFuel.Functions.Models
{
    public class BlobStorageOptions
    {
        public string AzureWebJobsStorage { get; set; }
        public string BlobContainerName { get; set; }
    }
}
