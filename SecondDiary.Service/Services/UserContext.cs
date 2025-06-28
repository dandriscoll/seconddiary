using System.Text.RegularExpressions;
using System.Security.Claims;
using Microsoft.Identity.Web;

namespace SecondDiary.Services
{
    public interface IUserContext
    {
        string? UserId { get; }
        string RequireUserId();
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
            ? GetUserIdFromClaims()
            : null;

        public string RequireUserId()
        {
            string? userId = UserId;
            if (string.IsNullOrEmpty(userId))
                throw new UnauthorizedAccessException("User not authenticated or invalid audience");

            return userId;
        }

        public bool IsAuthenticated => (_httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false) && 
                                       (HasValidAudience || IsPatAuthenticated);

        public bool IsPatAuthenticated
        {
            get
            {
                string? authMethod = _httpContextAccessor.HttpContext?.User?.FindFirst("auth_method")?.Value;
                return authMethod == "pat";
            }
        }

        public bool HasValidAudience
        {
            get
            {
                // If it's PAT authentication, skip audience validation
                if (IsPatAuthenticated)
                    return true;

                var expectedAudience = _configuration["AzureAd:ClientId"];
                if (string.IsNullOrEmpty(expectedAudience))
                    return false;

                var audienceClaim = _httpContextAccessor.HttpContext?.User?.FindFirst("aud");
                return audienceClaim != null && audienceClaim.Value == expectedAudience;
            }
        }

        private string? GetUserIdFromClaims()
        {
            if (IsPatAuthenticated)
            {
                // For PAT authentication, the user ID is already encrypted and stored in NameIdentifier
                return _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            }
            
            // For JWT authentication, encrypt the object ID
            string? objectId = _httpContextAccessor.HttpContext?.User.GetObjectId();
            if (string.IsNullOrEmpty(objectId))
                return null;
                
            return SanitizeUserId(_encryptionService.Encrypt(objectId));
        }

        private string SanitizeUserId(string userId)
        {
            if (string.IsNullOrEmpty(userId))
                return string.Empty;
            
            // Remove special characters, keeping only alphanumeric
            using (System.Security.Cryptography.SHA256 sha256 = System.Security.Cryptography.SHA256.Create())
            {
                byte[] hashBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(userId));
                // Convert to Base64 and take first 20 chars (or whatever length you need)
                userId = Regex.Replace(Convert.ToBase64String(hashBytes), "[^a-zA-Z0-9]", "");
            }
            
            // Trim to specified length
            if (userId.Length > 20)
                userId = userId.Substring(0, 20);
                
            return userId;
        }
    }
}
