using System.Threading.Tasks;

namespace AdelaideFuel.Functions.Services
{
    public interface ISendGridService
    {
        Task<bool> SendEmailAsync(string subject, string htmlContent);
    }
}
