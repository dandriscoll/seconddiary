using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Hosting;
using SecondDiary.Controllers;
using SecondDiary.Services;

namespace SecondDiary.Tests.Controllers
{
    public class SystemPromptControllerTests
    {
        private readonly Mock<ISystemPromptService> _mockSystemPromptService;
        private readonly Mock<IWebHostEnvironment> _mockEnvironment;
        private readonly Mock<IOpenAIService> _mockOpenAIService;
        private readonly Mock<IUserContext> _mockUserContext;
        private readonly SystemPromptController _controller;
        private readonly string _testUserId = "test-user";

        public SystemPromptControllerTests()
        {
            _mockSystemPromptService = new Mock<ISystemPromptService>();
            _mockEnvironment = new Mock<IWebHostEnvironment>();
            _mockOpenAIService = new Mock<IOpenAIService>();
            _mockUserContext = new Mock<IUserContext>();
            
            // Set up user context
            _mockUserContext.Setup(u => u.UserId).Returns(_testUserId);
            _mockUserContext.Setup(u => u.RequireUserId()).Returns(_testUserId);
            
            _controller = new SystemPromptController(
                _mockSystemPromptService.Object, 
                _mockEnvironment.Object, 
                _mockOpenAIService.Object, 
                _mockUserContext.Object);
        }

        [Fact]
        public async Task GetSystemPrompts_ReturnsOkResult_WithListOfPrompts()
        {
            // Arrange
            string expectedPrompt = "You are a helpful AI assistant that provides thoughtful recommendations based on diary entries.";
            _mockSystemPromptService.Setup(service => service.GetSystemPromptAsync(_testUserId))
                .ReturnsAsync(expectedPrompt);

            // Act
            ActionResult<string> result = await _controller.GetSystemPrompt();

            // Assert
            OkObjectResult okResult = Assert.IsType<OkObjectResult>(result.Result);
            string returnedPrompt = Assert.IsType<string>(okResult.Value);
            Assert.Equal(expectedPrompt, returnedPrompt);
        }
    }
}
