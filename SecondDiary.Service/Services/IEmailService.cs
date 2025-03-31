using SecondDiary.Models;
using System.Threading.Tasks;

namespace SecondDiary.Services
{
    public interface IEmailService
    {
        Task SendRecommendationEmailAsync(string userId, string emailAddress, string recommendation);
        Task SendTestEmailAsync(string emailAddress);
        Task<bool> CheckAndSendScheduledEmailsAsync();
    }
}
