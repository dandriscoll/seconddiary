using SecondDiary.API.Models;

namespace SecondDiary.API.Services
{
    public interface ISystemPromptService
    {
        Task<string> GetSystemPromptAsync(string userId);
        Task AddLineToPromptAsync(string userId, string line);
        Task RemoveLineAsync(string userId, string line);
    }

    public class SystemPromptService : ISystemPromptService
    {
        private readonly ICosmosDbService _cosmosDbService;
        private const string DefaultSystemPromptLine = "You are a helpful AI assistant that provides thoughtful recommendations based on diary entries.";

        public SystemPromptService(ICosmosDbService cosmosDbService)
        {
            _cosmosDbService = cosmosDbService;
        }

        public async Task<string> GetSystemPromptAsync(string userId)
        {
            SystemPrompt systemPrompt = await GetOrCreatePromptAsync(userId);
            return string.Join(Environment.NewLine, systemPrompt.PromptLines);
        }

        public async Task AddLineToPromptAsync(string userId, string line)
        {
            SystemPrompt systemPrompt = await GetOrCreatePromptAsync(userId);
            systemPrompt.PromptLines.Add(line);
            await _cosmosDbService.UpdateItemAsync(systemPrompt);
        }
        
        public async Task RemoveLineAsync(string userId, string line)
        {
            SystemPrompt systemPrompt = await GetOrCreatePromptAsync(userId);
            systemPrompt.PromptLines.Remove(line);
            
            // Make sure we have at least one line in the prompt
            if (systemPrompt.PromptLines.Count == 0)
                systemPrompt.PromptLines.Add(DefaultSystemPromptLine);
            
            await _cosmosDbService.UpdateItemAsync(systemPrompt);
        }

        private async Task<SystemPrompt> GetOrCreatePromptAsync(string userId)
        {
            string promptId = $"{userId}-systemprompt";
            SystemPrompt? systemPrompt = await _cosmosDbService.GetItemAsync<SystemPrompt>(promptId, userId);
            
            if (systemPrompt == null)
            {
                // Create a new system prompt if one doesn't exist
                systemPrompt = new SystemPrompt
                {
                    Id = promptId,
                    UserId = userId,
                    PromptLines = new List<string> { DefaultSystemPromptLine }
                };
                await _cosmosDbService.CreateItemAsync(systemPrompt);
            }
            
            return systemPrompt;
        }
    }
}