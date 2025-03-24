using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;
using SecondDiary.API;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using System.IO;

namespace SecondDiary.Tests.Integration
{
    public class ApiIntegrationTests : IClassFixture<WebApplicationFactory<SecondDiary.API.Program>>
    {
        private readonly WebApplicationFactory<SecondDiary.API.Program> _factory;

        public ApiIntegrationTests(WebApplicationFactory<SecondDiary.API.Program> factory)
        {
            _factory = factory.WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Test"); // Use a specific test environment
                
                builder.ConfigureAppConfiguration((context, config) =>
                {
                    // Clear existing configuration sources
                    config.Sources.Clear();
                    
                    // Add the test-specific appsettings file
                    // Use the test project's directory to ensure we get the test version
                    var projectDir = Directory.GetCurrentDirectory();
                    var configPath = Path.Combine(projectDir, "appsettings.json");
                    
                    config.AddJsonFile(configPath, optional: false, reloadOnChange: true);
                    
                    // For debugging
                    System.Diagnostics.Debug.WriteLine($"Loading test configuration from: {configPath}");
                });
            });
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

        [Fact]
        public async Task Get_AuthConfig_ReturnsCorrectAuthSettings()
        {
            // Arrange
            HttpClient client = _factory.CreateClient();

            // Act
            HttpResponseMessage response = await client.GetAsync("/api/auth/config");

            // Assert
            response.EnsureSuccessStatusCode(); // Status Code 200-299
            
            string content = await response.Content.ReadAsStringAsync();
            Assert.NotEmpty(content);
            
            // Deserialize the response
            var authConfig = JsonSerializer.Deserialize<JsonElement>(content);
            
            // Verify auth config has the expected properties with test values
            Assert.True(authConfig.TryGetProperty("clientId", out var clientId));
            Assert.Equal("test-client-id", clientId.GetString());
            
            Assert.True(authConfig.TryGetProperty("tenantId", out var tenantId));
            Assert.Equal("test-tenant-id", tenantId.GetString());
            
            // Additional validation could include checking redirectUri, authority, etc.
        }
    }
}
