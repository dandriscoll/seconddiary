using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using SecondDiary.Models;
using SecondDiary.Services;
using Xunit;

namespace SecondDiary.Tests.Services
{
    public class CosmosDbServicePersonalAccessTokenTests
    {
        private readonly Mock<Container> _mockContainer;
        private readonly Mock<CosmosClient> _mockCosmosClient;
        private readonly Mock<Database> _mockDatabase;
        private readonly CosmosDbService _service;
        private readonly CosmosDbSettings _settings;
        private const string TestUserId = "test-user-123";

        public CosmosDbServicePersonalAccessTokenTests()
        {
            _mockContainer = new Mock<Container>();
            _mockCosmosClient = new Mock<CosmosClient>();
            _mockDatabase = new Mock<Database>();
            
            _settings = new CosmosDbSettings
            {
                Endpoint = "https://test.documents.azure.com:443/",
                Key = "test-key",
                DatabaseName = "TestDatabase",
                DiaryEntriesContainerName = "TestDiaryContainer",
                SystemPromptsContainerName = "TestPromptsContainer", 
                EmailSettingsContainerName = "TestEmailContainer",
                RecommendationsContainerName = "TestRecommendationsContainer",
                PersonalAccessTokensContainerName = "TestPATContainer"
            };

            _mockCosmosClient
                .Setup(c => c.GetDatabase(_settings.DatabaseName))
                .Returns(_mockDatabase.Object);

            _mockCosmosClient
                .Setup(c => c.GetContainer(_settings.DatabaseName, It.IsAny<string>()))
                .Returns(_mockContainer.Object);

            _mockDatabase
                .Setup(d => d.GetContainer(It.IsAny<string>()))
                .Returns(_mockContainer.Object);

            var mockOptions = new Mock<IOptions<CosmosDbSettings>>();
            mockOptions.Setup(o => o.Value).Returns(_settings);
            
            var mockEncryption = new Mock<IEncryptionService>();
            var mockLogger = new Mock<ILogger<CosmosDbService>>();

            _service = new CosmosDbService(mockOptions.Object, mockEncryption.Object, mockLogger.Object, _mockCosmosClient.Object);
        }

        [Fact]
        public async Task CreatePersonalAccessTokenAsync_ShouldCallCreateItemAsync()
        {
            // Arrange
            var pat = new PersonalAccessToken
            {
                Id = "test-hash",
                UserId = TestUserId,
                TokenPrefix = "p_test",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            var mockResponse = new Mock<ItemResponse<PersonalAccessToken>>();
            mockResponse.Setup(r => r.Resource).Returns(pat);

            _mockContainer
                .Setup(c => c.CreateItemAsync(
                    It.IsAny<PersonalAccessToken>(),
                    It.IsAny<PartitionKey>(),
                    It.IsAny<ItemRequestOptions>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockResponse.Object);

            // Act
            PersonalAccessToken result = await _service.CreatePersonalAccessTokenAsync(pat);

            // Assert
            Assert.Equal(pat.Id, result.Id);
            Assert.Equal(pat.UserId, result.UserId);
            Assert.Equal(pat.TokenPrefix, result.TokenPrefix);

            _mockContainer.Verify(c => c.CreateItemAsync(
                It.Is<PersonalAccessToken>(p => 
                    p.Id == pat.Id && 
                    p.UserId == pat.UserId && 
                    p.TokenPrefix == pat.TokenPrefix),
                It.Is<PartitionKey>(pk => pk.ToString().Contains(TestUserId)),
                It.IsAny<ItemRequestOptions>(),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task GetPersonalAccessTokenByIdAsync_WhenTokenExists_ShouldReturnToken()
        {
            // Arrange
            string tokenId = "test-hash";
            var pat = new PersonalAccessToken
            {
                Id = tokenId,
                UserId = TestUserId,
                TokenPrefix = "p_test",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            var tokens = new List<PersonalAccessToken> { pat };
            var mockIterator = new Mock<FeedIterator<PersonalAccessToken>>();
            var mockResponse = new Mock<FeedResponse<PersonalAccessToken>>();
            
            mockResponse.Setup(r => r.GetEnumerator()).Returns(tokens.GetEnumerator());
            mockIterator.SetupSequence(i => i.HasMoreResults)
                .Returns(true)
                .Returns(false);
            mockIterator.Setup(i => i.ReadNextAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockResponse.Object);

            _mockContainer
                .Setup(c => c.GetItemQueryIterator<PersonalAccessToken>(
                    It.IsAny<QueryDefinition>(),
                    It.IsAny<string>(),
                    It.IsAny<QueryRequestOptions>()))
                .Returns(mockIterator.Object);

            // Act
            PersonalAccessToken? result = await _service.GetPersonalAccessTokenByIdAsync(tokenId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(tokenId, result.Id);
            Assert.Equal(TestUserId, result.UserId);
            Assert.Equal("p_test", result.TokenPrefix);

            _mockContainer.Verify(c => c.GetItemQueryIterator<PersonalAccessToken>(
                It.Is<QueryDefinition>(q => q.QueryText.Contains("WHERE c.id = @id")),
                It.IsAny<string>(),
                It.IsAny<QueryRequestOptions>()), Times.Once);
        }

        [Fact]
        public async Task GetPersonalAccessTokenByIdAsync_WhenTokenNotFound_ShouldReturnNull()
        {
            // Arrange
            string tokenId = "nonexistent-hash";

            var emptyTokens = new List<PersonalAccessToken>();
            var mockIterator = new Mock<FeedIterator<PersonalAccessToken>>();
            var mockResponse = new Mock<FeedResponse<PersonalAccessToken>>();
            
            mockResponse.Setup(r => r.GetEnumerator()).Returns(emptyTokens.GetEnumerator());
            mockIterator.SetupSequence(i => i.HasMoreResults)
                .Returns(true)
                .Returns(false);
            mockIterator.Setup(i => i.ReadNextAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockResponse.Object);

            _mockContainer
                .Setup(c => c.GetItemQueryIterator<PersonalAccessToken>(
                    It.IsAny<QueryDefinition>(),
                    It.IsAny<string>(),
                    It.IsAny<QueryRequestOptions>()))
                .Returns(mockIterator.Object);

            // Act
            PersonalAccessToken? result = await _service.GetPersonalAccessTokenByIdAsync(tokenId);

            // Assert
            Assert.Null(result);

            _mockContainer.Verify(c => c.GetItemQueryIterator<PersonalAccessToken>(
                It.Is<QueryDefinition>(q => q.QueryText.Contains("WHERE c.id = @id")),
                It.IsAny<string>(),
                It.IsAny<QueryRequestOptions>()), Times.Once);
        }

        [Fact]
        public async Task GetUserPersonalAccessTokensAsync_ShouldReturnUserTokens()
        {
            // Arrange
            var tokens = new List<PersonalAccessToken>
            {
                new PersonalAccessToken
                {
                    Id = "hash1",
                    UserId = TestUserId,
                    TokenPrefix = "p_token1",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow.AddDays(-1)
                },
                new PersonalAccessToken
                {
                    Id = "hash2",
                    UserId = TestUserId,
                    TokenPrefix = "p_token2",
                    IsActive = false,
                    CreatedAt = DateTime.UtcNow.AddDays(-2)
                }
            };

            var mockIterator = new Mock<FeedIterator<PersonalAccessToken>>();
            var mockResponse = new Mock<FeedResponse<PersonalAccessToken>>();
            
            mockResponse.Setup(r => r.GetEnumerator()).Returns(tokens.GetEnumerator());
            mockIterator.SetupSequence(i => i.HasMoreResults)
                .Returns(true)
                .Returns(false);
            mockIterator.Setup(i => i.ReadNextAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockResponse.Object);

            _mockContainer
                .Setup(c => c.GetItemQueryIterator<PersonalAccessToken>(
                    It.IsAny<QueryDefinition>(),
                    It.IsAny<string>(),
                    It.IsAny<QueryRequestOptions>()))
                .Returns(mockIterator.Object);

            // Act
            var result = await _service.GetPersonalAccessTokensAsync(TestUserId);
            List<PersonalAccessToken> resultList = result.ToList();

            // Assert
            Assert.Equal(2, resultList.Count);
            Assert.All(resultList, token => Assert.Equal(TestUserId, token.UserId));
            Assert.Contains(resultList, t => t.TokenPrefix == "p_token1");
            Assert.Contains(resultList, t => t.TokenPrefix == "p_token2");

            _mockContainer.Verify(c => c.GetItemQueryIterator<PersonalAccessToken>(
                It.Is<QueryDefinition>(q => q.QueryText.Contains("WHERE c.userId = @userId")),
                It.IsAny<string>(),
                It.IsAny<QueryRequestOptions>()),
                Times.Once);
        }

        [Fact]
        public async Task GetUserPersonalAccessTokensAsync_WhenNoTokens_ShouldReturnEmptyList()
        {
            // Arrange
            var mockIterator = new Mock<FeedIterator<PersonalAccessToken>>();
            var mockResponse = new Mock<FeedResponse<PersonalAccessToken>>();
            
            mockResponse.Setup(r => r.GetEnumerator()).Returns(new List<PersonalAccessToken>().GetEnumerator());
            mockIterator.Setup(i => i.HasMoreResults).Returns(false);

            _mockContainer
                .Setup(c => c.GetItemQueryIterator<PersonalAccessToken>(
                    It.IsAny<QueryDefinition>(),
                    It.IsAny<string>(),
                    It.IsAny<QueryRequestOptions>()))
                .Returns(mockIterator.Object);

            // Act
            var result = await _service.GetPersonalAccessTokensAsync(TestUserId);
            List<PersonalAccessToken> resultList = result.ToList();

            // Assert
            Assert.Empty(resultList);
        }

        [Fact]
        public async Task DeletePersonalAccessTokenAsync_ShouldCallDeleteItemAsync()
        {
            // Arrange
            string tokenId = "test-hash";

            var mockResponse = new Mock<ItemResponse<PersonalAccessToken>>();
            mockResponse.Setup(r => r.StatusCode).Returns(System.Net.HttpStatusCode.NoContent);

            _mockContainer
                .Setup(c => c.DeleteItemAsync<PersonalAccessToken>(
                    tokenId,
                    It.IsAny<PartitionKey>(),
                    It.IsAny<ItemRequestOptions>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockResponse.Object);

            // Act
            await _service.DeletePersonalAccessTokenAsync(tokenId, TestUserId);

            // Assert
            _mockContainer.Verify(c => c.DeleteItemAsync<PersonalAccessToken>(
                tokenId,
                It.Is<PartitionKey>(pk => pk.ToString().Contains(TestUserId)),
                It.IsAny<ItemRequestOptions>(),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task DeletePersonalAccessTokenAsync_WhenTokenNotFound_ShouldThrow()
        {
            // Arrange
            string tokenId = "nonexistent-hash";

            _mockContainer
                .Setup(c => c.DeleteItemAsync<PersonalAccessToken>(
                    tokenId,
                    It.IsAny<PartitionKey>(),
                    It.IsAny<ItemRequestOptions>(),
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(new CosmosException("Not found", System.Net.HttpStatusCode.NotFound, 404, "", 0));

            // Act & Assert
            // CosmosDB service should throw when token not found - this is expected behavior
            await Assert.ThrowsAsync<CosmosException>(() => _service.DeletePersonalAccessTokenAsync(tokenId, TestUserId));

            _mockContainer.Verify(c => c.DeleteItemAsync<PersonalAccessToken>(
                tokenId,
                It.Is<PartitionKey>(pk => pk.ToString().Contains(TestUserId)),
                It.IsAny<ItemRequestOptions>(),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task CreatePersonalAccessTokenAsync_WhenDuplicateId_ShouldThrow()
        {
            // Arrange
            var pat = new PersonalAccessToken
            {
                Id = "duplicate-hash",
                UserId = TestUserId,
                TokenPrefix = "p_test",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _mockContainer
                .Setup(c => c.CreateItemAsync(
                    It.IsAny<PersonalAccessToken>(),
                    It.IsAny<PartitionKey>(),
                    It.IsAny<ItemRequestOptions>(),
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(new CosmosException("Conflict", System.Net.HttpStatusCode.Conflict, 409, "", 0));

            // Act & Assert
            await Assert.ThrowsAsync<CosmosException>(() => _service.CreatePersonalAccessTokenAsync(pat));
        }
    }
}
