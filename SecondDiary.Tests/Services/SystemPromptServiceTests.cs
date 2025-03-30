using SecondDiary.Models;
using SecondDiary.Services;
using System.Reflection;

namespace SecondDiary.Tests.Services
{
    public class SystemPromptServiceTests
    {
        private readonly Mock<ICosmosDbService> _mockCosmosDbService;
        private readonly SystemPromptService _systemPromptService;
        private readonly MethodInfo _getOrCreatePromptAsyncMethod;

        public SystemPromptServiceTests()
        {
            _mockCosmosDbService = new Mock<ICosmosDbService>();
            _systemPromptService = new SystemPromptService(_mockCosmosDbService.Object);
            
            // Get the private method via reflection
            _getOrCreatePromptAsyncMethod = typeof(SystemPromptService).GetMethod(
                "GetOrCreatePromptAsync", 
                BindingFlags.NonPublic | BindingFlags.Instance)!;
        }

        [Fact]
        public async Task GetOrCreatePromptAsync_WhenPromptExists_ReturnsExistingPrompt()
        {
            // Arrange
            string userId = "user123";
            string promptId = $"{userId}-systemprompt";
            
            SystemPrompt existingPrompt = new SystemPrompt
            {
                Id = promptId,
                UserId = userId,
                Lines = new List<string> { "Existing line 1", "Existing line 2" }
            };
            
            _mockCosmosDbService
                .Setup(x => x.GetSystemPromptAsync(promptId, userId))
                .ReturnsAsync(existingPrompt);
                
            // Act
            SystemPrompt result = await InvokeGetOrCreatePromptAsync(userId);
            
            // Assert
            Assert.Equal(existingPrompt, result);
            _mockCosmosDbService.Verify(x => x.CreateSystemPromptAsync(It.IsAny<SystemPrompt>()), Times.Never);
        }
        
        [Fact]
        public async Task GetOrCreatePromptAsync_WhenPromptDoesNotExist_CreatesAndReturnsNewPrompt()
        {
            // Arrange
            string userId = "user456";
            string promptId = $"{userId}-systemprompt";
            
            _mockCosmosDbService
                .Setup(x => x.GetSystemPromptAsync(promptId, userId))
                .ReturnsAsync((SystemPrompt?)null);
                
            SystemPrompt? capturedPrompt = null;
            _mockCosmosDbService
                .Setup(x => x.CreateSystemPromptAsync(It.IsAny<SystemPrompt>()))
                .Callback<SystemPrompt>(p => capturedPrompt = p)
                .ReturnsAsync(() => capturedPrompt!);
                
            // Act
            SystemPrompt result = await InvokeGetOrCreatePromptAsync(userId);
            
            // Assert
            Assert.NotNull(result);
            _mockCosmosDbService.Verify(x => x.CreateSystemPromptAsync(It.IsAny<SystemPrompt>()), Times.Once);
            Assert.Same(capturedPrompt, result);
        }
        
        [Fact]
        public async Task GetOrCreatePromptAsync_WhenCreatingNewPrompt_SetsCorrectProperties()
        {
            // Arrange
            string userId = "user789";
            string promptId = $"{userId}-systemprompt";
            string expectedDefaultLine = "You are a helpful AI assistant that provides thoughtful recommendations based on diary entries.";
            
            _mockCosmosDbService
                .Setup(x => x.GetSystemPromptAsync(promptId, userId))
                .ReturnsAsync((SystemPrompt?)null);
                
            SystemPrompt? capturedPrompt = null;
            _mockCosmosDbService
                .Setup(x => x.CreateSystemPromptAsync(It.IsAny<SystemPrompt>()))
                .Callback<SystemPrompt>(p => capturedPrompt = p)
                .ReturnsAsync(() => capturedPrompt!);
                
            // Act
            SystemPrompt result = await InvokeGetOrCreatePromptAsync(userId);
            
            // Assert
            Assert.Equal(promptId, result.Id);
            Assert.Equal(userId, result.UserId);
            Assert.Single(result.Lines);
            Assert.Equal(expectedDefaultLine, result.Lines[0]);
        }

        private async Task<SystemPrompt> InvokeGetOrCreatePromptAsync(string userId)
        {
            // Helper method to invoke the private method via reflection
            return await (Task<SystemPrompt>)_getOrCreatePromptAsyncMethod.Invoke(
                _systemPromptService, 
                new object[] { userId })!;
        }
    }
}