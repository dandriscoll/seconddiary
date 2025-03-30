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

        public UserContext(IHttpContextAccessor httpContextAccessor, IConfiguration configuration)
        {
            _httpContextAccessor = httpContextAccessor;
            _configuration = configuration;
        }

        public string? UserId => IsAuthenticated && HasValidAudience
            ? _httpContextAccessor.HttpContext?.User.GetObjectId()
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
    }
}
