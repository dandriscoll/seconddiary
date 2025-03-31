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
        private readonly Container _emailSettingsContainer;
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
            _emailSettingsContainer = _cosmosClient.GetContainer(settings.DatabaseName, settings.EmailSettingsContainerName);
            _encryptionService = encryptionService;
            _logger = logger;
        }

        public async Task InitializeAsync()
        {
            _logger.LogInformation("Initializing Cosmos DB databases and containers");

            try
            {
                DatabaseResponse databaseResponse = await _cosmosClient.CreateDatabaseIfNotExistsAsync(_diaryContainer.Database.Id);
                Database database = databaseResponse.Database;
                _logger.LogInformation($"Database {databaseResponse.Database.Id} initialized");

                await database.CreateContainerIfNotExistsAsync(_diaryContainer.Id, "/userId");
                _logger.LogInformation($"Container {_diaryContainer.Id} initialized");

                await database.CreateContainerIfNotExistsAsync(_promptContainer.Id, "/userId");
                _logger.LogInformation($"Container {_promptContainer.Id} initialized");

                await database.CreateContainerIfNotExistsAsync(_emailSettingsContainer.Id, "/userId");
                _logger.LogInformation($"Container {_emailSettingsContainer.Id} initialized");

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
            // Create a new entry to avoid modifying the input object
            DiaryEntry entryToSave = new DiaryEntry
            {
                Id = entry.Id,
                UserId = entry.UserId,
                Date = entry.Date,
                Thought = entry.Thought != null ? _encryptionService.Encrypt(entry.Thought) : null,
                Context = entry.Context != null ? _encryptionService.Encrypt(entry.Context) : null
            };

            ItemResponse<DiaryEntry> response = await _diaryContainer.CreateItemAsync(entryToSave, new PartitionKey(entryToSave.UserId));
            
            // Return the original entry for the user
            return entry;
        }

        public async Task<DiaryEntry?> GetDiaryEntryAsync(string id, string userId)
        {
            try
            {
                ItemResponse<DiaryEntry> response = await _diaryContainer.ReadItemAsync<DiaryEntry>(id, new PartitionKey(userId));
                DiaryEntry entry = response.Resource;

                // Decrypt the thought and context
                if (entry.Thought != null)
                    entry.Thought = _encryptionService.Decrypt(entry.Thought);

                if (entry.Context != null)
                    entry.Context = _encryptionService.Decrypt(entry.Context);

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
                    // Decrypt the thought and context
                    if (entry.Thought != null)
                        entry.Thought = _encryptionService.Decrypt(entry.Thought);

                    if (entry.Context != null)
                        entry.Context = _encryptionService.Decrypt(entry.Context);

                    entries.Add(entry);
                }
            }

            return entries;
        }

        public async Task<DiaryEntry> UpdateDiaryEntryAsync(DiaryEntry entry)
        {
            // Create a clone of the entry with encrypted values
            DiaryEntry entryToSave = new DiaryEntry
            {
                Id = entry.Id,
                UserId = entry.UserId,
                Date = entry.Date,
                Thought = entry.Thought != null ? _encryptionService.Encrypt(entry.Thought) : null,
                Context = entry.Context != null ? _encryptionService.Encrypt(entry.Context) : null
            };

            ItemResponse<DiaryEntry> response = await _diaryContainer.UpsertItemAsync(entryToSave, new PartitionKey(entry.UserId));
            
            // Return the original entry with unencrypted values
            return entry;
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

        #region EmailSettings

        public async Task<EmailSettings> CreateEmailSettingsAsync(EmailSettings settings)
        {
            ItemResponse<EmailSettings> response = await _emailSettingsContainer.CreateItemAsync(settings, new PartitionKey(settings.UserId));
            return response.Resource;
        }

        public async Task<EmailSettings?> GetEmailSettingsAsync(string userId)
        {
            try
            {
                // Use a query to find the email settings for the user - we're using userId as the id
                QueryDefinition query = new QueryDefinition("SELECT * FROM c WHERE c.userId = @userId")
                    .WithParameter("@userId", userId);

                FeedIterator<EmailSettings> iterator = _emailSettingsContainer.GetItemQueryIterator<EmailSettings>(query);
                
                while (iterator.HasMoreResults)
                {
                    FeedResponse<EmailSettings> response = await iterator.ReadNextAsync();
                    return response.FirstOrDefault();
                }

                return null;
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }
        }

        public async Task<IEnumerable<EmailSettings>> GetAllEmailSettingsAsync()
        {
            List<EmailSettings> allSettings = new List<EmailSettings>();
            
            try
            {
                QueryDefinition query = new QueryDefinition("SELECT * FROM c");
                FeedIterator<EmailSettings> iterator = _emailSettingsContainer.GetItemQueryIterator<EmailSettings>(query);
                
                while (iterator.HasMoreResults)
                {
                    FeedResponse<EmailSettings> response = await iterator.ReadNextAsync();
                    allSettings.AddRange(response);
                }

                return allSettings;
            }
            catch (CosmosException ex)
            {
                _logger.LogError(ex, "Error retrieving all email settings");
                return allSettings;
            }
        }

        public async Task<EmailSettings> UpdateEmailSettingsAsync(EmailSettings settings)
        {
            ItemResponse<EmailSettings> response = await _emailSettingsContainer.UpsertItemAsync(settings, new PartitionKey(settings.UserId));
            return response.Resource;
        }

        public async Task DeleteEmailSettingsAsync(string userId)
        {
            // First get the settings to get the ID
            EmailSettings? settings = await GetEmailSettingsAsync(userId);
            if (settings != null)
            {
                await _emailSettingsContainer.DeleteItemAsync<EmailSettings>(settings.Id, new PartitionKey(userId));
            }
        }

        #endregion
    }
}