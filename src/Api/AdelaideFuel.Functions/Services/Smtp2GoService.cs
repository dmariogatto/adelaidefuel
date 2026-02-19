using AdelaideFuel.Functions.Models;
using Microsoft.Extensions.Options;
using Smtp2Go.Api;
using Smtp2Go.Api.Models.Emails;
using System;
using System.Threading.Tasks;

namespace AdelaideFuel.Functions.Services
{
    public class Smtp2GoService : IEmailService
    {
        private readonly Lazy<Smtp2GoApiService> _api;

        private readonly string _apiKey;
        private readonly string _fromEmail;
        private readonly string _toEmail;

        public Smtp2GoService(IOptions<Smtp2GoOptions> options)
        {
            _apiKey = options.Value.ApiKey;
            _fromEmail = options.Value.FromEmail;
            _toEmail = options.Value.ToEmail;

            _api = new Lazy<Smtp2GoApiService>(() => new Smtp2GoApiService(_apiKey));
        }

        public async Task<bool> SendEmailAsync(string subject, string htmlContent)
        {
            var msg = new EmailMessage(_fromEmail, _toEmail)
            {
                Subject = subject,
                BodyHtml = htmlContent
            };

            var response = await _api.Value.SendEmail(msg).ConfigureAwait(false);
            return response.Data.Failed == 0;
        }
    }
}