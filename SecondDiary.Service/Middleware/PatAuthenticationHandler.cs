using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using SecondDiary.Models;
using SecondDiary.Services;

namespace SecondDiary.Middleware
{
    public class PatAuthenticationSchemeOptions : AuthenticationSchemeOptions
    {
        public const string DefaultScheme = "PAT";
    }

    public class PatAuthenticationHandler : AuthenticationHandler<PatAuthenticationSchemeOptions>
    {
        private readonly IPersonalAccessTokenService _patService;
        private readonly ILogger<PatAuthenticationHandler> _logger;

        public PatAuthenticationHandler(
            IOptionsMonitor<PatAuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder,
            IPersonalAccessTokenService patService)
            : base(options, logger, encoder)
        {
            _patService = patService;
            _logger = logger.CreateLogger<PatAuthenticationHandler>();
        }

        protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            // Check for Authorization header
            if (!Request.Headers.ContainsKey("Authorization"))
                return AuthenticateResult.NoResult();

            string authHeader = Request.Headers["Authorization"].ToString();
            
            // Check if it's a Bearer token
            if (!authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                return AuthenticateResult.NoResult();

            string token = authHeader.Substring("Bearer ".Length).Trim();
            
            // Check if it looks like a PAT (starts with our prefix)
            if (!token.StartsWith("p_"))
                return AuthenticateResult.NoResult();

            try
            {
                PersonalAccessToken? patToken = await _patService.ValidateTokenAsync(token);
                if (patToken == null)
                {
                    _logger.LogWarning("Invalid PAT token provided from IP {RemoteIpAddress}", 
                        Context.Connection.RemoteIpAddress);
                    return AuthenticateResult.Fail("Invalid token");
                }

                List<Claim> claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, $"PAT-{patToken.TokenPrefix}"),
                    new Claim(ClaimTypes.NameIdentifier, patToken.UserId),
                    new Claim("http://schemas.microsoft.com/identity/claims/objectidentifier", patToken.UserId),
                    new Claim("pat_id", patToken.Id),
                    new Claim("auth_method", "pat")
                };

                ClaimsIdentity identity = new ClaimsIdentity(claims, Scheme.Name);
                ClaimsPrincipal principal = new ClaimsPrincipal(identity);
                AuthenticationTicket ticket = new AuthenticationTicket(principal, Scheme.Name);

                _logger.LogInformation("Successfully authenticated user {UserId} with PAT {TokenId}", 
                    patToken.UserId, patToken.Id);

                return AuthenticateResult.Success(ticket);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during PAT authentication");
                return AuthenticateResult.Fail("Authentication error");
            }
        }
    }

    public static class PatAuthenticationExtensions
    {
        public static AuthenticationBuilder AddPatAuthentication(this AuthenticationBuilder builder)
        {
            return builder.AddScheme<PatAuthenticationSchemeOptions, PatAuthenticationHandler>(
                PatAuthenticationSchemeOptions.DefaultScheme, 
                options => { });
        }
    }
}
