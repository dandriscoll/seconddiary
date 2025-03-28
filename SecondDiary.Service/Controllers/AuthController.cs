using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Web;
using SecondDiary.API.Services;

namespace SecondDiary.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController(IConfiguration configuration, IUserContext userContext) : ControllerBase
    {
        private readonly IConfiguration _configuration = configuration;
        private readonly IUserContext _userContext = userContext;

        [HttpGet("me")]
        [Authorize]
        public IActionResult GetCurrentUser()
        {
            if (User.Identity == null || !User.Identity.IsAuthenticated)
                return Unauthorized(new { message = "User identity not available or not authenticated" });

            return Ok(new
            {
                UserId = _userContext.UserId,
                Name = User.Identity.Name,
                ObjectId = User.GetObjectId(),
                Claims = User.Claims.Select(c => new { c.Type, c.Value })
            });
        }

        [HttpGet("login")]
        public IActionResult Login(string returnUrl = "/")
        {
            return Challenge(new AuthenticationProperties { RedirectUri = returnUrl });
        }

        [HttpGet("logout")]
        public IActionResult Logout()
        {
            // JWT tokens are stateless and can't be invalidated on the server
            // Logging out with JWTs is handled client-side by discarding the token
            
            return Ok(new { 
                message = "For JWT authentication, logout should be handled by the client by discarding the token",
                instructions = "Remove the token from your local storage or cookie"
            });
        }

        [HttpGet("config")]
        public IActionResult GetAuthConfig()
        {
            var authConfig = new
            {
                ClientId = _configuration["AzureAd:ClientId"],
                TenantId = _configuration["AzureAd:TenantId"],
                Instance = _configuration["AzureAd:Instance"]
            };

            return Ok(authConfig);
        }
    }
}
