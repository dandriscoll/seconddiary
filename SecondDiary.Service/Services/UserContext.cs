using System.Text.RegularExpressions;
using Microsoft.Identity.Web;

namespace SecondDiary.Services
{
    public interface IUserContext
    {
        string? UserId { get; }
        bool IsAuthenticated { get; }
    }

    public class UserContext : IUserContext
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IConfiguration _configuration;
        private readonly IEncryptionService _encryptionService;

        public UserContext(
            IHttpContextAccessor httpContextAccessor, 
            IConfiguration configuration,
            IEncryptionService encryptionService)
        {
            _httpContextAccessor = httpContextAccessor;
            _configuration = configuration;
            _encryptionService = encryptionService;
        }

        public string? UserId => IsAuthenticated && HasValidAudience
            ? SanitizeUserId(_encryptionService.Encrypt(_httpContextAccessor.HttpContext?.User.GetObjectId()!))
            : null;

        public bool IsAuthenticated => (_httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false) && HasValidAudience;

        public bool HasValidAudience
        {
            get
            {
                var expectedAudience = _configuration["AzureAd:ClientId"];
                if (string.IsNullOrEmpty(expectedAudience))
                    return false;

                var audienceClaim = _httpContextAccessor.HttpContext?.User?.FindFirst("aud");
                return audienceClaim != null && audienceClaim.Value == expectedAudience;
            }
        }

        private string SanitizeUserId(string encryptedId)
        {
            if (string.IsNullOrEmpty(encryptedId))
                return string.Empty;
            
            // Remove special characters, keeping only alphanumeric
            string sanitized = Regex.Replace(encryptedId, "[^a-zA-Z0-9]", "");
            
            // Trim to specified length
            if (sanitized.Length > 20)
                sanitized = sanitized.Substring(0, 20);
                
            return sanitized;
        }
    }
}
