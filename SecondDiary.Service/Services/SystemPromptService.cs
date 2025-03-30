using SecondDiary.Models;

namespace SecondDiary.Services
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
        private readonly IEncryptionService _encryptionService;

        public SystemPromptService(ICosmosDbService cosmosDbService, IEncryptionService encryptionService)
        {
            _cosmosDbService = cosmosDbService;
            _encryptionService = encryptionService;
        }

        public async Task<string> GetSystemPromptAsync(string userId)
        {
            SystemPrompt systemPrompt = await GetOrCreatePromptAsync(userId);
            
            // Decrypt each line before returning
            List<string> decryptedLines = new List<string>();
            foreach (string encryptedLine in systemPrompt.Lines)
                decryptedLines.Add(_encryptionService.Decrypt(encryptedLine));
            
            return string.Join(Environment.NewLine, decryptedLines);
        }

        public async Task AddLineToPromptAsync(string userId, string line)
        {
            SystemPrompt systemPrompt = await GetOrCreatePromptAsync(userId);
            
            // Encrypt the new line before adding
            string encryptedLine = _encryptionService.Encrypt(line);
            systemPrompt.Lines.Add(encryptedLine);
            
            await _cosmosDbService.UpdateSystemPromptAsync(systemPrompt);
        }
        
        public async Task RemoveLineAsync(string userId, string line)
        {
            SystemPrompt systemPrompt = await GetOrCreatePromptAsync(userId);
            
            // Decrypt all lines to find the match
            for (int i = 0; i < systemPrompt.Lines.Count; i++)
            {
                string decryptedLine = _encryptionService.Decrypt(systemPrompt.Lines[i]);
                if (decryptedLine == line)
                {
                    systemPrompt.Lines.RemoveAt(i);
                    await _cosmosDbService.UpdateSystemPromptAsync(systemPrompt);
                    return;
                }
            }
        }

        private async Task<SystemPrompt> GetOrCreatePromptAsync(string userId)
        {
            string promptId = $"{userId}-systemprompt";
            SystemPrompt? systemPrompt = await _cosmosDbService.GetSystemPromptAsync(promptId, userId);
            
            if (systemPrompt == null)
            {
                // Create a new system prompt if one doesn't exist with encrypted default line
                string defaultLine = "You are a helpful AI assistant that provides thoughtful recommendations based on diary entries.";
                string encryptedDefaultLine = _encryptionService.Encrypt(defaultLine);
                
                systemPrompt = new SystemPrompt
                {
                    Id = promptId,
                    UserId = userId,
                    Lines = new List<string> { encryptedDefaultLine }
                };
                
                await _cosmosDbService.CreateSystemPromptAsync(systemPrompt);
            }
            
            return systemPrompt;
        }
    }
}
