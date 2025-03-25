using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Identity.Web;
using SecondDiary.API.Services;

namespace SecondDiary.API
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add controllers and other services
            builder.Services.AddControllers();
            
            // Add Microsoft Identity authentication with Azure AD
            builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddMicrosoftIdentityWebApi(options =>
                {
                    builder.Configuration.GetSection("AzureAd").Bind(options);
                    
                    // Enforce audience validation
                    options.TokenValidationParameters.ValidateAudience = true;
                    options.TokenValidationParameters.ValidAudience = builder.Configuration["AzureAd:ClientId"];
                    
                    // Additional security settings
                    options.TokenValidationParameters.ValidateIssuer = true;
                    options.TokenValidationParameters.ValidateLifetime = true;
                }, options => { builder.Configuration.GetSection("AzureAd").Bind(options); });

            // Add HttpContextAccessor for user context
            builder.Services.AddHttpContextAccessor();
            
            // Add application services
            builder.Services.AddScoped<ITokenService, TokenService>();
            builder.Services.AddScoped<IUserContext, UserContext>();

            var app = builder.Build();

            // Enable authentication and authorization
            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }
    }
}
