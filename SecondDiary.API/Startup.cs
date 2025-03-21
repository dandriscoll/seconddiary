using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.MicrosoftAccount;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using SecondDiary.API.Models;
using SecondDiary.API.Services;

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
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            var clientId = Configuration["Authentication:Microsoft:ClientId"];
            if (!string.IsNullOrEmpty(clientId))
            {
                app.UseAuthentication();
            }

            app.UseRouting();
            app.UseAuthorization();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
} 