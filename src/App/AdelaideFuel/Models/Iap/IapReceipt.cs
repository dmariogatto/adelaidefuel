using System;

namespace AdelaideFuel.Models
{
    public class IapReceipt
    {
        public string BundleId { get; set; }
        public string ProductId { get; set; }
        public string TransactionId { get; set; }
        public string Token { get; set; }

        public string AppVersion { get; set; }
    }
}