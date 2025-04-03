using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SecondDiary.Models;
using SecondDiary.Services;
using System.Security.Claims;

namespace SecondDiary.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class EmailSettingsController : ControllerBase
    {
        private readonly ICosmosDbService _cosmosDbService;
        private readonly IUserContext _userContext;
        private readonly ILogger<EmailSettingsController> _logger;
        private readonly IEmailService _emailService;

        public EmailSettingsController(
            ICosmosDbService cosmosDbService,
            IUserContext userContext,
            ILogger<EmailSettingsController> logger,
            IEmailService emailService)
        {
            _cosmosDbService = cosmosDbService;
            _userContext = userContext;
            _logger = logger;
            _emailService = emailService;
        }

        [HttpGet]
        public async Task<IActionResult> GetEmailSettings()
        {
            string userId = _userContext.RequireUserId();
            
            try
            {
                EmailSettings? emailSettings = await _cosmosDbService.GetEmailSettingsAsync(userId);
                if (emailSettings == null)
                    return NotFound($"No email settings found for user {userId}");

                return Ok(emailSettings);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving email settings");
                return StatusCode(StatusCodes.Status500InternalServerError, "Error retrieving email settings");
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreateOrUpdateEmailSettings([FromBody] EmailSettings settings)
        {
            string userId = _userContext.RequireUserId();

            try
            {
                // Extract email from claims
                string userEmail = GetUserEmailFromClaims();
                if (string.IsNullOrEmpty(userEmail))
                    return BadRequest("User email not found in token");

                // Get existing settings or create new
                EmailSettings? existingSettings = await _cosmosDbService.GetEmailSettingsAsync(userId);
                
                if (existingSettings == null)
                {
                    // Create new settings
                    var newSettings = new EmailSettings
                    {
                        Id = Guid.NewGuid().ToString(),
                        UserId = userId,
                        Email = userEmail,
                        PreferredTime = TimeSpan.FromSeconds(settings.PreferredTime.TotalSeconds),
                        IsEnabled = settings.IsEnabled,
                        TimeZone = settings.TimeZone // Copy the timezone from the request
                    };

                    var result = await _cosmosDbService.CreateEmailSettingsAsync(newSettings);
                    return Ok(result);
                }
                else
                {
                    // Update existing settings
                    existingSettings.Email = userEmail; // Always update with the latest email from token
                    existingSettings.PreferredTime = TimeSpan.FromSeconds(settings.PreferredTime.TotalSeconds);
                    existingSettings.IsEnabled = settings.IsEnabled;
                    // Preserve timezone setting if not provided in the update
                    if (!string.IsNullOrEmpty(settings.TimeZone))
                        existingSettings.TimeZone = settings.TimeZone;

                    var result = await _cosmosDbService.UpdateEmailSettingsAsync(existingSettings);
                    return Ok(result);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving email settings");
                return StatusCode(StatusCodes.Status500InternalServerError, "Error saving email settings");
            }
        }

        [HttpDelete]
        public async Task<IActionResult> DeleteEmailSettings()
        {
            string userId = _userContext.RequireUserId();

            try
            {
                await _cosmosDbService.DeleteEmailSettingsAsync(userId);
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting email settings");
                return StatusCode(StatusCodes.Status500InternalServerError, "Error deleting email settings");
            }
        }

        [HttpPost("sendTestEmail")]
        public async Task<IActionResult> SendTestEmail()
        {
            string userId = _userContext.RequireUserId();

            try
            {
                EmailSettings? emailSettings = await _cosmosDbService.GetEmailSettingsAsync(userId);
                if (emailSettings == null)
                    return NotFound("No email settings found. Please configure your email settings first.");

                await _emailService.SendTestEmailAsync(emailSettings.Email);
                return Ok("Test email sent successfully!");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending test email");
                return StatusCode(StatusCodes.Status500InternalServerError, "Error sending test email");
            }
        }

        // Helper method to extract email from claims
        private string GetUserEmailFromClaims()
        {
            // Check 'email' claim first (most common)
            var emailClaim = User.Claims.FirstOrDefault(c => c.Type == "email" || c.Type == ClaimTypes.Email);
            if (emailClaim != null && !string.IsNullOrEmpty(emailClaim.Value))
            {
                return emailClaim.Value;
            }

            // Try alternative claims that might contain email
            var upnClaim = User.Claims.FirstOrDefault(c => c.Type == "upn" || c.Type == ClaimTypes.Upn);
            if (upnClaim != null && !string.IsNullOrEmpty(upnClaim.Value) && upnClaim.Value.Contains('@'))
            {
                return upnClaim.Value;
            }

            var nameClaim = User.Claims.FirstOrDefault(c => c.Type == "preferred_username");
            if (nameClaim != null && !string.IsNullOrEmpty(nameClaim.Value) && nameClaim.Value.Contains('@'))
            {
                return nameClaim.Value;
            }

            // If no email found, log all claims for debugging
            _logger.LogWarning("No email claim found. Available claims: " + string.Join(", ", User.Claims.Select(c => $"{c.Type}={c.Value}")));

            return string.Empty;
        }
    }
}
