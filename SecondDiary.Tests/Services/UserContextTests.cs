using Microsoft.AspNetCore.Http;
using Microsoft.Identity.Web;
using Moq;
using Microsoft.Extensions.Configuration;
using SecondDiary.Services;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using Xunit;

namespace SecondDiary.Tests.Services
{
    public class UserContextTests
    {
        private readonly Mock<IHttpContextAccessor> _mockHttpContextAccessor;
        private readonly Mock<IConfiguration> _mockConfiguration;
        private readonly Mock<IEncryptionService> _mockEncryptionService;
        private readonly UserContext _userContext;
        private readonly HttpContext _httpContext;
        private readonly ClaimsPrincipal _user;
        private readonly Mock<IConfigurationSection> _mockConfigSection;


        public UserContextTests()
        {
            _mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
            _mockConfiguration = new Mock<IConfiguration>();
            _mockEncryptionService = new Mock<IEncryptionService>();
            
            _httpContext = new DefaultHttpContext();
            _user = new ClaimsPrincipal();
            _httpContext.User = _user;
            _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(_httpContext);
            
            _mockConfigSection = new Mock<IConfigurationSection>();
            _mockConfigSection.Setup(x => x.Value).Returns("test-client-id");
            _mockConfiguration.Setup(x => x["AzureAd:ClientId"]).Returns("test-client-id");

            // Set up encryption key in configuration
            _mockConfiguration.Setup(x => x["Encryption:Key"]).Returns("abc123456789012345678901234567890");

            // Set up encryption service mock
            _mockEncryptionService.Setup(x => x.Encrypt(It.IsAny<string>()))
                .Returns((string s) => string.IsNullOrEmpty(s) ? s : "encrypted-" + s);
            _mockEncryptionService.Setup(x => x.Decrypt(It.IsAny<string>()))
                .Returns((string s) => s.Replace("encrypted-", ""));
            
            _userContext = new UserContext(
                _mockHttpContextAccessor.Object,
                _mockConfiguration.Object,
                _mockEncryptionService.Object);
        }

        private const string TestObjectId = "test-object-id";
        private const string TestAppId = "test-app-id";

        [Fact]
        public void UserId_ReturnsNull_WhenNotAuthenticated()
        {
            // Arrange
            var httpContextAccessor = CreateHttpContextAccessor(isAuthenticated: false);
            var configuration = CreateConfiguration();
            var encryptionService = CreateEncryptionService();
            var userContext = new UserContext(httpContextAccessor.Object, configuration.Object, encryptionService.Object);

            // Act
            var userId = userContext.UserId;

            // Assert
            Assert.Null(userId);
        }

        [Fact]
        public void UserId_ReturnsNull_WhenInvalidAudience()
        {
            // Arrange
            var httpContextAccessor = CreateHttpContextAccessor(
                isAuthenticated: true, 
                audience: "wrong-audience");
            var configuration = CreateConfiguration();
            var encryptionService = CreateEncryptionService();
            var userContext = new UserContext(httpContextAccessor.Object, configuration.Object, encryptionService.Object);

            // Act
            var userId = userContext.UserId;

            // Assert
            Assert.Null(userId);
        }

        [Fact]
        public void UserId_ReturnsObjectId_WhenAuthenticatedWithValidAudience()
        {
            // Arrange
            var httpContextAccessor = CreateHttpContextAccessor(
                isAuthenticated: true, 
                audience: TestAppId, 
                objectId: TestObjectId);
            var configuration = CreateConfiguration();
            var encryptionService = CreateEncryptionService();
            var userContext = new UserContext(httpContextAccessor.Object, configuration.Object, encryptionService.Object);

            // Act
            var userId = userContext.UserId;

            // Assert
            Assert.NotNull(userId);
            Assert.NotEqual(TestObjectId, userId);
        }

        [Fact]
        public void IsAuthenticated_ReturnsFalse_WhenNotAuthenticated()
        {
            // Arrange
            var httpContextAccessor = CreateHttpContextAccessor(isAuthenticated: false);
            var configuration = CreateConfiguration();
            var encryptionService = CreateEncryptionService();
            var userContext = new UserContext(httpContextAccessor.Object, configuration.Object, encryptionService.Object);

            // Act
            var isAuthenticated = userContext.IsAuthenticated;

            // Assert
            Assert.False(isAuthenticated);
        }

        [Fact]
        public void IsAuthenticated_ReturnsFalse_WhenInvalidAudience()
        {
            // Arrange
            var httpContextAccessor = CreateHttpContextAccessor(
                isAuthenticated: true, 
                audience: "wrong-audience");
            var configuration = CreateConfiguration();
            var encryptionService = CreateEncryptionService();
            var userContext = new UserContext(httpContextAccessor.Object, configuration.Object, encryptionService.Object);

            // Act
            var isAuthenticated = userContext.IsAuthenticated;

            // Assert
            Assert.False(isAuthenticated);
        }

        [Fact]
        public void IsAuthenticated_ReturnsTrue_WhenAuthenticatedWithValidAudience()
        {
            // Arrange
            var httpContextAccessor = CreateHttpContextAccessor(
                isAuthenticated: true, 
                audience: TestAppId);
            var configuration = CreateConfiguration();
            var encryptionService = CreateEncryptionService();
            var userContext = new UserContext(httpContextAccessor.Object, configuration.Object, encryptionService.Object);

            // Act
            var isAuthenticated = userContext.IsAuthenticated;

            // Assert
            Assert.True(isAuthenticated);
        }

        [Fact]
        public void HasValidAudience_ReturnsFalse_WhenNotAuthenticated()
        {
            // Arrange
            var httpContextAccessor = CreateHttpContextAccessor(isAuthenticated: false);
            var configuration = CreateConfiguration();
            var encryptionService = CreateEncryptionService();
            var userContext = new UserContext(httpContextAccessor.Object, configuration.Object, encryptionService.Object);

            // Act
            var hasValidAudience = userContext.HasValidAudience;

            // Assert
            Assert.False(hasValidAudience);
        }

        [Fact]
        public void HasValidAudience_ReturnsFalse_WhenInvalidAudience()
        {
            // Arrange
            var httpContextAccessor = CreateHttpContextAccessor(
                isAuthenticated: true, 
                audience: "wrong-audience");
            var configuration = CreateConfiguration();
            var encryptionService = CreateEncryptionService();
            var userContext = new UserContext(httpContextAccessor.Object, configuration.Object, encryptionService.Object);

            // Act
            var hasValidAudience = userContext.HasValidAudience;

            // Assert
            Assert.False(hasValidAudience);
        }

        [Fact]
        public void HasValidAudience_ReturnsTrue_WhenAuthenticatedWithValidAudience()
        {
            // Arrange
            var httpContextAccessor = CreateHttpContextAccessor(
                isAuthenticated: true, 
                audience: TestAppId);
            var configuration = CreateConfiguration();
            var encryptionService = CreateEncryptionService();
            var userContext = new UserContext(httpContextAccessor.Object, configuration.Object, encryptionService.Object);

            // Act
            var hasValidAudience = userContext.HasValidAudience;

            // Assert
            Assert.True(hasValidAudience);
        }

                [Fact]
        public void IsAuthenticated_WhenUserIsAuthenticatedAndAudienceValid_ReturnsTrue()
        {
            // Arrange
            SetupAuthenticatedUser(true, "test-client-id");

            // Act
            bool result = _userContext.IsAuthenticated;

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsAuthenticated_WhenUserIsNotAuthenticated_ReturnsFalse()
        {
            // Arrange
            SetupAuthenticatedUser(false, "test-client-id");

            // Act
            bool result = _userContext.IsAuthenticated;

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void HasValidAudience_WhenAudienceMatchesConfig_ReturnsTrue()
        {
            // Arrange
            SetupAuthenticatedUser(true, "test-client-id");

            // Act
            bool result = _userContext.HasValidAudience;

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void HasValidAudience_WhenAudienceDoesNotMatchConfig_ReturnsFalse()
        {
            // Arrange
            SetupAuthenticatedUser(true, "wrong-client-id");

            // Act
            bool result = _userContext.HasValidAudience;

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void UserId_WhenUserIsAuthenticatedAndValidAudience_ReturnsUserId()
        {
            // Arrange
            string objectId = Guid.NewGuid().ToString();
            SetupAuthenticatedUserWithObjectId(true, "test-client-id", objectId);
            _mockEncryptionService.Setup(x => x.Encrypt(objectId)).Returns("encrypted-id");

            // Act
            string? result = _userContext.UserId;

            // Assert
            Assert.NotNull(result);
            Assert.NotEqual(objectId.ToString(), result);
            Assert.NotEqual("encrypted-id", result);
        }

        [Fact]
        public void UserId_WhenUserIsNotAuthenticated_ReturnsNull()
        {
            // Arrange
            SetupAuthenticatedUser(false, "test-client-id");

            // Act
            string? result = _userContext.UserId;

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void RequireUserId_WhenUserIsAuthenticated_ReturnsUserId()
        {
            // Arrange
            string objectId = Guid.NewGuid().ToString();
            SetupAuthenticatedUserWithObjectId(true, "test-client-id", objectId);
            _mockEncryptionService.Setup(x => x.Encrypt(objectId)).Returns("encrypted-id");

            // Act
            string result = _userContext.RequireUserId();

            // Assert
            Assert.NotNull(result);
            Assert.NotEqual(objectId.ToString(), result);
            Assert.NotEqual("encrypted-id", result);
        }

        [Fact]
        public void RequireUserId_WhenUserIsNotAuthenticated_ThrowsUnauthorizedAccessException()
        {
            // Arrange
            SetupAuthenticatedUser(false, "test-client-id");

            // Act & Assert
            Assert.Throws<UnauthorizedAccessException>(() => _userContext.RequireUserId());
        }

        [Fact]
        public void RequireUserId_WithDifferentObjectIds_ReturnsDifferentValues()
        {
            // Arrange
            string objectId1 = Guid.NewGuid().ToString();
            string objectId2 = Guid.NewGuid().ToString();

            // Setup for first object ID
            SetupAuthenticatedUserWithObjectId(true, "test-client-id", objectId1);
            _mockEncryptionService.Setup(x => x.Encrypt(objectId1)).Returns("encrypted-id-1");
            string result1 = _userContext.RequireUserId();

            // Setup for second object ID
            SetupAuthenticatedUserWithObjectId(true, "test-client-id", objectId2);
            _mockEncryptionService.Setup(x => x.Encrypt(objectId2)).Returns("encrypted-id-2");
            string result2 = _userContext.RequireUserId();

            // Act & Assert
            Assert.NotEqual(result1, result2);
            Assert.NotEqual(objectId1.ToString(), result1);
            Assert.NotEqual(objectId2.ToString(), result2);
            Assert.NotEqual("encrypted-id-1", result1);
            Assert.NotEqual("encrypted-id-2", result2);
        }

        [Fact]
        public void RequireUserId_WithRealEncryptionService_ProducesConsistentValues()
        {
            // Arrange
            string objectId = Guid.NewGuid().ToString();
            IEncryptionService realEncryptionService = new EncryptionService(_mockConfiguration.Object);
            
            // Create a UserContext with the real encryption service
            UserContext userContextWithRealEncryption = new UserContext(
            _mockHttpContextAccessor.Object,
            _mockConfiguration.Object,
            realEncryptionService);
            
            // Setup authenticated user
            SetupAuthenticatedUserWithObjectId(true, "test-client-id", objectId);
            
            // Act
            string result1 = userContextWithRealEncryption.RequireUserId();
            string result2 = userContextWithRealEncryption.RequireUserId();
            
            // Assert
            Assert.NotNull(result1);
            Assert.Equal(result1, result2); // Same input should produce same output
            
            // Validate format (assuming the real service produces alphanumeric output)
            Assert.Matches("^[a-zA-Z0-9]+$", result1);
        }

        [Fact]
        public void RequireUserId_WithRealEncryptionService_ProducesDifferentValues()
        {
            // Arrange
            string objectId1 = Guid.NewGuid().ToString();
            string objectId2 = Guid.NewGuid().ToString();
            IEncryptionService realEncryptionService = new EncryptionService(_mockConfiguration.Object);
            
            // Create a UserContext with the real encryption service
            UserContext userContextWithRealEncryption = new UserContext(
            _mockHttpContextAccessor.Object,
            _mockConfiguration.Object,
            realEncryptionService);
            
            // Act
            SetupAuthenticatedUserWithObjectId(true, "test-client-id", objectId1);
            string result1 = userContextWithRealEncryption.RequireUserId();

            SetupAuthenticatedUserWithObjectId(true, "test-client-id", objectId2);
            string result2 = userContextWithRealEncryption.RequireUserId();
            
            // Assert
            Assert.NotNull(result1);
            Assert.NotNull(result2);
            Assert.NotEqual(result1, result2); // Same input should produce same output
            
            // Validate format (assuming the real service produces alphanumeric output)
            Assert.Matches("^[a-zA-Z0-9]+$", result1);
            Assert.Matches("^[a-zA-Z0-9]+$", result2);
        }

        [Fact]
        public void RequireUserId_WithNullObjectIds_Fails()
        {
            // Setup for null object ID
            SetupAuthenticatedUserWithObjectId(true, "test-client-id", null);
            Assert.Throws<UnauthorizedAccessException>(() => _userContext.RequireUserId()); // Throws UnauthorizedAccessException when ObjectId is null
        }

        private void SetupAuthenticatedUser(bool isAuthenticated, string audience)
        {
            var identity = new Mock<ClaimsIdentity>();
            identity.Setup(x => x.IsAuthenticated).Returns(isAuthenticated);
            
            var claimsPrincipal = new ClaimsPrincipal(identity.Object);
            if (!string.IsNullOrEmpty(audience))
                claimsPrincipal.AddIdentity(new ClaimsIdentity(new[] { new Claim("aud", audience) }));
            
            _httpContext.User = claimsPrincipal;
        }

        private void SetupAuthenticatedUserWithObjectId(bool isAuthenticated, string audience, string? objectId)
        {
            SetupAuthenticatedUser(isAuthenticated, audience);
            
            var claims = new List<Claim>();
            if (audience != null)
                claims.Add(new Claim("aud", audience));
            if (objectId != null)
                claims.Add(new Claim("http://schemas.microsoft.com/identity/claims/objectidentifier", objectId));
            
            var identity = new ClaimsIdentity(claims, "TestAuth");
            identity.AddClaim(new Claim(ClaimTypes.Name, "TestUser"));
            
            var claimsPrincipal = new ClaimsPrincipal(identity);
            _httpContext.User = claimsPrincipal;
        }

        private Mock<IHttpContextAccessor> CreateHttpContextAccessor(
            bool isAuthenticated, 
            string? audience = null, 
            string? objectId = null)
        {
            var identity = new Mock<ClaimsIdentity>();
            identity.Setup(i => i.IsAuthenticated).Returns(isAuthenticated);

            var claims = new List<Claim>();
            if (audience != null)
            {
                claims.Add(new Claim("aud", audience));
            }
            if (objectId != null)
            {
                claims.Add(new Claim("http://schemas.microsoft.com/identity/claims/objectidentifier", objectId));
            }

            var user = new ClaimsPrincipal(identity.Object);
            user.AddIdentity(new ClaimsIdentity(claims));

            var context = new DefaultHttpContext
            {
                User = user
            };

            var httpContextAccessor = new Mock<IHttpContextAccessor>();
            httpContextAccessor.Setup(x => x.HttpContext).Returns(context);

            return httpContextAccessor;
        }

        private Mock<IConfiguration> CreateConfiguration()
        {
            var configSection = new Mock<IConfigurationSection>();
            configSection.Setup(x => x.Value).Returns(TestAppId);

            var configuration = new Mock<IConfiguration>();
            configuration.Setup(x => x["AzureAd:ClientId"]).Returns(TestAppId);

            return configuration;
        }

        private Mock<IEncryptionService> CreateEncryptionService()
        {
            var encryptionService = new Mock<IEncryptionService>();
            encryptionService.Setup(x => x.Encrypt(It.IsAny<string>())).Returns((string s) => "ENCRYPTED: " + s);
            encryptionService.Setup(x => x.Decrypt(It.IsAny<string>())).Returns((string s) => s.Replace("ENCRYPTED: ", ""));
            return encryptionService;
        }
    }
}
