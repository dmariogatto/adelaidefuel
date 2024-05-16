using AdelaideFuel.Functions.Models;
using Microsoft.Extensions.Options;
using SendGrid;
using SendGrid.Helpers.Mail;
using System;
using System.Threading.Tasks;

namespace AdelaideFuel.Functions.Services
{
    public class SendGridService : ISendGridService
    {
        private readonly Lazy<SendGridClient> _sendGridClient;

        private readonly string _apiKey;
        private readonly string _fromEmail;
        private readonly string _toEmail;

        public SendGridService(IOptions<SendGridOptions> options)
        {
            _apiKey = options.Value.ApiKey;
            _fromEmail = options.Value.FromEmail;
            _toEmail = options.Value.ToEmail;

            _sendGridClient = new Lazy<SendGridClient>(() => new SendGridClient(_apiKey));
        }

        public async Task<bool> SendEmailAsync(string subject, string htmlContent)
        {
            var msg = new SendGridMessage()
            {
                From = new EmailAddress(_fromEmail),
                Subject = subject,
                HtmlContent = htmlContent
            };
            msg.AddTo(new EmailAddress(_toEmail));

            var response = await _sendGridClient.Value.SendEmailAsync(msg).ConfigureAwait(false);
            return response.IsSuccessStatusCode;
        }
    }
}