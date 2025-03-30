using SecondDiary.Models;

namespace SecondDiary.Services
{
    public interface IDiaryService
    {
        Task<DiaryEntry> CreateEntryAsync(DiaryEntry entry);
        Task<DiaryEntry?> GetEntryAsync(string id, string userId);
        Task<IEnumerable<DiaryEntry>> GetEntriesAsync(string userId);
        Task<DiaryEntry> UpdateEntryAsync(DiaryEntry entry);
        Task DeleteEntryAsync(string id, string userId);
        Task<string> GetRecommendationAsync(string userId);
    }

    public class DiaryService(ICosmosDbService cosmosDbService, IEncryptionService encryptionService) : IDiaryService
    {
        private readonly ICosmosDbService _cosmosDbService = cosmosDbService;
        private readonly IEncryptionService _encryptionService = encryptionService;

        public async Task<DiaryEntry> CreateEntryAsync(DiaryEntry entry)
        {
            return await _cosmosDbService.CreateDiaryEntryAsync(entry);
        }

        public async Task<DiaryEntry?> GetEntryAsync(string id, string userId)
        {
            return await _cosmosDbService.GetDiaryEntryAsync(id, userId);
        }

        public async Task<IEnumerable<DiaryEntry>> GetEntriesAsync(string userId)
        {
            return await _cosmosDbService.GetDiaryEntriesAsync(userId);
        }

        public async Task<DiaryEntry> UpdateEntryAsync(DiaryEntry entry)
        {
            return await _cosmosDbService.UpdateDiaryEntryAsync(entry);
        }

        public async Task DeleteEntryAsync(string id, string userId)
        {
            await _cosmosDbService.DeleteDiaryEntryAsync(id, userId);
        }

        public async Task<string> GetRecommendationAsync(string userId)
        {
            var entries = await _cosmosDbService.GetDiaryEntriesAsync(userId);
            if (!entries.Any())
            {
                return "Start writing your first diary entry!";
            }

            // Simple recommendation based on the most recent entry
            var latestEntry = entries.OrderByDescending(e => e.Date).First();
            return $"Based on your latest entry from {latestEntry.Date:MMM dd, yyyy}, consider writing about your context: {latestEntry.Context ?? "not specified"}";
        }
    }
}