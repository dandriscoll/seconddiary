using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SecondDiary.API.Models;

namespace SecondDiary.API.Services
{
    public class DiaryService
    {
        private readonly List<DiaryEntry> _entries = new List<DiaryEntry>();

        public Task<DiaryEntry> CreateEntryAsync(string userId, string? context, string thought)
        {
            DiaryEntry entry = new DiaryEntry
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Context = context,
                Thought = thought,
                CreatedAt = DateTime.UtcNow
            };

            _entries.Add(entry);
            return Task.FromResult(entry);
        }

        public Task<string> GetRecommendationAsync(string userId)
        {
            // In a real implementation, this would use an LLM service
            return Task.FromResult("Based on your diary entries, I recommend taking some time for self-reflection and meditation.");
        }
    }
} 