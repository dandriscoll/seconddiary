using SecondDiary.Models;
using SecondDiary.Tests.Mocks;

namespace SecondDiary.Tests.Services
{
    public class SystemPromptServiceDirectTests
    {
        private readonly SystemPromptService _service;
        private readonly string _testUserId = "test-user";

        public SystemPromptServiceDirectTests()
        {
            // Create the service with built-in repository behavior
            _service = new SystemPromptService();
        }

        [Fact]
        public async Task GetSystemPromptAsync_ReturnsDefaultPrompt_WhenNoCustomPromptExists()
        {
            // Act
            string result = await _service.GetSystemPromptAsync(_testUserId);

            // Assert
            Assert.Equal("You are a helpful AI assistant that provides thoughtful recommendations based on diary entries.", result);
        }

        [Fact]
        public async Task GetSystemPromptAsync_ReturnsCustomPrompt_WhenUserPromptExists()
        {
            // Arrange
            string customPrompt = "You are a test assistant.";
            _service.AddTestUserPrompt(_testUserId, customPrompt);

            // Act
            string result = await _service.GetSystemPromptAsync(_testUserId);

            // Assert
            Assert.Equal(customPrompt, result);
        }

        [Fact]
        public async Task AddLineToPromptAsync_AppendsLine_ToExistingPrompt()
        {
            // Arrange
            string initialPrompt = "Initial prompt line.";
            string newLine = "New line to add.";
            _service.AddTestUserPrompt(_testUserId, initialPrompt);

            // Act
            await _service.AddLineToPromptAsync(_testUserId, newLine);
            string result = await _service.GetSystemPromptAsync(_testUserId);

            // Assert
            string expectedPrompt = $"{initialPrompt}{Environment.NewLine}{newLine}";
            Assert.Equal(expectedPrompt, result);
        }

        [Fact]
        public async Task RemoveLineAsync_RemovesLine_FromExistingPrompt()
        {
            // Arrange
            string lineToKeep = "This line should remain.";
            string lineToRemove = "This line should be removed.";
            string initialPrompt = $"{lineToKeep}{Environment.NewLine}{lineToRemove}";
            _service.AddTestUserPrompt(_testUserId, initialPrompt);

            // Act
            await _service.RemoveLineAsync(_testUserId, lineToRemove);
            string result = await _service.GetSystemPromptAsync(_testUserId);

            // Assert
            Assert.Equal(lineToKeep, result);
            Assert.DoesNotContain(lineToRemove, result);
        }

        [Fact]
        public async Task CreateSystemPromptAsync_CreatesNewPrompt()
        {
            // Arrange
            SystemPrompt newPrompt = new SystemPrompt
            {
                UserId = _testUserId,
                Lines = new List<string> { "Test content" }
            };

            // Act
            SystemPrompt result = await _service.CreateSystemPromptAsync(newPrompt);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Id);
            Assert.Equal(newPrompt.UserId, result.UserId);
            Assert.Equal(newPrompt.Lines, result.Lines);
        }

        [Fact]
        public async Task GetSystemPromptByIdAsync_ReturnsPrompt_WhenExists()
        {
            // Arrange
            string promptId = "test-prompt-id";
            SystemPrompt testPrompt = new SystemPrompt
            {
                Id = promptId,
                UserId = _testUserId,
                Lines = new List<string> { "Test content" }
            };
            _service.AddTestPrompt(testPrompt);

            // Act
            SystemPrompt? result = await _service.GetSystemPromptByIdAsync(promptId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(promptId, result.Id);
            Assert.Equal(testPrompt.UserId, result.UserId);
            Assert.Equal(testPrompt.Lines, result.Lines);
        }

        [Fact]
        public async Task DeleteSystemPromptAsync_RemovesPrompt()
        {
            // Arrange
            string promptId = "test-delete-prompt";
            SystemPrompt prompt = new SystemPrompt
            {
                Id = promptId,
                UserId = _testUserId,
                Lines = new List<string> { "Test delete line" }
            };
            _service.AddTestPrompt(prompt);

            // Act
            bool result = await _service.DeleteSystemPromptAsync(promptId);
            SystemPrompt? retrievedPrompt = await _service.GetSystemPromptByIdAsync(promptId);

            // Assert
            Assert.True(result);
            Assert.Null(retrievedPrompt);
        }

        [Fact]
        public async Task ListSystemPromptsAsync_ReturnsAllPrompts()
        {
            // Arrange
            _service.ClearTestData(); // Start with clean data
            
            // Add test prompts
            SystemPrompt prompt1 = new SystemPrompt 
            { 
                Id = "test-1", 
                UserId = "user1",
                Lines = new List<string> { "Content 1" } 
            };
            SystemPrompt prompt2 = new SystemPrompt 
            { 
                Id = "test-2", 
                UserId = "user2",
                Lines = new List<string> { "Content 2" } 
            };
            _service.AddTestPrompt(prompt1);
            _service.AddTestPrompt(prompt2);

            // Act
            List<SystemPrompt> results = await _service.ListSystemPromptsAsync();

            // Assert
            Assert.Equal(3, results.Count); // 2 test prompts + 1 default prompt
            Assert.Contains(results, p => p.Id == "test-1");
            Assert.Contains(results, p => p.Id == "test-2");
            Assert.Contains(results, p => p.Id == "system-default");
        }
    }
}