using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using SecondDiary.API.Models;
using SecondDiary.API.Services;

namespace SecondDiary.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DiaryController : ControllerBase
    {
        private readonly DiaryService _diaryService;
        private readonly IWebHostEnvironment _environment;

        public DiaryController(DiaryService diaryService, IWebHostEnvironment environment)
        {
            _diaryService = diaryService;
            _environment = environment;
        }

        [HttpPost("entry")]
        public async Task<ActionResult<DiaryEntry>> CreateEntry([FromBody] DiaryEntryRequest request)
        {
            if (string.IsNullOrEmpty(request.Thought))
            {
                return BadRequest("Thought cannot be empty");
            }

            string userId = GetUserId();
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            DiaryEntry entry = await _diaryService.CreateEntryAsync(userId, request.Context, request.Thought);
            return Ok(entry);
        }

        [HttpGet("recommendation")]
        public async Task<ActionResult<string>> GetRecommendation()
        {
            string userId = GetUserId();
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            string recommendation = await _diaryService.GetRecommendationAsync(userId);
            return Ok(recommendation);
        }

        private string GetUserId()
        {
            if (_environment.IsDevelopment())
            {
                // In development, try to get the authenticated user first
                string? authenticatedUserId = User.Identity?.Name;
                if (!string.IsNullOrEmpty(authenticatedUserId))
                {
                    return authenticatedUserId;
                }
                // If no authenticated user, use development user
                return "development-user";
            }

            // In production, always require authentication
            return User.Identity?.Name ?? string.Empty;
        }
    }
} 