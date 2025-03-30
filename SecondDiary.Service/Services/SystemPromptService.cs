using SecondDiary.Models;

namespace SecondDiary.Services
{
    public interface ISystemPromptService
    {
        Task<string> GetSystemPromptAsync(string userId);
        Task AddLineToPromptAsync(string userId, string line);
        Task RemoveLineAsync(string userId, string line);
    }

    public class SystemPromptService(ICosmosDbService cosmosDbService) : ISystemPromptService
    {
        private readonly ICosmosDbService _cosmosDbService = cosmosDbService;

        public async Task<string> GetSystemPromptAsync(string userId)
        {
            SystemPrompt systemPrompt = await GetOrCreatePromptAsync(userId);
            return string.Join(Environment.NewLine, systemPrompt.Lines);
        }

        public async Task AddLineToPromptAsync(string userId, string line)
        {
            SystemPrompt systemPrompt = await GetOrCreatePromptAsync(userId);
            systemPrompt.Lines.Add(line);
            await _cosmosDbService.UpdateSystemPromptAsync(systemPrompt);
        }
        
        public async Task RemoveLineAsync(string userId, string line)
        {
            SystemPrompt systemPrompt = await GetOrCreatePromptAsync(userId);
            systemPrompt.Lines.Remove(line);
            await _cosmosDbService.UpdateSystemPromptAsync(systemPrompt);
        }

        private async Task<SystemPrompt> GetOrCreatePromptAsync(string userId)
        {
            string promptId = $"{userId}-systemprompt";
            SystemPrompt? systemPrompt = await _cosmosDbService.GetSystemPromptAsync(promptId, userId);
            
            if (systemPrompt == null)
            {
                // Create a new system prompt if one doesn't exist
                systemPrompt = new SystemPrompt
                {
                    Id = promptId,
                    UserId = userId,
                    Lines = new List<string> { "You are a helpful AI assistant that provides thoughtful recommendations based on diary entries." }
                };
                await _cosmosDbService.CreateSystemPromptAsync(systemPrompt);
            }
            
            return systemPrompt;
        }
    }
}