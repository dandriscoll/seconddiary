using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SecondDiary.Services;

namespace SecondDiary.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public class SystemPromptController(
        ISystemPromptService systemPromptService,
        IWebHostEnvironment environment,
        IOpenAIService openAIService,
        IUserContext userContext) : ControllerBase
    {
        private readonly ISystemPromptService _systemPromptService = systemPromptService;
        private readonly IWebHostEnvironment _environment = environment;
        private readonly IOpenAIService _openAIService = openAIService;
        private readonly IUserContext _userContext = userContext;

        [HttpGet]
        public async Task<ActionResult<string>> GetSystemPrompt()
        {
            string userId = _userContext.RequireUserId();
            string prompt = await _systemPromptService.GetSystemPromptAsync(userId);
            return Ok(prompt);
        }

        [HttpPost]
        [Consumes("text/plain")]
        public async Task<IActionResult> AddLineToPrompt()
        {
            using StreamReader reader = new StreamReader(Request.Body);
            string line = await reader.ReadToEndAsync();
            
            if (string.IsNullOrEmpty(line))
                return BadRequest("Line cannot be empty");

            string userId = _userContext.RequireUserId();
            await _systemPromptService.AddLineToPromptAsync(userId, line.Trim());
            return Ok();
        }

        [HttpDelete("line")]
        public async Task<IActionResult> RemoveLine([FromBody] string? line)
        {
            if (string.IsNullOrEmpty(line))
                return BadRequest("Line text cannot be empty");

            string userId = _userContext.RequireUserId();
            await _systemPromptService.RemoveLineAsync(userId, line.Trim());
            return Ok();
        }

        [HttpGet("recommendations")]
        public async Task<ActionResult<string>> GetRecommendations()
        {
            string userId = _userContext.RequireUserId();
            
            try
            {
                string recommendations = await _openAIService.GetRecommendationAsync(userId);
                return Ok(recommendations);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error generating recommendations: {ex.Message}");
            }
        }
    }
}