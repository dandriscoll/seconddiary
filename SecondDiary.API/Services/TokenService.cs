using Microsoft.Identity.Web;

namespace SecondDiary.API.Services
{
    public interface ITokenService
    {
        string? GetUserIdFromToken(string? token);
        bool ValidateToken(string? token);
    }

    public class TokenService : ITokenService
    {
        private readonly IConfiguration _configuration;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public TokenService(IConfiguration configuration, IHttpContextAccessor httpContextAccessor)
        {
            _configuration = configuration;
            _httpContextAccessor = httpContextAccessor;
        }

        public string? GetUserIdFromToken(string? token)
        {
            // For AAD tokens, it's generally better to rely on the built-in validation
            // that happens through Microsoft.Identity.Web and then access the claims
            // from the HttpContext rather than manually validating tokens
            
            if (_httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated == true)
            {
                // Use the object ID claim for AAD users
                return _httpContextAccessor.HttpContext.User.GetObjectId();
            }
            
            return null;
        }

        public bool ValidateToken(string? token)
        {
            return !string.IsNullOrEmpty(GetUserIdFromToken(token));
        }
    }
}
