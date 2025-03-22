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

        [HttpGet]
        public async Task<ActionResult<string>> GetSystemPrompt()
        {
            string prompt = await _systemPromptService.GetSystemPromptAsync();
            return Ok(prompt);
        }

        [HttpPost]
        public async Task<IActionResult> SetSystemPrompt([FromBody] string? newPrompt)
        {
            if (string.IsNullOrEmpty(newPrompt))
                return BadRequest("System prompt cannot be empty");

            await _systemPromptService.SetSystemPromptAsync(newPrompt);
            return Ok();
        }

        [HttpPost("append")]
        public async Task<IActionResult> AppendToSystemPrompt([FromBody] string? additionalPrompt)
        {
            if (string.IsNullOrEmpty(additionalPrompt))
                return BadRequest("Additional prompt cannot be empty");

            await _systemPromptService.AppendToSystemPromptAsync(additionalPrompt);
            return Ok();
        }
    }
}