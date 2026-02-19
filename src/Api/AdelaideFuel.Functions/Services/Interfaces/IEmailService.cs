using System.Threading.Tasks;

namespace AdelaideFuel.Functions.Services
{
    public interface IEmailService
    {
        Task<bool> SendEmailAsync(string subject, string htmlContent);
    }
}