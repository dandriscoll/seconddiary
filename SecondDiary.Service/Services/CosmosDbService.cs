using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Options;
using SecondDiary.Models;
using System.Reflection;

namespace SecondDiary.Services
{
    public class CosmosDbService : ICosmosDbService
    {
        private readonly CosmosClient _cosmosClient;
        private readonly Container _diaryContainer;
        private readonly Container _promptContainer;
        private readonly IEncryptionService _encryptionService;
        private readonly ILogger<CosmosDbService> _logger;

        public CosmosDbService(
            IOptions<CosmosDbSettings> cosmosDbSettings,
            IEncryptionService encryptionService,
            ILogger<CosmosDbService> logger)
        {
            CosmosDbSettings settings = cosmosDbSettings.Value;
            _cosmosClient = new CosmosClient(settings.Endpoint, settings.Key);
            _diaryContainer = _cosmosClient.GetContainer(settings.DatabaseName, settings.DiaryEntriesContainerName);
            _promptContainer = _cosmosClient.GetContainer(settings.DatabaseName, settings.SystemPromptsContainerName);
            _encryptionService = encryptionService;
            _logger = logger;
        }

        public async Task InitializeAsync()
        {
            _logger.LogInformation("Initializing Cosmos DB databases and containers");

            try
            {
                DatabaseResponse databaseResponse = await _cosmosClient.CreateDatabaseIfNotExistsAsync(_diaryContainer.Database.Id, 400);
                Database database = databaseResponse.Database;
                _logger.LogInformation($"Database {databaseResponse.Database.Id} initialized");

                await database.CreateContainerIfNotExistsAsync(_diaryContainer.Id, "/UserId");
                _logger.LogInformation($"Container {_diaryContainer.Id} initialized");

                await database.CreateContainerIfNotExistsAsync(_promptContainer.Id, "/UserId");
                _logger.LogInformation($"Container {_promptContainer.Id} initialized");

                _logger.LogInformation("Cosmos DB initialization completed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing Cosmos DB");
                throw;
            }
        }

        public async Task<DiaryEntry> CreateDiaryEntryAsync(DiaryEntry entry)
        {
            entry.EncryptedThought = entry.Thought != null ? _encryptionService.Encrypt(entry.Thought) : null;
            entry.Thought = null;

            entry.EncryptedContext = entry.Context != null ? _encryptionService.Encrypt(entry.Context) : null;
            entry.Context = null;

            ItemResponse<DiaryEntry> response = await _diaryContainer.CreateItemAsync(entry, new PartitionKey(entry.UserId));
            return response.Resource;
        }

        public async Task<DiaryEntry?> GetDiaryEntryAsync(string id, string userId)
        {
            try
            {
                ItemResponse<DiaryEntry> response = await _diaryContainer.ReadItemAsync<DiaryEntry>(id, new PartitionKey(userId));
                DiaryEntry entry = response.Resource;

                if (entry.EncryptedThought != null)
                    entry.Thought = _encryptionService.Decrypt(entry.EncryptedThought);

                if (entry.EncryptedContext != null)
                    entry.Context = _encryptionService.Decrypt(entry.EncryptedContext);

                return entry;
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }
        }

        public async Task<IEnumerable<DiaryEntry>> GetDiaryEntriesAsync(string userId)
        {
            QueryDefinition query = new QueryDefinition("SELECT * FROM c WHERE c.userId = @userId")
                .WithParameter("@userId", userId);

            FeedIterator<DiaryEntry> iterator = _diaryContainer.GetItemQueryIterator<DiaryEntry>(query);
            List<DiaryEntry> entries = new List<DiaryEntry>();

            while (iterator.HasMoreResults)
            {
                FeedResponse<DiaryEntry> response = await iterator.ReadNextAsync();
                foreach (DiaryEntry entry in response)
                {
                    if (entry.EncryptedThought != null)
                        entry.Thought = _encryptionService.Decrypt(entry.EncryptedThought);

                    if (entry.EncryptedContext != null)
                        entry.Context = _encryptionService.Decrypt(entry.EncryptedContext);

                    entries.Add(entry);
                }
            }

            return entries;
        }

        public async Task<DiaryEntry> UpdateDiaryEntryAsync(DiaryEntry entry)
        {
            entry.EncryptedThought = entry.Thought != null ? _encryptionService.Encrypt(entry.Thought) : null;
            entry.Thought = null;

            entry.EncryptedContext = entry.Context != null ? _encryptionService.Encrypt(entry.Context) : null;
            entry.Context = null;

            ItemResponse<DiaryEntry> response = await _diaryContainer.UpsertItemAsync(entry, new PartitionKey(entry.UserId));
            return response.Resource;
        }

        public async Task DeleteDiaryEntryAsync(string id, string userId)
        {
            await _diaryContainer.DeleteItemAsync<DiaryEntry>(id, new PartitionKey(userId));
        }

        public async Task<SystemPrompt> CreateSystemPromptAsync(SystemPrompt prompt)
        {
            ItemResponse<SystemPrompt> response = await _promptContainer.CreateItemAsync(prompt, new PartitionKey(prompt.UserId));
            return response.Resource;
        }

        public async Task<SystemPrompt?> GetSystemPromptAsync(string id, string userId)
        {
            try
            {
                ItemResponse<SystemPrompt> response = await _promptContainer.ReadItemAsync<SystemPrompt>(id, new PartitionKey(userId));
                return response.Resource;
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }
        }

        public async Task<IEnumerable<SystemPrompt>> GetSystemPromptsAsync(string userId)
        {
            QueryDefinition query = new QueryDefinition("SELECT * FROM c WHERE c.userId = @userId")
                .WithParameter("@userId", userId);

            FeedIterator<SystemPrompt> iterator = _promptContainer.GetItemQueryIterator<SystemPrompt>(query);
            List<SystemPrompt> prompts = new List<SystemPrompt>();

            while (iterator.HasMoreResults)
            {
                FeedResponse<SystemPrompt> response = await iterator.ReadNextAsync();
                prompts.AddRange(response);
            }

            return prompts;
        }

        public async Task<SystemPrompt> UpdateSystemPromptAsync(SystemPrompt prompt)
        {
            ItemResponse<SystemPrompt> response = await _promptContainer.UpsertItemAsync(prompt, new PartitionKey(prompt.UserId));
            return response.Resource;
        }

        public async Task DeleteSystemPromptAsync(string id, string userId)
        {
            await _promptContainer.DeleteItemAsync<SystemPrompt>(id, new PartitionKey(userId));
        }
    }
}