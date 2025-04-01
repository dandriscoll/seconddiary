using Microsoft.AspNetCore.Mvc;
using SecondDiary.Controllers;
using SecondDiary.Services;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
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
            string encryptedUserId = "encrypted-test-user-id";
    
            Mock<IUserContext> userContext = new Mock<IUserContext>();
            userContext.Setup(x => x.IsAuthenticated).Returns(true);
            userContext.Setup(x => x.UserId).Returns(encryptedUserId);
            userContext.Setup(x => x.RequireUserId()).Returns(encryptedUserId);

            Mock<IConfiguration> configuration = new Mock<IConfiguration>();
            AuthController controller = new AuthController(configuration.Object, userContext.Object);
            controller.ControllerContext = CreateControllerContext(isAuthenticated: true);

            // Act
            IActionResult result = controller.GetCurrentUser();

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
            Assert.NotNull(okResult.Value);
            
            // Use JsonElement to safely access dynamic properties
            string jsonValue = JsonSerializer.Serialize(okResult.Value);
            JsonElement element = JsonSerializer.Deserialize<JsonElement>(jsonValue);
            
            Assert.True(element.TryGetProperty("UserId", out JsonElement userIdElement));
            Assert.Equal(encryptedUserId, userIdElement.GetString());
            
            // Verify RequireUserId was called
            userContext.Verify(x => x.RequireUserId(), Times.Once);
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
