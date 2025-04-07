using Azure;
using Azure.Communication.Email;
using Microsoft.Extensions.Options;
using SecondDiary.Models;
using Markdig;
using System.Text.RegularExpressions;

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
            
            // Convert markdown content to HTML
            string processedHtmlContent = Markdown.ToHtml(messageContent);
            
            // Convert markdown to plain text
            string processedPlainTextContent = ConvertMarkdownToPlainText(messageContent);
            
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
                    <div class='markdown-content'>
                        {processedHtmlContent}
                    </div>
                    <p>{outro}</p>
                    <p>
                        <a href='{_communicationSettings.BaseUrl}' class='button'>Visit Second Diary</a>
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

            {processedPlainTextContent}

            {outro}

            Visit Second Diary: {_communicationSettings.BaseUrl}

            If you'd like to update your email preferences, please visit your account settings.

            &copy; {DateTime.Now.Year} Second Diary. All rights reserved.";

            return (htmlContent, plainTextContent);
        }
        
        /// <summary>
        /// Converts Markdown to plain text, preserving structure but removing formatting
        /// </summary>
        private string ConvertMarkdownToPlainText(string markdown)
        {
            if (string.IsNullOrEmpty(markdown))
                return string.Empty;
                
            // Headers: Convert # Header to HEADER
            string result = Regex.Replace(markdown, @"^#{1,6}\s+(.+)$", match => 
            {
                string headerText = match.Groups[1].Value.Trim();
                return headerText.ToUpper() + "\n";
            }, RegexOptions.Multiline);
            
            // Bold: Convert **bold** or __bold__ to *bold*
            result = Regex.Replace(result, @"\*\*(.+?)\*\*|__(.+?)__", match => 
            {
                string text = match.Groups[1].Value;
                if (string.IsNullOrEmpty(text))
                    text = match.Groups[2].Value;
                return "*" + text + "*";
            });
            
            // Italic: Convert *italic* or _italic_ to _italic_
            result = Regex.Replace(result, @"\*(.+?)\*|_(.+?)_", match => 
            {
                string text = match.Groups[1].Value;
                if (string.IsNullOrEmpty(text))
                    text = match.Groups[2].Value;
                return "_" + text + "_";
            });
            
            // Lists: Convert - item to * item
            result = Regex.Replace(result, @"^[\*\-\+]\s+(.+)$", "* $1", RegexOptions.Multiline);
            
            // Numbered lists: Preserve as is
            result = Regex.Replace(result, @"^\d+\.\s+(.+)$", match => match.Value, RegexOptions.Multiline);
            
            // Links: Convert [text](url) to text (url)
            result = Regex.Replace(result, @"\[(.+?)\]\((.+?)\)", "$1 ($2)");
            
            // Images: Convert ![alt](url) to [IMAGE: alt]
            result = Regex.Replace(result, @"!\[(.+?)\]\((.+?)\)", "[IMAGE: $1]");
            
            // Blockquotes: Convert > quote to | quote
            result = Regex.Replace(result, @"^>\s+(.+)$", "| $1", RegexOptions.Multiline);
            
            // Code blocks: Replace with simple TEXT BLOCK
            result = Regex.Replace(result, @"```[\s\S]*?```", match => 
            {
                string codeContent = match.Value.Replace("```", "").Trim();
                return "CODE BLOCK:\n" + codeContent + "\nEND CODE BLOCK\n";
            });
            
            // Inline code: Replace `code` with "code"
            result = Regex.Replace(result, @"`(.+?)`", "\"$1\"");
            
            // Handle horizontal rules
            result = Regex.Replace(result, @"^[\*\-_]{3,}$", "---------------------", RegexOptions.Multiline);
            
            return result;
        }
        
        public async Task SendRecommendationEmailAsync(string userId, string emailAddress, string recommendation)
        {
            try
            {
                string subject = "Your Daily Second Diary Recommendation";
                string intro = "Based on your recent diary entries, here's a personalized recommendation for you:";
                string outro = "We hope you find this recommendation helpful and insightful.";
                (string htmlContent, string plainTextContent) = CreateEmailContent("Your Daily Recommendation", intro, recommendation, outro);

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
                    EmailSettings? emailSettings = await _cosmosDbService.GetEmailSettingsAsync(userId);
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
                (string htmlContent, string plainTextContent) = CreateEmailContent("Test Email", string.Empty, message, string.Empty);

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

        protected virtual DateTime GetCurrentUtcTime()
        {
            return DateTime.UtcNow;
        }

        public async Task<bool> CheckAndSendScheduledEmailsAsync()
        {
            try
            {
                _logger.LogInformation("Starting scheduled email check");
                bool sentAnyEmails = false;
                
                // Get current UTC time
                DateTime currentUtcDateTime = GetCurrentUtcTime();
                
                // Get all email settings
                IEnumerable<EmailSettings> allEmailSettings = await _cosmosDbService.GetAllEmailSettingsAsync();
                
                // Process each user's email settings
                foreach (EmailSettings emailSettings in allEmailSettings)
                {
                    // Skip if email settings are not enabled
                    if (!emailSettings.IsEnabled)
                        continue;
                    
                    try
                    {
                        // Get timezone from user settings, default to UTC if not specified
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
                        
                        // Create a DateTime object for the user's preferred time today in their timezone
                        DateTime preferredTimeToday = new DateTime(
                            currentTimeInUserTimeZone.Year,
                            currentTimeInUserTimeZone.Month,
                            currentTimeInUserTimeZone.Day,
                            emailSettings.PreferredTime.Hours,
                            emailSettings.PreferredTime.Minutes,
                            0,
                            DateTimeKind.Unspecified);
                        
                        // Convert this local preferred time to UTC for comparison
                        DateTime preferredTimeInUtc = TimeZoneInfo.ConvertTimeToUtc(preferredTimeToday, userTimeZone);
                        
                        // Check if it's time to send the email (within 5 minutes after the preferred time)
                        TimeSpan timeDifference = currentUtcDateTime - preferredTimeInUtc;
                        
                        if (timeDifference.TotalMinutes < 0 || timeDifference.TotalMinutes > 5)
                                                    continue;
                                                
                        // Check if we've already sent an email today (using user's local date)
                        if (emailSettings.LastEmailSent.HasValue)
                        {
                            DateTime lastSentUtc = emailSettings.LastEmailSent.Value;
                            DateTime lastSentUserTime = TimeZoneInfo.ConvertTimeFromUtc(lastSentUtc, userTimeZone);
                            
                            if (lastSentUserTime.Date == currentTimeInUserTimeZone.Date)
                                                            continue;
                            }
                                                
                        string userId = emailSettings.UserId;
                        
                        // Generate recommendation using OpenAI
                        string recommendation = await _openAIService.GetRecommendationAsync(userId);
                        
                        // Send the email
                        await SendRecommendationEmailAsync(userId, emailSettings.Email, recommendation);
                        sentAnyEmails = true;
                        
                        _logger.LogInformation($"Sent scheduled email to user {userId} at {emailSettings.Email} " +
                                             $"(timezone: {timezone}, local time: {currentTimeInUserTimeZone}, " +
                                             $"preferred time: {preferredTimeToday})");
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