using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SecondDiary.Services
{
    public class BackgroundEmailService : BackgroundService
    {
        private readonly ILogger<BackgroundEmailService> _logger;
        private readonly IServiceProvider _serviceProvider;
        
        public BackgroundEmailService(
            ILogger<BackgroundEmailService> logger,
            IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Background Email Service started at: {time}", DateTimeOffset.Now);

            // Run every minute to check for emails that need to be sent
            // This allows us to be within 5 minutes of the preferred time
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();
                        bool emailsSent = await emailService.CheckAndSendScheduledEmailsAsync();

                        if (emailsSent)
                        {
                            _logger.LogInformation("Successfully sent scheduled emails at: {time}", DateTimeOffset.Now);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred while sending scheduled emails");
                }

                // Wait for 1 minute before checking again
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }
    }
}
