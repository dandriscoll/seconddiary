using Microsoft.AspNetCore.Mvc;
using Moq;
using SecondDiary.API.Controllers;
using SecondDiary.API.Services;
using System.Security.Claims;
using Xunit;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System.Text.Json;

namespace SecondDiary.Tests.Controllers
{
    public class AuthControllerTests
    {
        private const string TestUserId = "test-user-id";
        private const string TestUserName = "Test User";

        [Fact]
        public void GetCurrentUser_ReturnsUnauthorized_WhenNotAuthenticated()
        {
            // Arrange
            var userContext = new Mock<IUserContext>();
            userContext.Setup(x => x.IsAuthenticated).Returns(false);
            userContext.Setup(x => x.UserId).Returns((string?)null);

            var configuration = new Mock<IConfiguration>();
            var controller = new AuthController(configuration.Object, userContext.Object);
            controller.ControllerContext = CreateControllerContext(isAuthenticated: false);

            // Act
            var result = controller.GetCurrentUser();

            // Assert
            Assert.IsType<UnauthorizedObjectResult>(result);
        }

        [Fact]
        public void GetCurrentUser_ReturnsOk_WhenAuthenticated()
        {
            // Arrange
            var userContext = new Mock<IUserContext>();
            userContext.Setup(x => x.IsAuthenticated).Returns(true);
            userContext.Setup(x => x.UserId).Returns(TestUserId);

            var configuration = new Mock<IConfiguration>();
            var controller = new AuthController(configuration.Object, userContext.Object);
            controller.ControllerContext = CreateControllerContext(isAuthenticated: true);

            // Act
            var result = controller.GetCurrentUser();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
            
            // Use JsonElement to safely access dynamic properties
            var jsonValue = JsonSerializer.Serialize(okResult.Value);
            var element = JsonSerializer.Deserialize<JsonElement>(jsonValue);
            
            Assert.True(element.TryGetProperty("UserId", out var userIdElement));
            Assert.Equal(TestUserId, userIdElement.GetString());
        }

        private ControllerContext CreateControllerContext(bool isAuthenticated)
        {
            var identity = new ClaimsIdentity();
            if (isAuthenticated)
            {
                identity = new ClaimsIdentity(new Claim[]
                {
                    new Claim(ClaimTypes.Name, TestUserName),
                    new Claim(ClaimTypes.NameIdentifier, TestUserId),
                    new Claim("http://schemas.microsoft.com/identity/claims/objectidentifier", TestUserId)
                }, "TestAuth");
            }

            var user = new ClaimsPrincipal(identity);
            return new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            };
        }
    }
}
