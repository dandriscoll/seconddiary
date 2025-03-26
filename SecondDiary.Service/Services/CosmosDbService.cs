using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Options;
using SecondDiary.API.Models;
using System.Reflection;

namespace SecondDiary.API.Services
{
    public interface ICosmosDbService
    {
        Task<DiaryEntry> CreateEntryAsync(DiaryEntry entry);
        Task<DiaryEntry?> GetEntryAsync(string id, string userId);
        Task<IEnumerable<DiaryEntry>> GetEntriesAsync(string userId);
        Task<DiaryEntry> UpdateEntryAsync(DiaryEntry entry);
        Task DeleteEntryAsync(string id, string userId);

        // Generic methods for any type including SystemPrompt
        Task<T?> GetItemAsync<T>(string id, string userId) where T : class;
        Task<T> CreateItemAsync<T>(T item) where T : class;
        Task<T> UpdateItemAsync<T>(T item) where T : class;
    }

    public class CosmosDbService : ICosmosDbService
    {
        private readonly CosmosClient _cosmosClient;
        private readonly Container _container;
        private readonly IEncryptionService _encryptionService;

        public CosmosDbService(
            IOptions<CosmosDbSettings> cosmosDbSettings,
            IEncryptionService encryptionService)
        {
            CosmosDbSettings settings = cosmosDbSettings.Value;
            _cosmosClient = new CosmosClient(settings.Endpoint, settings.Key);
            _container = _cosmosClient.GetContainer(settings.DatabaseName, settings.ContainerName);
            _encryptionService = encryptionService;
        }

        public async Task<DiaryEntry> CreateEntryAsync(DiaryEntry entry)
        {
            // Encrypt the thought before storing (only if it's not null)
            entry.EncryptedThought = entry.Thought != null ? _encryptionService.Encrypt(entry.Thought) : null;
            entry.Thought = null; // Clear the plain text thought

            // Encrypt the context before storing (only if it's not null)
            entry.EncryptedContext = entry.Context != null ? _encryptionService.Encrypt(entry.Context) : null;
            entry.Context = null; // Clear the plain text context

            ItemResponse<DiaryEntry> response = await _container.CreateItemAsync(entry, new PartitionKey(entry.UserId));
            return response.Resource;
        }

        public async Task<DiaryEntry?> GetEntryAsync(string id, string userId)
        {
            try
            {
                ItemResponse<DiaryEntry> response = await _container.ReadItemAsync<DiaryEntry>(
                    id,
                    new PartitionKey(userId));

                DiaryEntry entry = response.Resource;
                // Decrypt the thought before returning (only if encrypted thought exists)
                if (entry.EncryptedThought != null)
                    entry.Thought = _encryptionService.Decrypt(entry.EncryptedThought);
                
                // Decrypt the context before returning (only if encrypted context exists)
                if (entry.EncryptedContext != null)
                    entry.Context = _encryptionService.Decrypt(entry.EncryptedContext);
                
                return entry;
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }
        }

        public async Task<IEnumerable<DiaryEntry>> GetEntriesAsync(string userId)
        {
            QueryDefinition query = new QueryDefinition("SELECT * FROM c WHERE c.userId = @userId")
                .WithParameter("@userId", userId);

            FeedIterator<DiaryEntry> iterator = _container.GetItemQueryIterator<DiaryEntry>(query);
            List<DiaryEntry> entries = new List<DiaryEntry>();

            while (iterator.HasMoreResults)
            {
                FeedResponse<DiaryEntry> response = await iterator.ReadNextAsync();
                foreach (DiaryEntry entry in response)
                {
                    // Decrypt the thought before returning (only if encrypted thought exists)
                    if (entry.EncryptedThought != null)
                        entry.Thought = _encryptionService.Decrypt(entry.EncryptedThought);
                    
                    // Decrypt the context before returning (only if encrypted context exists)
                    if (entry.EncryptedContext != null)
                        entry.Context = _encryptionService.Decrypt(entry.EncryptedContext);
                    
                    entries.Add(entry);
                }
            }

            return entries;
        }

        public async Task<DiaryEntry> UpdateEntryAsync(DiaryEntry entry)
        {
            // Encrypt the thought before storing (only if it's not null)
            entry.EncryptedThought = entry.Thought != null ? _encryptionService.Encrypt(entry.Thought) : null;
            entry.Thought = null; // Clear the plain text thought

            // Encrypt the context before storing (only if it's not null)
            entry.EncryptedContext = entry.Context != null ? _encryptionService.Encrypt(entry.Context) : null;
            entry.Context = null; // Clear the plain text context

            ItemResponse<DiaryEntry> response = await _container.UpsertItemAsync(entry, new PartitionKey(entry.UserId));
            return response.Resource;
        }

        public async Task DeleteEntryAsync(string id, string userId)
        {
            await _container.DeleteItemAsync<DiaryEntry>(
                id,
                new PartitionKey(userId));
        }

        // Generic methods implementation
        public async Task<T?> GetItemAsync<T>(string id, string userId) where T : class
        {
            try
            {
                ItemResponse<T> response = await _container.ReadItemAsync<T>(
                    id,
                    new PartitionKey(userId));
                
                return response.Resource;
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }
        }

        public async Task<T> CreateItemAsync<T>(T item) where T : class
        {
            string userId = GetUserIdFromItem(item);
            ItemResponse<T> response = await _container.CreateItemAsync(item, new PartitionKey(userId));
            return response.Resource;
        }

        public async Task<T> UpdateItemAsync<T>(T item) where T : class
        {
            string userId = GetUserIdFromItem(item);
            ItemResponse<T> response = await _container.UpsertItemAsync(item, new PartitionKey(userId));
            return response.Resource;
        }

        // Helper method to extract UserId from any item type
        private string GetUserIdFromItem<T>(T item) where T : class
        {
            PropertyInfo? userIdProperty = typeof(T).GetProperty("UserId");
            if (userIdProperty == null)
                throw new InvalidOperationException($"Type {typeof(T).Name} does not have a UserId property");
            
            string? userId = userIdProperty.GetValue(item)?.ToString();
            if (userId == null)
                throw new InvalidOperationException("UserId property cannot be null");
            
            return userId;
        }
    }
}