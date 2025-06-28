using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using SecondDiary.Controllers;
using SecondDiary.Models;
using SecondDiary.Services;
using System.Security.Claims;
using Xunit;

namespace SecondDiary.Tests.Controllers
{
    public class PersonalAccessTokenControllerTests
    {
        private readonly Mock<IPersonalAccessTokenService> _mockPatService;
        private readonly Mock<ILogger<PersonalAccessTokenController>> _mockLogger;
        private readonly PersonalAccessTokenController _controller;
        private const string TestUserId = "test-user-123";

        public PersonalAccessTokenControllerTests()
        {
            _mockPatService = new Mock<IPersonalAccessTokenService>();
            _mockLogger = new Mock<ILogger<PersonalAccessTokenController>>();
            _controller = new PersonalAccessTokenController(_mockPatService.Object, _mockLogger.Object);

            // Setup user context
            SetupUserContext();
        }

        private void SetupUserContext()
        {
            var claims = new List<Claim>
            {
                new Claim("http://schemas.microsoft.com/identity/claims/objectidentifier", TestUserId),
                new Claim(ClaimTypes.NameIdentifier, TestUserId)
            };

            ClaimsIdentity identity = new ClaimsIdentity(claims, "Bearer");
            ClaimsPrincipal principal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = principal
                }
            };
        }

        [Fact]
        public async Task CreateToken_WithValidUser_ShouldReturnOkWithToken()
        {
            // Arrange
            var expectedResponse = new CreatePersonalAccessTokenResponse
            {
                Id = "test-hash",
                Token = "p_randomtoken123456",
                TokenPrefix = "p_randomtoken",
                Warning = "This token will only be shown once. Please copy it now."
            };

            _mockPatService
                .Setup(s => s.CreateTokenAsync(TestUserId))
                .ReturnsAsync(expectedResponse);

            // Act
            ActionResult<CreatePersonalAccessTokenResponse> result = await _controller.CreateToken();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsType<CreatePersonalAccessTokenResponse>(okResult.Value);
            
            Assert.Equal(expectedResponse.Id, response.Id);
            Assert.Equal(expectedResponse.Token, response.Token);
            Assert.Equal(expectedResponse.TokenPrefix, response.TokenPrefix);
            Assert.Equal(expectedResponse.Warning, response.Warning);

            _mockPatService.Verify(s => s.CreateTokenAsync(TestUserId), Times.Once);
        }

        [Fact]
        public async Task CreateToken_WithNoUserClaims_ShouldReturnUnauthorized()
        {
            // Arrange
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity())
                }
            };

            // Act
            ActionResult<CreatePersonalAccessTokenResponse> result = await _controller.CreateToken();

            // Assert
            var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result.Result);
            Assert.Equal("User ID not found in token", unauthorizedResult.Value);

            _mockPatService.Verify(s => s.CreateTokenAsync(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task CreateToken_WhenServiceThrows_ShouldReturnServerError()
        {
            // Arrange
            _mockPatService
                .Setup(s => s.CreateTokenAsync(TestUserId))
                .ThrowsAsync(new Exception("Database error"));

            // Act
            ActionResult<CreatePersonalAccessTokenResponse> result = await _controller.CreateToken();

            // Assert
            var errorResult = Assert.IsType<ObjectResult>(result.Result);
            Assert.Equal(500, errorResult.StatusCode);
            Assert.Equal("An error occurred while creating the token", errorResult.Value);
        }

        [Fact]
        public async Task GetUserTokens_WithValidUser_ShouldReturnTokenList()
        {
            // Arrange
            var expectedTokens = new List<PersonalAccessTokenSummary>
            {
                new PersonalAccessTokenSummary
                {
                    Id = "hash1",
                    TokenPrefix = "p_token1",
                    CreatedAt = DateTime.UtcNow.AddDays(-1),
                    IsActive = true
                },
                new PersonalAccessTokenSummary
                {
                    Id = "hash2",
                    TokenPrefix = "p_token2",
                    CreatedAt = DateTime.UtcNow.AddDays(-2),
                    IsActive = false
                }
            };

            _mockPatService
                .Setup(s => s.GetUserTokensAsync(TestUserId))
                .ReturnsAsync(expectedTokens);

            // Act
            ActionResult<IEnumerable<PersonalAccessTokenSummary>> result = await _controller.GetUserTokens();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var tokens = Assert.IsType<List<PersonalAccessTokenSummary>>(okResult.Value);
            
            Assert.Equal(2, tokens.Count);
            Assert.Equal("p_token1", tokens[0].TokenPrefix);
            Assert.Equal("p_token2", tokens[1].TokenPrefix);

            _mockPatService.Verify(s => s.GetUserTokensAsync(TestUserId), Times.Once);
        }

        [Fact]
        public async Task GetUserTokens_WithNoUserClaims_ShouldReturnUnauthorized()
        {
            // Arrange
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity())
                }
            };

            // Act
            ActionResult<IEnumerable<PersonalAccessTokenSummary>> result = await _controller.GetUserTokens();

            // Assert
            var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result.Result);
            Assert.Equal("User ID not found in token", unauthorizedResult.Value);

            _mockPatService.Verify(s => s.GetUserTokensAsync(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task GetUserTokens_WhenServiceThrows_ShouldReturnServerError()
        {
            // Arrange
            _mockPatService
                .Setup(s => s.GetUserTokensAsync(TestUserId))
                .ThrowsAsync(new Exception("Database error"));

            // Act
            ActionResult<IEnumerable<PersonalAccessTokenSummary>> result = await _controller.GetUserTokens();

            // Assert
            var errorResult = Assert.IsType<ObjectResult>(result.Result);
            Assert.Equal(500, errorResult.StatusCode);
            Assert.Equal("An error occurred while retrieving tokens", errorResult.Value);
        }

        [Fact]
        public async Task RevokeToken_WithValidToken_ShouldReturnNoContent()
        {
            // Arrange
            string tokenId = "test-hash";

            _mockPatService
                .Setup(s => s.RevokeTokenAsync(tokenId, TestUserId))
                .ReturnsAsync(true);

            // Act
            ActionResult result = await _controller.RevokeToken(tokenId);

            // Assert
            Assert.IsType<NoContentResult>(result);

            _mockPatService.Verify(s => s.RevokeTokenAsync(tokenId, TestUserId), Times.Once);
        }

        [Fact]
        public async Task RevokeToken_WithNonexistentToken_ShouldReturnNotFound()
        {
            // Arrange
            string tokenId = "nonexistent-hash";

            _mockPatService
                .Setup(s => s.RevokeTokenAsync(tokenId, TestUserId))
                .ReturnsAsync(false);

            // Act
            ActionResult result = await _controller.RevokeToken(tokenId);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal("Token not found", notFoundResult.Value);

            _mockPatService.Verify(s => s.RevokeTokenAsync(tokenId, TestUserId), Times.Once);
        }

        [Fact]
        public async Task RevokeToken_WithNoUserClaims_ShouldReturnUnauthorized()
        {
            // Arrange
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity())
                }
            };

            string tokenId = "test-hash";

            // Act
            ActionResult result = await _controller.RevokeToken(tokenId);

            // Assert
            var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
            Assert.Equal("User ID not found in token", unauthorizedResult.Value);

            _mockPatService.Verify(s => s.RevokeTokenAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task RevokeToken_WhenServiceThrows_ShouldReturnServerError()
        {
            // Arrange
            string tokenId = "test-hash";

            _mockPatService
                .Setup(s => s.RevokeTokenAsync(tokenId, TestUserId))
                .ThrowsAsync(new Exception("Database error"));

            // Act
            ActionResult result = await _controller.RevokeToken(tokenId);

            // Assert
            var errorResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, errorResult.StatusCode);
            Assert.Equal("An error occurred while revoking the token", errorResult.Value);
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        [InlineData("   ")]
        public async Task RevokeToken_WithInvalidTokenId_ShouldStillCallService(string? tokenId)
        {
            // Arrange
            _mockPatService
                .Setup(s => s.RevokeTokenAsync(tokenId ?? "", TestUserId))
                .ReturnsAsync(false);

            // Act
            ActionResult result = await _controller.RevokeToken(tokenId ?? "");

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal("Token not found", notFoundResult.Value);

            _mockPatService.Verify(s => s.RevokeTokenAsync(tokenId ?? "", TestUserId), Times.Once);
        }
    }
}
