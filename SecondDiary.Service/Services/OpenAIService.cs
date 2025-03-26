using Azure;
using Azure.AI.OpenAI;
using Microsoft.Extensions.Options;
using OpenAI.Chat;
using SecondDiary.API.Models;
using System.Text;

namespace SecondDiary.API.Services
{
    public interface IOpenAIService
    {
        Task<string> GetRecommendationsAsync(string userId);
    }

    public class OpenAIService : IOpenAIService
    {
        private readonly AzureOpenAIClient _client;
        private readonly ChatClient _chatClient;
        private readonly string _deploymentName;
        private readonly ICosmosDbService _cosmosDbService;
        private readonly ISystemPromptService _systemPromptService;

        public OpenAIService(
            IOptions<OpenAISettings> settings,
            ICosmosDbService cosmosDbService,
            ISystemPromptService systemPromptService)
        {
            var openAISettings = settings.Value;
            
            // Updated to use OpenAIChatCompletionsClient for chat completions
            _client = new AzureOpenAIClient(
                new Uri(openAISettings.Endpoint), 
                new AzureKeyCredential(openAISettings.ApiKey));
            _chatClient = _client.GetChatClient(openAISettings.DeploymentName);
                
            _deploymentName = openAISettings.DeploymentName;
            _cosmosDbService = cosmosDbService;
            _systemPromptService = systemPromptService;
        }

        public async Task<string> GetRecommendationsAsync(string userId)
        {
            // Get user's diary entries
            IEnumerable<DiaryEntry> entries = await _cosmosDbService.GetEntriesAsync(userId);
            
            // Get the system prompt
            string systemPrompt = await _systemPromptService.GetSystemPromptAsync(userId);

            // Format the diary entries for the prompt
            StringBuilder entriesText = new StringBuilder();
            foreach (DiaryEntry entry in entries)
            {
                entriesText.AppendLine($"Date: {entry.Date}");
                if (!string.IsNullOrEmpty(entry.Thought))
                {
                    entriesText.AppendLine($"Thought: {entry.Thought}");
                }
                entriesText.AppendLine();
            }

            // Create the messages collection
            ChatMessage[] messages =
            {
                new SystemChatMessage(systemPrompt),
                new UserChatMessage($"Based on my diary entries, please provide me with thoughtful recommendations:\n\n{entriesText}")
            };

            // Use the new method to get chat completions
            System.ClientModel.ClientResult<ChatCompletion> result = await _chatClient.CompleteChatAsync(
                messages,
                new ChatCompletionOptions
                {
                    Temperature = 0.7f,
                    TopP = 1.0f,
                    FrequencyPenalty = 0.0f,
                    PresencePenalty = 0.0f
                });
            
            // Return the recommendation
            return result.Value.Content[0].Text;
        }
    }
}
