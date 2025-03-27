using Microsoft.Extensions.FileProviders;
using Newtonsoft.Json;
using SecondDiary.API.Models;
using SecondDiary.API.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

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

            // Register HttpContextAccessor
            services.AddHttpContextAccessor();

            // Configure Cosmos DB
            services.Configure<CosmosDbSettings>(Configuration.GetSection("CosmosDb"));
            services.AddSingleton<ICosmosDbService, CosmosDbService>();

            // Configure Azure OpenAI
            services.Configure<OpenAISettings>(Configuration.GetSection("AzureOpenAI"));
            services.AddSingleton<IOpenAIService, OpenAIService>();

            // Configure Encryption Service
            services.AddSingleton<IEncryptionService, EncryptionService>();

            // Register services with their interfaces
            services.AddSingleton<IDiaryService, DiaryService>();
            services.AddSingleton<ISystemPromptService, SystemPromptService>();
            services.AddSingleton<IUserContext, UserContext>();

            // Configure JWT Bearer Authentication with AAD validation
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.Authority = $"https://login.microsoftonline.com/{Configuration["Authentication:Microsoft:TenantId"]}/v2.0";
                options.Audience = Configuration["Authentication:Microsoft:ClientId"];
                
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = $"https://login.microsoftonline.com/{Configuration["Authentication:Microsoft:TenantId"]}/v2.0",
                    ValidAudience = Configuration["Authentication:Microsoft:ClientId"],
                    NameClaimType = "name",
                };
            });

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
            
            // Log all requests and responses
            app.Use(async (context, next) =>
            {
                logger.LogInformation($"Handling request: {context.Request.Method} {context.Request.Path}");
                await next();
                logger.LogInformation($"Response status code: {context.Response.StatusCode}");
            });

            // IMPORTANT: Static files middleware must be before routing to properly handle static content
            string wwwrootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            
            app.UseStaticFiles();

            app.UseHttpsRedirection();
            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                
                // Add this only if you're building a SPA
                endpoints.MapFallbackToFile("index.html");
            });
        }
    }
}