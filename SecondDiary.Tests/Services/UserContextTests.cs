using Microsoft.AspNetCore.Http;
using SecondDiary.Services;
using System.Security.Claims;

namespace SecondDiary.Tests.Services
{
    public class UserContextTests
    {
        private const string TestObjectId = "test-object-id";
        private const string TestAppId = "test-app-id";

        [Fact]
        public void UserId_ReturnsNull_WhenNotAuthenticated()
        {
            // Arrange
            var httpContextAccessor = CreateHttpContextAccessor(isAuthenticated: false);
            var configuration = CreateConfiguration();
            var userContext = new UserContext(httpContextAccessor.Object, configuration.Object);

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
            var userContext = new UserContext(httpContextAccessor.Object, configuration.Object);

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
            var userContext = new UserContext(httpContextAccessor.Object, configuration.Object);

            // Act
            var userId = userContext.UserId;

            // Assert
            Assert.Equal(TestObjectId, userId);
        }

        [Fact]
        public void IsAuthenticated_ReturnsFalse_WhenNotAuthenticated()
        {
            // Arrange
            var httpContextAccessor = CreateHttpContextAccessor(isAuthenticated: false);
            var configuration = CreateConfiguration();
            var userContext = new UserContext(httpContextAccessor.Object, configuration.Object);

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
            var userContext = new UserContext(httpContextAccessor.Object, configuration.Object);

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
            var userContext = new UserContext(httpContextAccessor.Object, configuration.Object);

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
            var userContext = new UserContext(httpContextAccessor.Object, configuration.Object);

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
            var userContext = new UserContext(httpContextAccessor.Object, configuration.Object);

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
            var userContext = new UserContext(httpContextAccessor.Object, configuration.Object);

            // Act
            var hasValidAudience = userContext.HasValidAudience;

            // Assert
            Assert.True(hasValidAudience);
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
    }
}
