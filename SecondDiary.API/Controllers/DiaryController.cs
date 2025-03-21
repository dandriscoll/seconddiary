using Microsoft.AspNetCore.Mvc;
using SecondDiary.API.Models;
using SecondDiary.API.Services;

namespace SecondDiary.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DiaryController : ControllerBase
    {
        private readonly IDiaryService _diaryService;
        private readonly IWebHostEnvironment _environment;

        public DiaryController(
            IDiaryService diaryService,
            IWebHostEnvironment environment)
        {
            _diaryService = diaryService ?? throw new ArgumentNullException(nameof(diaryService));
            _environment = environment ?? throw new ArgumentNullException(nameof(environment));
        }

        private string GetUserId()
        {
            if (_environment.IsDevelopment())
            {
                return "development-user";
            }

            var userName = User.Identity?.Name;
            if (string.IsNullOrEmpty(userName))
            {
                throw new UnauthorizedAccessException("User not authenticated");
            }
            return userName;
        }

        [HttpPost]
        public async Task<ActionResult<DiaryEntry>> CreateEntry([FromBody] DiaryEntryRequest? request)
        {
            if (request == null)
            {
                return BadRequest("Request body cannot be null");
            }

            if (string.IsNullOrEmpty(request.Thought))
            {
                return BadRequest("Thought cannot be empty");
            }

            var entry = new DiaryEntry
            {
                UserId = GetUserId(),
                Date = DateTime.UtcNow,
                Thought = request.Thought
            };

            var createdEntry = await _diaryService.CreateEntryAsync(entry);
            return CreatedAtAction(nameof(GetEntry), new { id = createdEntry.Id }, createdEntry);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<DiaryEntry>> GetEntry(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return BadRequest("Entry ID cannot be empty");
            }

            var entry = await _diaryService.GetEntryAsync(id, GetUserId());
            if (entry == null)
            {
                return NotFound();
            }

            return Ok(entry);
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<DiaryEntry>>> GetEntries()
        {
            var entries = await _diaryService.GetEntriesAsync(GetUserId());
            return Ok(entries);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<DiaryEntry>> UpdateEntry(string id, [FromBody] DiaryEntryRequest? request)
        {
            if (string.IsNullOrEmpty(id))
            {
                return BadRequest("Entry ID cannot be empty");
            }

            if (request == null)
            {
                return BadRequest("Request body cannot be null");
            }

            if (string.IsNullOrEmpty(request.Thought))
            {
                return BadRequest("Thought cannot be empty");
            }

            var existingEntry = await _diaryService.GetEntryAsync(id, GetUserId());
            if (existingEntry == null)
            {
                return NotFound();
            }

            existingEntry.Thought = request.Thought;

            var updatedEntry = await _diaryService.UpdateEntryAsync(existingEntry);
            return Ok(updatedEntry);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteEntry(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return BadRequest("Entry ID cannot be empty");
            }

            var entry = await _diaryService.GetEntryAsync(id, GetUserId());
            if (entry == null)
            {
                return NotFound();
            }

            await _diaryService.DeleteEntryAsync(id, GetUserId());
            return NoContent();
        }

        [HttpGet("recommendation")]
        public async Task<ActionResult<string>> GetRecommendation()
        {
            var recommendation = await _diaryService.GetRecommendationAsync(GetUserId());
            return Ok(recommendation);
        }
    }
} 