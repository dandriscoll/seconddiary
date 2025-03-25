using Microsoft.AspNetCore.Mvc;
using SecondDiary.API.Services;

namespace SecondDiary.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SystemPromptController : ControllerBase
    {
        private readonly ISystemPromptService _systemPromptService;
        private readonly IWebHostEnvironment _environment;
        private readonly IOpenAIService _openAIService;

        public SystemPromptController(
            ISystemPromptService systemPromptService,
            IWebHostEnvironment environment,
            IOpenAIService openAIService)
        {
            _systemPromptService = systemPromptService ?? throw new ArgumentNullException(nameof(systemPromptService));
            _environment = environment ?? throw new ArgumentNullException(nameof(environment));
            _openAIService = openAIService ?? throw new ArgumentNullException(nameof(openAIService));
        }

        [HttpGet("{userId}")]
        public async Task<ActionResult<string>> GetSystemPrompt(string userId)
        {
            if (string.IsNullOrEmpty(userId))
                return BadRequest("User ID cannot be empty");

            string prompt = await _systemPromptService.GetSystemPromptAsync(userId);
            return Ok(prompt);
        }

        [HttpPost("{userId}/line")]
        public async Task<IActionResult> AddLineToPrompt(string userId, [FromBody] string? line)
        {
            if (string.IsNullOrEmpty(userId))
                return BadRequest("User ID cannot be empty");

            if (string.IsNullOrEmpty(line))
                return BadRequest("Line cannot be empty");

            await _systemPromptService.AddLineToPromptAsync(userId, line);
            return Ok();
        }

        [HttpDelete("{userId}/line")]
        public async Task<IActionResult> RemoveLine(string userId, [FromBody] string? line)
        {
            if (string.IsNullOrEmpty(userId))
                return BadRequest("User ID cannot be empty");

            if (string.IsNullOrEmpty(line))
                return BadRequest("Line text cannot be empty");

            await _systemPromptService.RemoveLineAsync(userId, line);
            return Ok();
        }

        [HttpGet("{userId}/recommendations")]
        public async Task<ActionResult<string>> GetRecommendations(string userId)
        {
            if (string.IsNullOrEmpty(userId))
                return BadRequest("User ID cannot be empty");

            try
            {
                string recommendations = await _openAIService.GetRecommendationsAsync(userId);
                return Ok(recommendations);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error generating recommendations: {ex.Message}");
            }
        }
    }
}