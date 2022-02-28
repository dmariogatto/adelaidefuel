using System;

namespace AdelaideFuel.Functions.Models
{
    public class SendGridOptions
    {
        public string ApiKey { get; set; }
        public string FromEmail { get; set; }
        public string ToEmail { get; set; }
    }
}
