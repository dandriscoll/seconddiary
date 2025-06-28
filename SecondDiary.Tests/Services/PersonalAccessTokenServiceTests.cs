using Microsoft.Extensions.Logging;
using Moq;
using SecondDiary.Models;
using SecondDiary.Services;
using Xunit;

namespace SecondDiary.Tests.Services
{
    public class PersonalAccessTokenServiceTests
    {
        private readonly Mock<ICosmosDbService> _mockCosmosDbService;
        private readonly Mock<ILogger<PersonalAccessTokenService>> _mockLogger;
        private readonly PersonalAccessTokenService _service;
        private const string TestUserId = "test-user-123";

        public PersonalAccessTokenServiceTests()
        {
            _mockCosmosDbService = new Mock<ICosmosDbService>();
            _mockLogger = new Mock<ILogger<PersonalAccessTokenService>>();
            _service = new PersonalAccessTokenService(_mockCosmosDbService.Object, _mockLogger.Object);
        }

        [Fact]
        public async Task CreateTokenAsync_ShouldCreateValidToken()
        {
            // Arrange
            PersonalAccessToken? capturedToken = null;
            var expectedCreatedAt = DateTime.UtcNow;

            _mockCosmosDbService
                .Setup(x => x.CreatePersonalAccessTokenAsync(It.IsAny<PersonalAccessToken>()))
                .Callback<PersonalAccessToken>(token => capturedToken = token)
                .ReturnsAsync((PersonalAccessToken token) => new PersonalAccessToken
                {
                    Id = token.Id,
                    UserId = token.UserId,
                    TokenPrefix = token.TokenPrefix,
                    CreatedAt = token.CreatedAt,
                    IsActive = token.IsActive
                });

            // Act
            CreatePersonalAccessTokenResponse result = await _service.CreateTokenAsync(TestUserId);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(capturedToken);
            Assert.Equal(capturedToken.Id, result.Id);
            Assert.StartsWith("p_", result.Token);
            Assert.Equal(capturedToken.TokenPrefix, result.TokenPrefix);
            Assert.Equal(capturedToken.CreatedAt, result.CreatedAt);
            Assert.Contains("This token will only be shown once", result.Warning);

            _mockCosmosDbService.Verify(
                x => x.CreatePersonalAccessTokenAsync(It.Is<PersonalAccessToken>(t => 
                    t.UserId == TestUserId &&
                    t.IsActive == true &&
                    !string.IsNullOrEmpty(t.Id) &&
                    !string.IsNullOrEmpty(t.TokenPrefix)
                )), 
                Times.Once
            );
        }

        [Fact]
        public async Task GetUserTokensAsync_ShouldReturnTokenSummaries()
        {
            // Arrange
            var tokens = new List<PersonalAccessToken>
            {
                new PersonalAccessToken
                {
                    Id = "hash1",
                    UserId = TestUserId,
                    TokenPrefix = "p_abc123",
                    CreatedAt = DateTime.UtcNow.AddDays(-1),
                    IsActive = true
                },
                new PersonalAccessToken
                {
                    Id = "hash2",
                    UserId = TestUserId,
                    TokenPrefix = "p_def456",
                    CreatedAt = DateTime.UtcNow.AddDays(-2),
                    IsActive = false
                }
            };

            _mockCosmosDbService
                .Setup(x => x.GetPersonalAccessTokensAsync(TestUserId))
                .ReturnsAsync(tokens);

            // Act
            IEnumerable<PersonalAccessTokenSummary> result = await _service.GetUserTokensAsync(TestUserId);

            // Assert
            PersonalAccessTokenSummary[] summaries = result.ToArray();
            Assert.Equal(2, summaries.Length);
            
            Assert.Equal("hash1", summaries[0].Id);
            Assert.Equal("p_abc123", summaries[0].TokenPrefix);
            Assert.True(summaries[0].IsActive);
            
            Assert.Equal("hash2", summaries[1].Id);
            Assert.Equal("p_def456", summaries[1].TokenPrefix);
            Assert.False(summaries[1].IsActive);

            _mockCosmosDbService.Verify(x => x.GetPersonalAccessTokensAsync(TestUserId), Times.Once);
        }

        [Fact]
        public async Task ValidateTokenAsync_WithValidToken_ShouldReturnToken()
        {
            // Arrange
            string testToken = "p_validtoken123";
            var storedToken = new PersonalAccessToken
            {
                Id = "test-hash",
                UserId = TestUserId,
                TokenPrefix = "p_validtoken",
                IsActive = true
            };

            _mockCosmosDbService
                .Setup(x => x.GetPersonalAccessTokenByIdAsync(It.IsAny<string>()))
                .ReturnsAsync(storedToken);

            // Act
            PersonalAccessToken? result = await _service.ValidateTokenAsync(testToken);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(storedToken.Id, result.Id);
            Assert.Equal(storedToken.UserId, result.UserId);
            Assert.True(result.IsValid);

            _mockCosmosDbService.Verify(
                x => x.GetPersonalAccessTokenByIdAsync(It.IsAny<string>()), 
                Times.Once
            );
        }

        [Fact]
        public async Task ValidateTokenAsync_WithInvalidPrefix_ShouldReturnNull()
        {
            // Arrange
            string invalidToken = "invalid_token123";

            // Act
            PersonalAccessToken? result = await _service.ValidateTokenAsync(invalidToken);

            // Assert
            Assert.Null(result);
            _mockCosmosDbService.Verify(
                x => x.GetPersonalAccessTokenByIdAsync(It.IsAny<string>()), 
                Times.Never
            );
        }

        [Fact]
        public async Task ValidateTokenAsync_WithNullToken_ShouldReturnNull()
        {
            // Act
            PersonalAccessToken? result = await _service.ValidateTokenAsync(string.Empty);

            // Assert
            Assert.Null(result);
            _mockCosmosDbService.Verify(
                x => x.GetPersonalAccessTokenByIdAsync(It.IsAny<string>()), 
                Times.Never
            );
        }

        [Fact]
        public async Task ValidateTokenAsync_WithInactiveToken_ShouldReturnNull()
        {
            // Arrange
            string testToken = "p_inactivetoken123";
            var storedToken = new PersonalAccessToken
            {
                Id = "test-hash",
                UserId = TestUserId,
                IsActive = false
            };

            _mockCosmosDbService
                .Setup(x => x.GetPersonalAccessTokenByIdAsync(It.IsAny<string>()))
                .ReturnsAsync(storedToken);

            // Act
            PersonalAccessToken? result = await _service.ValidateTokenAsync(testToken);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task ValidateTokenAsync_WithNonExistentToken_ShouldReturnNull()
        {
            // Arrange
            string testToken = "p_nonexistenttoken123";

            _mockCosmosDbService
                .Setup(x => x.GetPersonalAccessTokenByIdAsync(It.IsAny<string>()))
                .ReturnsAsync((PersonalAccessToken?)null);

            // Act
            PersonalAccessToken? result = await _service.ValidateTokenAsync(testToken);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task RevokeTokenAsync_ShouldDeleteToken()
        {
            // Arrange
            string tokenId = "test-token-id";

            _mockCosmosDbService
                .Setup(x => x.DeletePersonalAccessTokenAsync(tokenId, TestUserId))
                .Returns(Task.CompletedTask);

            // Act
            bool result = await _service.RevokeTokenAsync(TestUserId, tokenId);

            // Assert
            Assert.True(result);
            _mockCosmosDbService.Verify(
                x => x.DeletePersonalAccessTokenAsync(tokenId, TestUserId), 
                Times.Once
            );
        }

        [Fact]
        public async Task RevokeTokenAsync_WhenDeleteFails_ShouldReturnFalse()
        {
            // Arrange
            string tokenId = "test-token-id";

            _mockCosmosDbService
                .Setup(x => x.DeletePersonalAccessTokenAsync(tokenId, TestUserId))
                .ThrowsAsync(new Exception("Database error"));

            // Act
            bool result = await _service.RevokeTokenAsync(TestUserId, tokenId);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task GetTokenAsync_ShouldReturnToken()
        {
            // Arrange
            string tokenId = "test-token-id";
            var expectedToken = new PersonalAccessToken
            {
                Id = tokenId,
                UserId = TestUserId,
                TokenPrefix = "p_test123",
                IsActive = true
            };

            _mockCosmosDbService
                .Setup(x => x.GetPersonalAccessTokenByIdAsync(tokenId))
                .ReturnsAsync(expectedToken);

            // Act
            PersonalAccessToken? result = await _service.GetTokenAsync(TestUserId, tokenId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(expectedToken.Id, result.Id);
            Assert.Equal(expectedToken.UserId, result.UserId);

            _mockCosmosDbService.Verify(
                x => x.GetPersonalAccessTokenByIdAsync(tokenId), 
                Times.Once
            );
        }

        [Fact]
        public async Task GeneratedToken_ShouldHaveCorrectFormat()
        {
            // This tests the token generation indirectly through CreateTokenAsync
            // Arrange
            var mockToken = new PersonalAccessToken
            {
                Id = "test-hash",
                UserId = TestUserId,
                TokenPrefix = "p_generated",
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };

            _mockCosmosDbService
                .Setup(x => x.CreatePersonalAccessTokenAsync(It.IsAny<PersonalAccessToken>()))
                .ReturnsAsync(mockToken);

            // Act
            Task<CreatePersonalAccessTokenResponse> task = _service.CreateTokenAsync(TestUserId);
            CreatePersonalAccessTokenResponse result = await task;

            // Assert
            Assert.StartsWith("p_", result.Token);
            Assert.True(result.Token.Length > 10); // Should be reasonably long
            Assert.DoesNotContain(" ", result.Token); // No spaces
            Assert.DoesNotContain("+", result.Token); // Should be URL-safe
            Assert.DoesNotContain("/", result.Token); // Should be URL-safe
        }
    }
}
