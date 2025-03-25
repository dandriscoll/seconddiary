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

        public SystemPromptController(
            ISystemPromptService systemPromptService,
            IWebHostEnvironment environment)
        {
            _systemPromptService = systemPromptService ?? throw new ArgumentNullException(nameof(systemPromptService));
            _environment = environment ?? throw new ArgumentNullException(nameof(environment));
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
    }
}