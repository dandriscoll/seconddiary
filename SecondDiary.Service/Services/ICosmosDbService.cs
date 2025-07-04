using System.Collections.Generic;
using System.Threading.Tasks;
using SecondDiary.Models;

namespace SecondDiary.Services
{
    public interface ICosmosDbService
    {
        Task InitializeAsync();
        Task<DiaryEntry> CreateDiaryEntryAsync(DiaryEntry entry);
        Task<DiaryEntry?> GetDiaryEntryAsync(string id, string userId);
        Task<IEnumerable<DiaryEntry>> GetDiaryEntriesAsync(string userId);
        Task<DiaryEntry> UpdateDiaryEntryAsync(DiaryEntry entry);
        Task DeleteDiaryEntryAsync(string id, string userId);

        Task<SystemPrompt> CreateSystemPromptAsync(SystemPrompt prompt);
        Task<SystemPrompt?> GetSystemPromptAsync(string id, string userId);
        Task<IEnumerable<SystemPrompt>> GetSystemPromptsAsync(string userId);
        Task<SystemPrompt> UpdateSystemPromptAsync(SystemPrompt prompt);
        Task DeleteSystemPromptAsync(string id, string userId);

        // Email Settings
        Task<EmailSettings> CreateEmailSettingsAsync(EmailSettings settings);
        Task<EmailSettings?> GetEmailSettingsAsync(string userId);
        Task<IEnumerable<EmailSettings>> GetAllEmailSettingsAsync();
        Task<EmailSettings> UpdateEmailSettingsAsync(EmailSettings settings);
        Task DeleteEmailSettingsAsync(string userId);
        
        // Recommendations
        Task<Recommendation> CreateRecommendationAsync(Recommendation recommendation);
        Task<IEnumerable<Recommendation>> GetRecentRecommendationsAsync(string userId, int count = 5);
        
        // Personal Access Tokens
        Task<PersonalAccessToken> CreatePersonalAccessTokenAsync(PersonalAccessToken token);
        Task<PersonalAccessToken?> GetPersonalAccessTokenByIdAsync(string id);
        Task<IEnumerable<PersonalAccessToken>> GetPersonalAccessTokensAsync(string userId);
        Task DeletePersonalAccessTokenAsync(string id, string userId);
    }
}