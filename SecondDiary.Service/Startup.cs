using Newtonsoft.Json;
using SecondDiary.Models;
using SecondDiary.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace SecondDiary
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

            // Configure Azure Communication Service
            services.Configure<CommunicationServiceSettings>(Configuration.GetSection("CommunicationService"));
            services.AddSingleton<IEmailService, EmailService>();
            
            // Configure the background email service
            services.AddHostedService<BackgroundEmailService>();

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
                // Using Microsoft Account consumer token Authority instead of AAD
                options.Authority = "https://login.microsoftonline.com/consumers/v2.0";
                options.Audience = Configuration["AzureAd:ClientId"];
                
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    // Using Microsoft Account consumer token Issuer
                    ValidIssuer = "https://login.microsoftonline.com/consumers/v2.0",
                    ValidAudience = Configuration["AzureAd:ClientId"],
                    NameClaimType = "name",
                };
                
#if false
                // For debugging: add error tracing for JWT token validation failures
                options.Events = new JwtBearerEvents
                {
                    OnAuthenticationFailed = context =>
                    {
                        ILogger<Startup> logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Startup>>();
                        logger.LogError($"Authentication failed: {context.Exception.Message}");
                        
                        if (context.Exception is SecurityTokenExpiredException)
                            logger.LogWarning("Token expired");
                        else if (context.Exception is SecurityTokenInvalidAudienceException)
                            logger.LogWarning("Invalid audience: {Audience}", ((SecurityTokenInvalidAudienceException)context.Exception).InvalidAudience);
                        else if (context.Exception is SecurityTokenInvalidIssuerException)
                            logger.LogWarning("Invalid issuer: {Issuer}", ((SecurityTokenInvalidIssuerException)context.Exception).InvalidIssuer);
                        else
                            logger.LogError(context.Exception, "Other token validation error");
                        
                        return Task.CompletedTask;
                    },
                    OnChallenge = context =>
                    {
                        ILogger<Startup> logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Startup>>();
                        
                        if (!context.Handled)
                            logger.LogInformation("Authentication challenge issued. Error: {Error}, ErrorDescription: {Description}", 
                                context.Error, context.ErrorDescription);
                            
                        return Task.CompletedTask;
                    },
                    OnTokenValidated = context =>
                    {
                        ILogger<Startup> logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Startup>>();
                        logger.LogInformation("Token validated successfully for user: {Name}", context.Principal.Identity.Name);
                        return Task.CompletedTask;
                    }
                };
#endif
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

            // Initialize Cosmos DB
            using (IServiceScope scope = app.ApplicationServices.CreateScope())
            {
                ICosmosDbService cosmosDbService = scope.ServiceProvider.GetRequiredService<ICosmosDbService>();
                cosmosDbService.InitializeAsync().GetAwaiter().GetResult();
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