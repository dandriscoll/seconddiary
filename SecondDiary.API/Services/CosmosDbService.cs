using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Options;
using SecondDiary.API.Models;

namespace SecondDiary.API.Services
{
    public interface ICosmosDbService
    {
        Task<DiaryEntry> CreateEntryAsync(DiaryEntry entry);
        Task<DiaryEntry?> GetEntryAsync(string id, string userId);
        Task<IEnumerable<DiaryEntry>> GetEntriesAsync(string userId);
        Task<DiaryEntry> UpdateEntryAsync(DiaryEntry entry);
        Task DeleteEntryAsync(string id, string userId);
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
            var settings = cosmosDbSettings.Value;
            _cosmosClient = new CosmosClient(settings.Endpoint, settings.Key);
            _container = _cosmosClient.GetContainer(settings.DatabaseName, settings.ContainerName);
            _encryptionService = encryptionService;
        }

        public async Task<DiaryEntry> CreateEntryAsync(DiaryEntry entry)
        {
            // Encrypt the thought before storing
            entry.EncryptedThought = _encryptionService.Encrypt(entry.Thought);
            entry.Thought = null; // Clear the plain text thought

            var response = await _container.CreateItemAsync(entry, new PartitionKey(entry.UserId));
            return response.Resource;
        }

        public async Task<DiaryEntry?> GetEntryAsync(string id, string userId)
        {
            try
            {
                var response = await _container.ReadItemAsync<DiaryEntry>(
                    id,
                    new PartitionKey(userId));

                var entry = response.Resource;
                // Decrypt the thought before returning
                entry.Thought = _encryptionService.Decrypt(entry.EncryptedThought);
                return entry;
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }
        }

        public async Task<IEnumerable<DiaryEntry>> GetEntriesAsync(string userId)
        {
            var query = new QueryDefinition("SELECT * FROM c WHERE c.userId = @userId")
                .WithParameter("@userId", userId);

            var iterator = _container.GetItemQueryIterator<DiaryEntry>(query);
            var entries = new List<DiaryEntry>();

            while (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync();
                foreach (var entry in response)
                {
                    // Decrypt the thought before returning
                    entry.Thought = _encryptionService.Decrypt(entry.EncryptedThought);
                    entries.Add(entry);
                }
            }

            return entries;
        }

        public async Task<DiaryEntry> UpdateEntryAsync(DiaryEntry entry)
        {
            // Encrypt the thought before storing
            entry.EncryptedThought = _encryptionService.Encrypt(entry.Thought);
            entry.Thought = null; // Clear the plain text thought

            var response = await _container.UpsertItemAsync(entry, new PartitionKey(entry.UserId));
            return response.Resource;
        }

        public async Task DeleteEntryAsync(string id, string userId)
        {
            await _container.DeleteItemAsync<DiaryEntry>(
                id,
                new PartitionKey(userId));
        }
    }
} 