using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SecondDiary.Models;
using SecondDiary.Services;
using System.Security.Claims;

namespace SecondDiary.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PersonalAccessTokenController : ControllerBase
    {
        private readonly IPersonalAccessTokenService _patService;
        private readonly ILogger<PersonalAccessTokenController> _logger;

        public PersonalAccessTokenController(
            IPersonalAccessTokenService patService,
            ILogger<PersonalAccessTokenController> logger)
        {
            _patService = patService;
            _logger = logger;
        }

        [HttpPost]
        [Authorize]
        public async Task<ActionResult<CreatePersonalAccessTokenResponse>> CreateToken()
        {
            try
            {
                string? userId = User.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier")?.Value
                    ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized("User ID not found in token");
                }

                CreatePersonalAccessTokenResponse response = await _patService.CreateTokenAsync(userId);
                
                _logger.LogInformation("Created PAT for user {UserId}", userId);
                
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating PAT");
                return StatusCode(500, "An error occurred while creating the token");
            }
        }

        [HttpGet]
        [Authorize]
        public async Task<ActionResult<IEnumerable<PersonalAccessTokenSummary>>> GetUserTokens()
        {
            try
            {
                string? userId = User.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier")?.Value
                    ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized("User ID not found in token");
                }

                IEnumerable<PersonalAccessTokenSummary> tokens = await _patService.GetUserTokensAsync(userId);
                
                return Ok(tokens);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving PATs for user");
                return StatusCode(500, "An error occurred while retrieving tokens");
            }
        }

        [HttpDelete("{tokenId}")]
        [Authorize]
        public async Task<ActionResult> RevokeToken(string tokenId)
        {
            try
            {
                string? userId = User.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier")?.Value
                    ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized("User ID not found in token");
                }

                bool revoked = await _patService.RevokeTokenAsync(tokenId, userId);
                
                if (!revoked)
                {
                    return NotFound("Token not found");
                }

                _logger.LogInformation("Revoked PAT {TokenId} for user {UserId}", tokenId, userId);
                
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error revoking PAT {TokenId}", tokenId);
                return StatusCode(500, "An error occurred while revoking the token");
            }
        }
    }
}
