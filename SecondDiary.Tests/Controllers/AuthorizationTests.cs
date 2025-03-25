using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;

namespace SecondDiary.Tests.Controllers
{
    public class AuthorizationTests : IClassFixture<WebApplicationFactory<SecondDiary.API.Program>>
    {
        private readonly WebApplicationFactory<SecondDiary.API.Program> _factory;

        public AuthorizationTests(WebApplicationFactory<SecondDiary.API.Program> factory)
        {
            _factory = factory;
        }

        [Theory]
        [InlineData("/api/diary")]
        [InlineData("/api/diary/recommendation")]
        [InlineData("/api/systemprompt")]
        [InlineData("/api/systemprompt/recommendations")]
        [InlineData("/api/auth/me")]
        [InlineData("/api/auth/login")]
        public async Task ProtectedEndpoints_Return401_WithoutToken(string endpoint)
        {
            // Arrange
            var client = _factory.CreateClient();

            // Act
            var response = await client.GetAsync(endpoint);

            // Assert
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Theory]
        [InlineData("/api/auth/config")]
        public async Task PublicEndpoints_Return200_WithoutToken(string endpoint)
        {
            // Arrange
            var client = _factory.CreateClient();

            // Act
            var response = await client.GetAsync(endpoint);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Theory]
        [InlineData("/api/auth/logout")]
        public async Task OptionalAuthEndpoints_DoNotReturn401_WithoutToken(string endpoint)
        {
            // Arrange
            var client = _factory.CreateClient();

            // Act
            var response = await client.GetAsync(endpoint);

            // Assert
            Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
        }
    }
}
