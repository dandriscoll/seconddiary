using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using SecondDiary.Middleware;
using SecondDiary.Models;
using SecondDiary.Services;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Xunit;

namespace SecondDiary.Tests.Middleware
{
    public class PatAuthenticationHandlerTests
    {
        private readonly Mock<IPersonalAccessTokenService> _mockPatService;
        private readonly Mock<IOptionsMonitor<PatAuthenticationSchemeOptions>> _mockOptions;
        private readonly Mock<ILoggerFactory> _mockLoggerFactory;
        private readonly Mock<ILogger<PatAuthenticationHandler>> _mockLogger;
        private readonly Mock<UrlEncoder> _mockUrlEncoder;
        private readonly DefaultHttpContext _httpContext;
        private readonly PatAuthenticationHandler _handler;
        private const string TestUserId = "test-user-123";

        public PatAuthenticationHandlerTests()
        {
            _mockPatService = new Mock<IPersonalAccessTokenService>();
            _mockOptions = new Mock<IOptionsMonitor<PatAuthenticationSchemeOptions>>();
            _mockLoggerFactory = new Mock<ILoggerFactory>();
            _mockLogger = new Mock<ILogger<PatAuthenticationHandler>>();
            _mockUrlEncoder = new Mock<UrlEncoder>();
            _httpContext = new DefaultHttpContext();

            _mockOptions.Setup(x => x.CurrentValue)
                .Returns(new PatAuthenticationSchemeOptions());
            
            _mockLoggerFactory.Setup(x => x.CreateLogger(It.IsAny<string>()))
                .Returns(_mockLogger.Object);

            var scheme = new AuthenticationScheme(
                PatAuthenticationSchemeOptions.DefaultScheme,
                PatAuthenticationSchemeOptions.DefaultScheme,
                typeof(PatAuthenticationHandler));

            _handler = new PatAuthenticationHandler(
                _mockOptions.Object,
                _mockLoggerFactory.Object,
                _mockUrlEncoder.Object,
                _mockPatService.Object);

            // Initialize the handler with a scheme and context
            _handler.InitializeAsync(scheme, _httpContext).GetAwaiter().GetResult();
        }

        [Fact]
        public async Task HandleAuthenticateAsync_WithNoAuthorizationHeader_ShouldReturnNoResult()
        {
            // Arrange
            // No Authorization header set

            // Act
            AuthenticateResult result = await _handler.AuthenticateAsync();

            // Assert
            Assert.False(result.Succeeded);
            Assert.True(result.None);
            _mockPatService.Verify(x => x.ValidateTokenAsync(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task HandleAuthenticateAsync_WithNonBearerToken_ShouldReturnNoResult()
        {
            // Arrange
            _httpContext.Request.Headers["Authorization"] = "Basic sometoken";

            // Act
            AuthenticateResult result = await _handler.AuthenticateAsync();

            // Assert
            Assert.False(result.Succeeded);
            Assert.True(result.None);
            _mockPatService.Verify(x => x.ValidateTokenAsync(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task HandleAuthenticateAsync_WithNonPatToken_ShouldReturnNoResult()
        {
            // Arrange
            _httpContext.Request.Headers["Authorization"] = "Bearer someothertoken";

            // Act
            AuthenticateResult result = await _handler.AuthenticateAsync();

            // Assert
            Assert.False(result.Succeeded);
            Assert.True(result.None);
            _mockPatService.Verify(x => x.ValidateTokenAsync(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task HandleAuthenticateAsync_WithValidPatToken_ShouldReturnSuccess()
        {
            // Arrange
            string token = "p_validtoken123";
            _httpContext.Request.Headers["Authorization"] = $"Bearer {token}";

            var patToken = new PersonalAccessToken
            {
                Id = "test-hash",
                UserId = TestUserId,
                TokenPrefix = "p_validtoken",
                IsActive = true
            };

            _mockPatService
                .Setup(x => x.ValidateTokenAsync(token))
                .ReturnsAsync(patToken);

            // Act
            AuthenticateResult result = await _handler.AuthenticateAsync();

            // Assert
            Assert.True(result.Succeeded);
            Assert.NotNull(result.Principal);
            Assert.NotNull(result.Ticket);

            // Verify claims
            ClaimsIdentity? identity = result.Principal.Identity as ClaimsIdentity;
            Assert.NotNull(identity);
            Assert.Equal(PatAuthenticationSchemeOptions.DefaultScheme, identity.AuthenticationType);

            Claim? userIdClaim = result.Principal.FindFirst(ClaimTypes.NameIdentifier);
            Assert.NotNull(userIdClaim);
            Assert.Equal(TestUserId, userIdClaim.Value);

            Claim? objectIdClaim = result.Principal.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier");
            Assert.NotNull(objectIdClaim);
            Assert.Equal(TestUserId, objectIdClaim.Value);

            Claim? patIdClaim = result.Principal.FindFirst("pat_id");
            Assert.NotNull(patIdClaim);
            Assert.Equal(patToken.Id, patIdClaim.Value);

            Claim? authMethodClaim = result.Principal.FindFirst("auth_method");
            Assert.NotNull(authMethodClaim);
            Assert.Equal("pat", authMethodClaim.Value);

            Claim? nameClaim = result.Principal.FindFirst(ClaimTypes.Name);
            Assert.NotNull(nameClaim);
            Assert.Equal($"PAT-{patToken.TokenPrefix}", nameClaim.Value);

            _mockPatService.Verify(x => x.ValidateTokenAsync(token), Times.Once);
        }

        [Fact]
        public async Task HandleAuthenticateAsync_WithInvalidPatToken_ShouldReturnFail()
        {
            // Arrange
            string token = "p_invalidtoken123";
            _httpContext.Request.Headers["Authorization"] = $"Bearer {token}";

            _mockPatService
                .Setup(x => x.ValidateTokenAsync(token))
                .ReturnsAsync((PersonalAccessToken?)null);

            // Act
            AuthenticateResult result = await _handler.AuthenticateAsync();

            // Assert
            Assert.False(result.Succeeded);
            Assert.True(result.Failure != null);
            Assert.Equal("Invalid token", result.Failure.Message);

            _mockPatService.Verify(x => x.ValidateTokenAsync(token), Times.Once);
        }

        [Fact]
        public async Task HandleAuthenticateAsync_WhenValidationThrows_ShouldReturnFail()
        {
            // Arrange
            string token = "p_errortoken123";
            _httpContext.Request.Headers["Authorization"] = $"Bearer {token}";

            _mockPatService
                .Setup(x => x.ValidateTokenAsync(token))
                .ThrowsAsync(new Exception("Database error"));

            // Act
            AuthenticateResult result = await _handler.AuthenticateAsync();

            // Assert
            Assert.False(result.Succeeded);
            Assert.True(result.Failure != null);
            Assert.Equal("Authentication error", result.Failure.Message);

            _mockPatService.Verify(x => x.ValidateTokenAsync(token), Times.Once);
        }

        [Fact]
        public async Task HandleAuthenticateAsync_WithBearerTokenWithSpaces_ShouldTrimAndProcess()
        {
            // Arrange
            string token = "p_tokenwithtrim";
            _httpContext.Request.Headers["Authorization"] = $"Bearer   {token}   ";

            var patToken = new PersonalAccessToken
            {
                Id = "test-hash",
                UserId = TestUserId,
                TokenPrefix = "p_tokenwithtrim",
                IsActive = true
            };

            _mockPatService
                .Setup(x => x.ValidateTokenAsync(token))
                .ReturnsAsync(patToken);

            // Act
            AuthenticateResult result = await _handler.AuthenticateAsync();

            // Assert
            Assert.True(result.Succeeded);
            _mockPatService.Verify(x => x.ValidateTokenAsync(token), Times.Once);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("Bearer")]
        [InlineData("Bearer ")]
        public async Task HandleAuthenticateAsync_WithInvalidAuthorizationHeader_ShouldReturnNoResult(string authHeader)
        {
            // Arrange
            _httpContext.Request.Headers["Authorization"] = authHeader;

            // Act
            AuthenticateResult result = await _handler.AuthenticateAsync();

            // Assert
            Assert.False(result.Succeeded);
            Assert.True(result.None);
            _mockPatService.Verify(x => x.ValidateTokenAsync(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task HandleAuthenticateAsync_WithCaseInsensitiveBearerToken_ShouldWork()
        {
            // Arrange
            string token = "p_casetest123";
            _httpContext.Request.Headers["Authorization"] = $"bearer {token}";

            var patToken = new PersonalAccessToken
            {
                Id = "test-hash",
                UserId = TestUserId,
                TokenPrefix = "p_casetest",
                IsActive = true
            };

            _mockPatService
                .Setup(x => x.ValidateTokenAsync(token))
                .ReturnsAsync(patToken);

            // Act
            AuthenticateResult result = await _handler.AuthenticateAsync();

            // Assert
            Assert.True(result.Succeeded);
            _mockPatService.Verify(x => x.ValidateTokenAsync(token), Times.Once);
        }
    }
}
