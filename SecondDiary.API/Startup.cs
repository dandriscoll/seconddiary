using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.MicrosoftAccount;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using SecondDiary.API.Models;
using SecondDiary.API.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Web;
using System.IO;

namespace SecondDiary.API
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            // Configure JWT authentication
            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.Authority = $"https://login.microsoftonline.com/{Configuration["AzureAd:TenantId"]}/v2.0";
                    options.Audience = Configuration["AzureAd:ClientId"];
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = $"https://login.microsoftonline.com/{Configuration["AzureAd:TenantId"]}/v2.0",
                        ValidAudience = Configuration["AzureAd:ClientId"]
                    };
                });

            services.AddControllers()
                .AddNewtonsoftJson(options =>
                {
                    options.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
                    options.SerializerSettings.NullValueHandling = NullValueHandling.Ignore;
                });
            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen();

            // Configure Cosmos DB
            services.Configure<CosmosDbSettings>(Configuration.GetSection("CosmosDb"));
            services.AddSingleton<ICosmosDbService, CosmosDbService>();

            // Configure Encryption Service
            services.AddSingleton<IEncryptionService, EncryptionService>();

            // Configure Microsoft Account Authentication
            var clientId = Configuration["Authentication:Microsoft:ClientId"];
            var clientSecret = Configuration["Authentication:Microsoft:ClientSecret"];

            if (!string.IsNullOrEmpty(clientId) && !string.IsNullOrEmpty(clientSecret))
            {
                services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                    options.DefaultChallengeScheme = MicrosoftAccountDefaults.AuthenticationScheme;
                })
                .AddMicrosoftAccount(options =>
                {
                    options.ClientId = clientId;
                    options.ClientSecret = clientSecret;
                })
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = "https://login.microsoftonline.com/common/v2.0",
                        ValidAudience = clientId
                    };
                });
            }

            // Register services with their interfaces
            services.AddSingleton<IDiaryService, DiaryService>();
            services.AddSingleton<ISystemPromptService, SystemPromptService>();

            // Add authorization policies if needed
            services.AddAuthorization();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, ILogger<Startup> logger)
        {
            if (env.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
                app.UseDeveloperExceptionPage();
            }

            app.UseHttpsRedirection();
            
            // Default static files middleware
            app.UseStaticFiles();
            
            // Add ClientApp static files with correct path mapping
            var clientAppPath = Path.Combine(Directory.GetCurrentDirectory(), "ClientApp/dist");
            if (Directory.Exists(clientAppPath))
            {
                app.UseStaticFiles(new StaticFileOptions
                {
                    FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(clientAppPath),
                    RequestPath = "/ClientApp/dist"
                });
            }
            
            app.UseRouting();

            // Add authentication middleware
            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapFallbackToFile("index.html");
            });
        }
    }
}