using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace SecondDiary.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public AuthController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpGet("me")]
        [Authorize]
        public IActionResult GetCurrentUser()
        {
            if (User.Identity == null || !User.Identity.IsAuthenticated)
                return Unauthorized(new { message = "User identity not available or not authenticated" });

            return Ok(new
            {
                Name = User.Identity.Name,
                Claims = User.Claims.Select(c => new { c.Type, c.Value })
            });
        }

        [HttpGet("login")]
        public IActionResult Login(string returnUrl = "/")
        {
            return Challenge(new AuthenticationProperties { RedirectUri = returnUrl });
        }

        [HttpGet("logout")]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync();
            return Ok(new { message = "Logged out successfully" });
        }

        [HttpGet("config")]
        public IActionResult GetAuthConfig()
        {
            var authConfig = new
            {
                ClientId = _configuration["AzureAd:ClientId"],
                TenantId = _configuration["AzureAd:TenantId"]
            };

            return Ok(authConfig);
        }
    }
}
