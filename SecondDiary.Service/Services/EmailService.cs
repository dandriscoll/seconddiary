using Azure;
using Azure.Communication.Email;
using Microsoft.Extensions.Options;
using SecondDiary.Models;

namespace SecondDiary.Services
{
    public class EmailService : IEmailService
    {
        private readonly EmailClient _emailClient;
        private readonly ILogger<EmailService> _logger;
        private readonly ICosmosDbService _cosmosDbService;
        private readonly IOpenAIService _openAIService;
        private readonly CommunicationServiceSettings _communicationSettings;

        public EmailService(
            IOptions<CommunicationServiceSettings> communicationSettings,
            ILogger<EmailService> logger,
            ICosmosDbService cosmosDbService,
            IOpenAIService openAIService)
        {
            _communicationSettings = communicationSettings.Value;
            _emailClient = new EmailClient(_communicationSettings.ConnectionString);
            _logger = logger;
            _cosmosDbService = cosmosDbService;
            _openAIService = openAIService;
        }

        private (string htmlContent, string plainTextContent) CreateEmailContent(string header, string intro, string messageContent, string outro)
        {
            string subject = $"{header} from Second Diary";
            
            string htmlContent = $@"
            <!DOCTYPE html>
            <html>
            <head>
                <meta charset='UTF-8'>
                <meta name='viewport' content='width=device-width, initial-scale=1.0'>
                <title>{header}</title>
                <style>
                    body {{
                        font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
                        line-height: 1.6;
                        color: #333;
                        max-width: 600px;
                        margin: 0 auto;
                        padding: 20px;
                    }}
                    .header {{
                        background-color: #4a6da7;
                        color: white;
                        padding: 20px;
                        text-align: center;
                        border-radius: 5px 5px 0 0;
                    }}
                    .content {{
                        padding: 20px;
                        background-color: #f9f9f9;
                        border: 1px solid #ddd;
                        border-top: none;
                        border-radius: 0 0 5px 5px;
                    }}
                    .footer {{
                        text-align: center;
                        font-size: 12px;
                        color: #777;
                        margin-top: 20px;
                    }}
                    .button {{
                        background-color: #4a6da7;
                        color: white;
                        padding: 10px 20px;
                        text-decoration: none;
                        border-radius: 5px;
                        display: inline-block;
                        margin-top: 15px;
                    }}
                </style>
            </head>
            <body>
                <div class='header'>
                    <h1>Second Diary</h1>
                    <p>{header}</p>
                </div>
                <div class='content'>
                    <p>Hello,</p>
                    <p>{intro}</p>
                    <blockquote>
                        {messageContent}
                    </blockquote>
                    <p>{outro}</p>
                    <p>
                        <a href='#YOUR_APP_URL#' class='button'>Visit Second Diary</a>
                    </p>
                </div>
                <div class='footer'>
                    <p>If you'd like to update your email preferences, please visit your account settings.</p>
                    <p>&copy; {DateTime.Now.Year} Second Diary. All rights reserved.</p>
                </div>
            </body>
            </html>";

            string plainTextContent = $@"
            SECOND DIARY - YOUR {header.ToUpper()}

            Hello,

            {intro}

            {messageContent}

            {outro}

            Visit Second Diary: #YOUR_APP_URL#

            If you'd like to update your email preferences, please visit your account settings.

            &copy; {DateTime.Now.Year} Second Diary. All rights reserved.";

            return (htmlContent, plainTextContent);
        }

        public async Task SendRecommendationEmailAsync(string userId, string emailAddress, string recommendation)
        {
            try
            {
                string subject = "Your Daily Second Diary Recommendation";
                string intro = "Based on your recent diary entries, here's a personalized recommendation for you:";
                string outro = "We hope you find this recommendation helpful and insightful.";
                var (htmlContent, plainTextContent) = CreateEmailContent("Your Daily Recommendation", intro, recommendation, outro);

                EmailContent emailContent = new EmailContent(subject)
                {
                    PlainText = plainTextContent,
                    Html = htmlContent
                };

                EmailRecipients emailRecipients = new EmailRecipients(new List<EmailAddress> { new EmailAddress(emailAddress) });

                EmailMessage emailMessage = new EmailMessage(
                    _communicationSettings.SenderEmail,
                    emailRecipients,
                    emailContent);

                try
                {
                    EmailSendOperation emailSendOperation = await _emailClient.SendAsync(
                        WaitUntil.Started,
                        emailMessage);

                    // Update the last email sent timestamp
                    var emailSettings = await _cosmosDbService.GetEmailSettingsAsync(userId);
                    if (emailSettings != null)
                    {
                        emailSettings.LastEmailSent = DateTime.UtcNow;
                        await _cosmosDbService.UpdateEmailSettingsAsync(emailSettings);
                    }

                    _logger.LogInformation($"Email sent successfully to {emailAddress}, operation ID: {emailSendOperation.Id}");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error sending email to {emailAddress}");
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in SendRecommendationEmailAsync for user {userId}");
                throw;
            }
        }

        public async Task SendTestEmailAsync(string emailAddress)
        {
            try
            {
                string subject = "Test Email from Second Diary";
                string message = "This is a test email to confirm your email settings are working correctly.";
                var (htmlContent, plainTextContent) = CreateEmailContent("Test Email", string.Empty, message, string.Empty);

                EmailContent emailContent = new EmailContent(subject)
                {
                    PlainText = plainTextContent,
                    Html = htmlContent
                };

                EmailRecipients emailRecipients = new EmailRecipients(new List<EmailAddress> { new EmailAddress(emailAddress) });

                EmailMessage emailMessage = new EmailMessage(
                    _communicationSettings.SenderEmail,
                    emailRecipients,
                    emailContent);

                EmailSendOperation emailSendOperation = await _emailClient.SendAsync(
                    WaitUntil.Started,
                    emailMessage);

                _logger.LogInformation($"Test email sent successfully to {emailAddress}, operation ID: {emailSendOperation.Id}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error sending test email to {emailAddress}");
                throw;
            }
        }

        // ...existing code...
        public async Task<bool> CheckAndSendScheduledEmailsAsync()
        {
            // ...existing code (unchanged)...
            try
            {
                _logger.LogInformation("Starting scheduled email check");
                bool sentAnyEmails = false;
                
                // Get current UTC time 
                TimeSpan currentUtcTime = DateTime.UtcNow.TimeOfDay;
                DateTime currentUtcDateTime = DateTime.UtcNow;
                
                // Get all email settings
                IEnumerable<EmailSettings> allEmailSettings = await _cosmosDbService.GetAllEmailSettingsAsync();
                
                // Process each user's email settings
                foreach (var emailSettings in allEmailSettings)
                {
                    // Skip if email settings are not enabled
                    if (!emailSettings.IsEnabled)
                        continue;
                    
                    try
                    {
                        // Convert user's preferred time to UTC based on their timezone
                        string timezone = string.IsNullOrEmpty(emailSettings.TimeZone) ? "UTC" : emailSettings.TimeZone;
                        
                        // Get current time in user's timezone
                        TimeZoneInfo userTimeZone;
                        try
                        {
                            userTimeZone = TimeZoneInfo.FindSystemTimeZoneById(timezone);
                        }
                        catch (TimeZoneNotFoundException)
                        {
                            _logger.LogWarning($"Invalid timezone '{timezone}' for user {emailSettings.UserId}, falling back to UTC");
                            userTimeZone = TimeZoneInfo.Utc;
                        }
                        
                        // Calculate the current time in user's timezone
                        DateTime currentTimeInUserTimeZone = TimeZoneInfo.ConvertTimeFromUtc(currentUtcDateTime, userTimeZone);
                        
                        // Create a DateTime object for today with the user's preferred time in their timezone
                        DateTime preferredTimeToday = currentTimeInUserTimeZone.Date.Add(emailSettings.PreferredTime);
                        
                        // Convert preferred time to UTC for comparison
                        DateTime preferredTimeInUtc = TimeZoneInfo.ConvertTimeToUtc(preferredTimeToday, userTimeZone);
                        
                        // Check if it's time to send the email (within 5 minutes of preferred time)
                        TimeSpan timeDifference = currentUtcDateTime - preferredTimeInUtc;
                        
                        if (timeDifference.TotalMinutes < 0 || timeDifference.TotalMinutes > 5)
                            continue;
                        
                        // Check if we've already sent an email today (using user's local date)
                        if (emailSettings.LastEmailSent.HasValue)
                        {
                            var lastSentUtc = emailSettings.LastEmailSent.Value;
                            var lastSentUserTime = TimeZoneInfo.ConvertTimeFromUtc(lastSentUtc, userTimeZone);
                            
                            if (lastSentUserTime.Date == currentTimeInUserTimeZone.Date)
                                continue;
                        }
                        
                        string userId = emailSettings.UserId;
                        
                        // Generate recommendation using OpenAI
                        string recommendation = await _openAIService.GetRecommendationAsync(userId);
                        
                        // Send the email
                        await SendRecommendationEmailAsync(userId, emailSettings.Email, recommendation);
                        sentAnyEmails = true;
                        
                        _logger.LogInformation($"Sent scheduled email to user {userId} at {emailSettings.Email} (timezone: {timezone})");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Error processing email settings for user {emailSettings.UserId}");
                        // Continue with the next user instead of failing the entire batch
                        continue;
                    }
                }
                
                return sentAnyEmails;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in CheckAndSendScheduledEmailsAsync");
                return false;
            }
        }
    }
}