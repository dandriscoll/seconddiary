using Microsoft.AspNetCore.Authorization;
using SecondDiary.Models;

namespace SecondDiary.Authorization
{
    public class PatScopeRequirement : IAuthorizationRequirement
    {
        public string RequiredScope { get; }

        public PatScopeRequirement(string requiredScope)
        {
            RequiredScope = requiredScope;
        }
    }

    public class PatScopeHandler : AuthorizationHandler<PatScopeRequirement>
    {
        private readonly ILogger<PatScopeHandler> _logger;

        public PatScopeHandler(ILogger<PatScopeHandler> logger)
        {
            _logger = logger;
        }

        protected override Task HandleRequirementAsync(
            AuthorizationHandlerContext context, 
            PatScopeRequirement requirement)
        {
            // If user is authenticated via normal JWT, allow access
            string? authMethod = context.User.FindFirst("auth_method")?.Value;
            if (authMethod != "pat")
            {
                context.Succeed(requirement);
                return Task.CompletedTask;
            }

            // For PAT authentication, check scopes
            IEnumerable<string> scopes = context.User.FindAll("scope").Select(c => c.Value);
            
            if (scopes.Contains(requirement.RequiredScope))
            {
                _logger.LogDebug("PAT access granted for scope {Scope}", requirement.RequiredScope);
                context.Succeed(requirement);
            }
            else
            {
                _logger.LogWarning("PAT access denied for scope {Scope}. Available scopes: {Scopes}", 
                    requirement.RequiredScope, string.Join(", ", scopes));
                context.Fail();
            }

            return Task.CompletedTask;
        }
    }

    public static class PatScopeAuthorizationExtensions
    {
        public static void AddPatScopePolicy(this AuthorizationOptions options, string policyName, string requiredScope)
        {
            options.AddPolicy(policyName, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.AddRequirements(new PatScopeRequirement(requiredScope));
            });
        }
    }
}
