using DeligateWebAPI.Models;

namespace DeligateWebAPI.Interfaces
{
    public interface IEmailService
    {
        Task<(bool isSuccess, string errorMessage)> SendEmailAsync(EmailRequest request);
    }
}
