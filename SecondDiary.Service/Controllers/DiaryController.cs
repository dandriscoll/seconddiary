using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SecondDiary.Models;
using SecondDiary.Services;

namespace SecondDiary.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public class DiaryController(
        IDiaryService diaryService,
        IOpenAIService openAIService,
        IWebHostEnvironment environment,
        IUserContext userContext) : ControllerBase
    {
        private readonly IDiaryService _diaryService = diaryService ?? throw new ArgumentNullException(nameof(diaryService));
        private readonly IOpenAIService _openAIService = openAIService ?? throw new ArgumentNullException(nameof(openAIService));
        private readonly IWebHostEnvironment _environment = environment ?? throw new ArgumentNullException(nameof(environment));
        private readonly IUserContext _userContext = userContext ?? throw new ArgumentNullException(nameof(userContext));

        [HttpPost]
        public async Task<ActionResult<DiaryEntry>> CreateEntry([FromBody] DiaryEntryRequest? request)
        {
            if (request == null)
            return BadRequest("Request body cannot be null");

            if (string.IsNullOrEmpty(request.Thought))
            return BadRequest("Thought cannot be empty");

            DateTimeOffset entryDate = DateTimeOffset.UtcNow;
            
            // Try to get date from header
            if (Request.Headers.TryGetValue("X-Entry-Date", out Microsoft.Extensions.Primitives.StringValues dateHeader) && !string.IsNullOrEmpty(dateHeader))
                if (DateTimeOffset.TryParse(dateHeader, out DateTimeOffset parsedDate))
                    entryDate = parsedDate;

            DiaryEntry entry = new DiaryEntry
            {
                UserId = _userContext.RequireUserId(),
                Date = entryDate,
                Thought = request.Thought,
                Context = request.Context
            };

            DiaryEntry createdEntry = await _diaryService.CreateEntryAsync(entry);
            return CreatedAtAction(nameof(GetEntry), new { id = createdEntry.Id }, createdEntry);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<DiaryEntry>> GetEntry(string id)
        {
            if (string.IsNullOrEmpty(id))
                return BadRequest("Entry ID cannot be empty");

            DiaryEntry? entry = await _diaryService.GetEntryAsync(id, _userContext.RequireUserId());
            if (entry == null)
                return NotFound();

            return Ok(entry);
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<DiaryEntry>>> GetEntries()
        {
            IEnumerable<DiaryEntry> entries = await _diaryService.GetEntriesAsync(_userContext.RequireUserId());
            return Ok(entries);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<DiaryEntry>> UpdateEntry(string id, [FromBody] DiaryEntryRequest? request)
        {
            if (string.IsNullOrEmpty(id))
                return BadRequest("Entry ID cannot be empty");

            if (request == null)
                return BadRequest("Request body cannot be null");

            if (string.IsNullOrEmpty(request.Thought))
                return BadRequest("Thought cannot be empty");

            DiaryEntry? existingEntry = await _diaryService.GetEntryAsync(id, _userContext.RequireUserId());
            if (existingEntry == null)
                return NotFound();

            existingEntry.Thought = request.Thought;

            DiaryEntry updatedEntry = await _diaryService.UpdateEntryAsync(existingEntry);
            return Ok(updatedEntry);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteEntry(string id)
        {
            if (string.IsNullOrEmpty(id))
                return BadRequest("Entry ID cannot be empty");

            DiaryEntry? entry = await _diaryService.GetEntryAsync(id, _userContext.RequireUserId());
            if (entry == null)
                return NotFound();

            await _diaryService.DeleteEntryAsync(id, _userContext.RequireUserId());
            return NoContent();
        }

        [HttpGet("recommendation")]
        public async Task<ActionResult<string>> GetRecommendation()
        {
            string recommendation = await _openAIService.GetRecommendationAsync(_userContext.RequireUserId());
            return Ok(recommendation);
        }
    }
}