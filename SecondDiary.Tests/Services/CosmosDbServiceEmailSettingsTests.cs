using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using SecondDiary.Models;
using SecondDiary.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace SecondDiary.Tests.Services
{
    public class CosmosDbServiceEmailSettingsTests
    {
        private readonly Mock<CosmosClient> _mockCosmosClient;
        private readonly Mock<Container> _mockContainer;
        private readonly Mock<Database> _mockDatabase;
        private readonly Mock<IEncryptionService> _mockEncryptionService;
        private readonly Mock<ILogger<CosmosDbService>> _mockLogger;
        private readonly Mock<IOptions<CosmosDbSettings>> _mockOptions;
        private readonly string _testUserId = "test-user";
        private readonly string _testEmail = "test@example.com";

        public CosmosDbServiceEmailSettingsTests()
        {
            _mockCosmosClient = new Mock<CosmosClient>();
            _mockContainer = new Mock<Container>();
            _mockDatabase = new Mock<Database>();
            _mockEncryptionService = new Mock<IEncryptionService>();
            _mockLogger = new Mock<ILogger<CosmosDbService>>();
            _mockOptions = new Mock<IOptions<CosmosDbSettings>>();

            // Setup options
            _mockOptions.Setup(o => o.Value).Returns(new CosmosDbSettings
            {
                Endpoint = "https://test.documents.azure.com:443/",
                Key = "test-key",
                DatabaseName = "test-db",
                DiaryEntriesContainerName = "diary-entries",
                SystemPromptsContainerName = "system-prompts",
                EmailSettingsContainerName = "email-settings"
            });

            // Setup cosmosClient to return database
            _mockCosmosClient
                .Setup(c => c.GetDatabase(It.IsAny<string>()))
                .Returns(_mockDatabase.Object);

            // Setup database to return containers
            _mockCosmosClient
                .Setup(c => c.GetContainer(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(_mockContainer.Object);
        }

        [Fact]
        public async Task CreateEmailSettingsAsync_CreatesSettingsInContainer()
        {
            // Arrange
            var settings = new EmailSettings
            {
                Id = "test-id",
                UserId = _testUserId,
                Email = _testEmail,
                PreferredTime = new TimeSpan(9, 0, 0),
                IsEnabled = true
            };

            var mockResponse = new Mock<ItemResponse<EmailSettings>>();
            mockResponse.Setup(r => r.Resource).Returns(settings);

            _mockContainer
                .Setup(c => c.CreateItemAsync(
                    It.IsAny<EmailSettings>(),
                    It.IsAny<PartitionKey>(),
                    It.IsAny<ItemRequestOptions>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockResponse.Object);

            var service = CreateCosmosDbService();

            // Act
            var result = await service.CreateEmailSettingsAsync(settings);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(settings.Id, result.Id);
            Assert.Equal(settings.UserId, result.UserId);
            Assert.Equal(settings.Email, result.Email);
            Assert.Equal(settings.PreferredTime, result.PreferredTime);
            Assert.Equal(settings.IsEnabled, result.IsEnabled);

            _mockContainer.Verify(c => c.CreateItemAsync(
                It.Is<EmailSettings>(s => s.Id == settings.Id),
                It.Is<PartitionKey>(p => p.ToString() == $"[\"{_testUserId}\"]"),
                It.IsAny<ItemRequestOptions>(),
                It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task GetEmailSettingsAsync_ReturnsSettings_WhenFound()
        {
            // Arrange
            var settings = new EmailSettings
            {
                Id = "test-id",
                UserId = _testUserId,
                Email = _testEmail,
                PreferredTime = new TimeSpan(9, 0, 0),
                IsEnabled = true
            };

            var mockFeedResponse = new Mock<FeedResponse<EmailSettings>>();
            mockFeedResponse.Setup(r => r.GetEnumerator()).Returns(new List<EmailSettings> { settings }.GetEnumerator());

            var mockFeedIterator = new Mock<FeedIterator<EmailSettings>>();
            mockFeedIterator.SetupSequence(i => i.HasMoreResults)
                .Returns(true)
                .Returns(false);
            mockFeedIterator.Setup(i => i.ReadNextAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockFeedResponse.Object);

            _mockContainer
                .Setup(c => c.GetItemQueryIterator<EmailSettings>(
                    It.IsAny<QueryDefinition>(),
                    It.IsAny<string>(),
                    It.IsAny<QueryRequestOptions>()))
                .Returns(mockFeedIterator.Object);

            var service = CreateCosmosDbService();

            // Act
            var result = await service.GetEmailSettingsAsync(_testUserId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(settings.Id, result.Id);
            Assert.Equal(settings.UserId, result.UserId);
            Assert.Equal(settings.Email, result.Email);
            Assert.Equal(settings.PreferredTime, result.PreferredTime);
            Assert.Equal(settings.IsEnabled, result.IsEnabled);
        }

        [Fact]
        public async Task GetEmailSettingsAsync_ReturnsNull_WhenNotFound()
        {
            // Arrange
            var emptyList = new List<EmailSettings>();
            var mockFeedResponse = new Mock<FeedResponse<EmailSettings>>();
            mockFeedResponse.Setup(r => r.GetEnumerator()).Returns(emptyList.GetEnumerator());

            var mockFeedIterator = new Mock<FeedIterator<EmailSettings>>();
            mockFeedIterator.SetupSequence(i => i.HasMoreResults)
                .Returns(true)
                .Returns(false);
            mockFeedIterator.Setup(i => i.ReadNextAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockFeedResponse.Object);

            _mockContainer
                .Setup(c => c.GetItemQueryIterator<EmailSettings>(
                    It.IsAny<QueryDefinition>(),
                    It.IsAny<string>(),
                    It.IsAny<QueryRequestOptions>()))
                .Returns(mockFeedIterator.Object);

            var service = CreateCosmosDbService();

            // Act
            var result = await service.GetEmailSettingsAsync(_testUserId);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task UpdateEmailSettingsAsync_UpdatesSettingsInContainer()
        {
            // Arrange
            var settings = new EmailSettings
            {
                Id = "test-id",
                UserId = _testUserId,
                Email = _testEmail,
                PreferredTime = new TimeSpan(9, 0, 0),
                IsEnabled = true
            };

            var mockResponse = new Mock<ItemResponse<EmailSettings>>();
            mockResponse.Setup(r => r.Resource).Returns(settings);

            _mockContainer
                .Setup(c => c.UpsertItemAsync(
                    It.IsAny<EmailSettings>(),
                    It.IsAny<PartitionKey>(),
                    It.IsAny<ItemRequestOptions>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockResponse.Object);

            var service = CreateCosmosDbService();

            // Act
            var result = await service.UpdateEmailSettingsAsync(settings);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(settings.Id, result.Id);
            Assert.Equal(settings.Email, result.Email);
            Assert.Equal(settings.PreferredTime, result.PreferredTime);

            _mockContainer.Verify(c => c.UpsertItemAsync(
                It.Is<EmailSettings>(s => s.Id == settings.Id),
                It.Is<PartitionKey>(p => p.ToString() == $"[\"{_testUserId}\"]"),
                It.IsAny<ItemRequestOptions>(),
                It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task DeleteEmailSettingsAsync_DeletesSettings_WhenFound()
        {
            // Arrange
            var settings = new EmailSettings
            {
                Id = "test-id",
                UserId = _testUserId,
                Email = _testEmail
            };

            var mockFeedResponse = new Mock<FeedResponse<EmailSettings>>();
            mockFeedResponse.Setup(r => r.GetEnumerator()).Returns(new List<EmailSettings> { settings }.GetEnumerator());

            var mockFeedIterator = new Mock<FeedIterator<EmailSettings>>();
            mockFeedIterator.SetupSequence(i => i.HasMoreResults)
                .Returns(true)
                .Returns(false);
            mockFeedIterator.Setup(i => i.ReadNextAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockFeedResponse.Object);

            _mockContainer
                .Setup(c => c.GetItemQueryIterator<EmailSettings>(
                    It.IsAny<QueryDefinition>(),
                    It.IsAny<string>(),
                    It.IsAny<QueryRequestOptions>()))
                .Returns(mockFeedIterator.Object);

            var mockDeleteResponse = new Mock<ItemResponse<EmailSettings>>();
            _mockContainer
                .Setup(c => c.DeleteItemAsync<EmailSettings>(
                    It.IsAny<string>(),
                    It.IsAny<PartitionKey>(),
                    It.IsAny<ItemRequestOptions>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockDeleteResponse.Object);

            var service = CreateCosmosDbService();

            // Act
            await service.DeleteEmailSettingsAsync(_testUserId);

            // Assert
            _mockContainer.Verify(c => c.DeleteItemAsync<EmailSettings>(
                settings.Id,
                It.Is<PartitionKey>(p => p.ToString() == $"[\"{_testUserId}\"]"),
                It.IsAny<ItemRequestOptions>(),
                It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task DeleteEmailSettingsAsync_DoesNothing_WhenSettingsNotFound()
        {
            // Arrange
            var emptyList = new List<EmailSettings>();
            var mockFeedResponse = new Mock<FeedResponse<EmailSettings>>();
            mockFeedResponse.Setup(r => r.GetEnumerator()).Returns(emptyList.GetEnumerator());

            var mockFeedIterator = new Mock<FeedIterator<EmailSettings>>();
            mockFeedIterator.SetupSequence(i => i.HasMoreResults)
                .Returns(true)
                .Returns(false);
            mockFeedIterator.Setup(i => i.ReadNextAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockFeedResponse.Object);

            _mockContainer
                .Setup(c => c.GetItemQueryIterator<EmailSettings>(
                    It.IsAny<QueryDefinition>(),
                    It.IsAny<string>(),
                    It.IsAny<QueryRequestOptions>()))
                .Returns(mockFeedIterator.Object);

            var service = CreateCosmosDbService();

            // Act
            await service.DeleteEmailSettingsAsync(_testUserId);

            // Assert
            _mockContainer.Verify(c => c.DeleteItemAsync<EmailSettings>(
                It.IsAny<string>(),
                It.IsAny<PartitionKey>(),
                It.IsAny<ItemRequestOptions>(),
                It.IsAny<CancellationToken>()),
                Times.Never);
        }

        private CosmosDbService CreateCosmosDbService()
        {
            // Use reflection to create a CosmosDbService with our mocks
            // This is a workaround because the CosmosDbService constructor directly initializes cosmos client
            var service = new CosmosDbService(
                _mockOptions.Object,
                _mockEncryptionService.Object,
                _mockLogger.Object);

            // Use reflection to set the private fields
            var type = typeof(CosmosDbService);
            
            // Set _cosmosClient
            var cosmosClientField = type.GetField("_cosmosClient", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            cosmosClientField?.SetValue(service, _mockCosmosClient.Object);
            
            // Set _emailSettingsContainer
            var containerField = type.GetField("_emailSettingsContainer", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            containerField?.SetValue(service, _mockContainer.Object);

            return service;
        }
    }
}
