using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;
using SecondDiary.API;

namespace SecondDiary.Tests.Integration
{
    public class ApiIntegrationTests : IClassFixture<WebApplicationFactory<SecondDiary.API.Program>>
    {
        private readonly WebApplicationFactory<SecondDiary.API.Program> _factory;

        public ApiIntegrationTests(WebApplicationFactory<SecondDiary.API.Program> factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task Get_RootEndpoint_ReturnsSuccessStatusCode()
        {
            // Arrange
            HttpClient client = _factory.CreateClient();

            // Act
            HttpResponseMessage response = await client.GetAsync("/");

            // Assert
            response.EnsureSuccessStatusCode(); // Status Code 200-299
            
            string content = await response.Content.ReadAsStringAsync();
            Assert.NotEmpty(content);
            
            // Check for HTML content
            Assert.Contains("<html", content.ToLower());
            Assert.Contains("<body", content.ToLower());
        }
    }
}
