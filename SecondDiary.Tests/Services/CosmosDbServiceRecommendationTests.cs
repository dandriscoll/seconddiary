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
    public class CosmosDbServiceRecommendationTests
    {
        private readonly Mock<CosmosClient> _mockCosmosClient;
        private readonly Mock<Container> _mockContainer;
        private readonly Mock<Database> _mockDatabase;
        private readonly Mock<IEncryptionService> _mockEncryptionService;
        private readonly Mock<ILogger<CosmosDbService>> _mockLogger;
        private readonly Mock<IOptions<CosmosDbSettings>> _mockOptions;
        private readonly string _testUserId = "test-user";

        public CosmosDbServiceRecommendationTests()
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
                EmailSettingsContainerName = "email-settings",
                RecommendationsContainerName = "recommendations"
            });

            // Setup cosmosClient to return database
            _mockCosmosClient
                .Setup(c => c.GetDatabase(It.IsAny<string>()))
                .Returns(_mockDatabase.Object);

            // Setup database to return container
            _mockCosmosClient
                .Setup(c => c.GetContainer(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(_mockContainer.Object);
                
            // Setup container database ID property
            _mockContainer
                .Setup(c => c.Database)
                .Returns(_mockDatabase.Object);
            
            _mockDatabase
                .Setup(d => d.Id)
                .Returns("test-db");
        }

        [Fact]
        public async Task CreateRecommendationAsync_ShouldCreateRecommendation()
        {
            // Arrange
            Recommendation testRecommendation = new Recommendation
            {
                Id = Guid.NewGuid().ToString(),
                UserId = _testUserId,
                Text = "Test recommendation text",
                Date = DateTimeOffset.Now
            };

            Mock<ItemResponse<Recommendation>> mockResponse = new Mock<ItemResponse<Recommendation>>();
            mockResponse.Setup(r => r.Resource).Returns(testRecommendation);

            _mockContainer
                .Setup(c => c.CreateItemAsync(
                    It.IsAny<Recommendation>(),
                    It.IsAny<PartitionKey>(),
                    It.IsAny<ItemRequestOptions>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockResponse.Object);

            CosmosDbService service = new CosmosDbService(
                _mockOptions.Object,
                _mockEncryptionService.Object,
                _mockLogger.Object,
                _mockCosmosClient.Object);

            // Act
            Recommendation result = await service.CreateRecommendationAsync(testRecommendation);

            // Assert
            Assert.Equal(testRecommendation.Id, result.Id);
            Assert.Equal(testRecommendation.UserId, result.UserId);
            Assert.Equal(testRecommendation.Text, result.Text);
            
            _mockContainer.Verify(c => c.CreateItemAsync(
                It.Is<Recommendation>(r => r.Id == testRecommendation.Id && 
                                          r.UserId == testRecommendation.UserId && 
                                          r.Text == testRecommendation.Text),
                It.Is<PartitionKey>(pk => pk.ToString().Contains(testRecommendation.UserId)),
                It.IsAny<ItemRequestOptions>(),
                It.IsAny<CancellationToken>()), 
                Times.Once);
        }

        [Fact]
        public async Task GetRecentRecommendationsAsync_ShouldReturnRecommendations()
        {
            // Arrange
            List<Recommendation> testRecommendations = new List<Recommendation>
            {
                new Recommendation { Id = "1", UserId = _testUserId, Text = "Recommendation 1", Date = DateTimeOffset.Now.AddDays(-3) },
                new Recommendation { Id = "2", UserId = _testUserId, Text = "Recommendation 2", Date = DateTimeOffset.Now.AddDays(-2) },
                new Recommendation { Id = "3", UserId = _testUserId, Text = "Recommendation 3", Date = DateTimeOffset.Now.AddDays(-1) }
            };

            Mock<FeedResponse<Recommendation>> mockFeedResponse = new Mock<FeedResponse<Recommendation>>();
            mockFeedResponse
                .Setup(r => r.GetEnumerator())
                .Returns(testRecommendations.GetEnumerator());

            Mock<FeedIterator<Recommendation>> mockFeedIterator = new Mock<FeedIterator<Recommendation>>();
            mockFeedIterator
                .SetupSequence(fi => fi.HasMoreResults)
                .Returns(true)
                .Returns(false);
            mockFeedIterator
                .Setup(fi => fi.ReadNextAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockFeedResponse.Object);

            _mockContainer
                .Setup(c => c.GetItemQueryIterator<Recommendation>(
                    It.IsAny<QueryDefinition>(), 
                    It.IsAny<string>(), 
                    It.IsAny<QueryRequestOptions>()))
                .Returns(mockFeedIterator.Object);

            CosmosDbService service = new CosmosDbService(
                _mockOptions.Object,
                _mockEncryptionService.Object,
                _mockLogger.Object,
                _mockCosmosClient.Object);

            // Act
            IEnumerable<Recommendation> results = await service.GetRecentRecommendationsAsync(_testUserId, 5);
            List<Recommendation> resultsList = results.ToList();

            // Assert
            Assert.Equal(testRecommendations.Count, resultsList.Count);
            for (int i = 0; i < testRecommendations.Count; i++)
            {
                Assert.Equal(testRecommendations[i].Id, resultsList[i].Id);
                Assert.Equal(testRecommendations[i].UserId, resultsList[i].UserId);
                Assert.Equal(testRecommendations[i].Text, resultsList[i].Text);
            }
            _mockContainer.Verify(c => c.GetItemQueryIterator<Recommendation>(
                It.IsAny<QueryDefinition>(),
                It.IsAny<string>(),
                It.Is<QueryRequestOptions>(qro => qro.PartitionKey != null && qro.PartitionKey.ToString()!.Contains(_testUserId))),
                Times.Once);
        }
    }
}
