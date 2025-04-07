using Azure;
using Azure.Communication.Email;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using SecondDiary.Models;
using SecondDiary.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace SecondDiary.Tests.Services
{
    public class EmailServiceTests
    {
        private readonly Mock<IOptions<CommunicationServiceSettings>> _mockOptions;
        private readonly Mock<ILogger<EmailService>> _mockLogger;
        private readonly Mock<ICosmosDbService> _mockCosmosDbService;
        private readonly Mock<IDiaryService> _mockDiaryService;
        private readonly Mock<IOpenAIService> _mockOpenAIService;
        private readonly Mock<EmailClient> _mockEmailClient;
        private readonly string _testUserId = "test-user";
        private readonly string _testEmail = "test@example.com";

        public EmailServiceTests()
        {
            _mockOptions = new Mock<IOptions<CommunicationServiceSettings>>();
            _mockLogger = new Mock<ILogger<EmailService>>();
            _mockCosmosDbService = new Mock<ICosmosDbService>();
            _mockDiaryService = new Mock<IDiaryService>();
            _mockOpenAIService = new Mock<IOpenAIService>();
            _mockEmailClient = new Mock<EmailClient>();

            // Setup options
            _mockOptions.Setup(o => o.Value).Returns(new CommunicationServiceSettings
            {
                ConnectionString = "endpoint=https://test.communication.azure.com/;accesskey=test",
                SenderEmail = "noreply@test.com",
                SenderName = "Test Sender"
            });
        }

        [Fact]
        public async Task SendRecommendationEmailAsync_SendsEmail_AndUpdatesLastSentTimestamp()
        {
            // Arrange
            var emailSettings = new EmailSettings
            {
                Id = "test-id",
                UserId = _testUserId,
                Email = _testEmail,
                PreferredTime = new TimeSpan(9, 0, 0),
                IsEnabled = true
            };

            _mockCosmosDbService
                .Setup(s => s.GetEmailSettingsAsync(_testUserId))
                .ReturnsAsync(emailSettings);

            _mockCosmosDbService
                .Setup(s => s.UpdateEmailSettingsAsync(It.IsAny<EmailSettings>()))
                .ReturnsAsync((EmailSettings settings) => settings);

            // Setup mock email operation
            var mockOperation = new Mock<EmailSendOperation>();
            mockOperation.Setup(o => o.Id).Returns("test-operation-id");

            // Create the service with our mocks injected via reflection
            var service = CreateEmailService();

            string recommendation = "Test recommendation";

            // Act
            await service.SendRecommendationEmailAsync(_testUserId, _testEmail, recommendation);

            // Assert
            // Verify that the last sent timestamp was updated
            _mockCosmosDbService.Verify(
                s => s.UpdateEmailSettingsAsync(It.Is<EmailSettings>(
                    es => es.UserId == _testUserId && es.LastEmailSent.HasValue)),
                Times.Once);

            // Verify logging occurred
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => (v.ToString() ?? string.Empty).Contains("Email sent successfully")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()!),
                Times.Once);
        }

        [Fact]
        public async Task CheckAndSendScheduledEmailsAsync_SendsEmails_WhenTimeMatches()
        {
            // Arrange
            var currentTime = DateTime.UtcNow;
            var preferredTime = currentTime.TimeOfDay;
            
            var emailSettings = new EmailSettings
            {
                Id = "test-id",
                UserId = _testUserId,
                Email = _testEmail,
                PreferredTime = preferredTime,
                IsEnabled = true,
                LastEmailSent = currentTime.Date.AddDays(-1) // Last sent yesterday
            };

            var diaryEntries = new List<DiaryEntry>
            {
                new DiaryEntry { Id = "entry1", UserId = _testUserId, Date = currentTime.AddDays(-1) },
                new DiaryEntry { Id = "entry2", UserId = _testUserId, Date = currentTime.AddDays(-2) }
            };

            // Set up mock services
            _mockDiaryService
                .Setup(s => s.GetEntriesAsync(_testUserId))
                .ReturnsAsync(diaryEntries);

            _mockCosmosDbService
                .Setup(s => s.GetEmailSettingsAsync(_testUserId))
                .ReturnsAsync(emailSettings);
            
            _mockCosmosDbService
                .Setup(s => s.GetAllEmailSettingsAsync())
                .ReturnsAsync(new List<EmailSettings> { emailSettings });

            _mockOpenAIService
                .Setup(s => s.GetRecommendationAsync(_testUserId))
                .ReturnsAsync("Test recommendation");

            var service = CreateEmailService();

            // Act
            var result = await service.CheckAndSendScheduledEmailsAsync();

            // Assert
            Assert.True(result);
            
            // Verify the email was sent
            _mockLogger.Verify(
                x => x.Log(
                    It.IsAny<LogLevel>(),
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Sent scheduled email")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()!),
                Times.Once);
        }

        [Fact]
        public async Task CheckAndSendScheduledEmailsAsync_DoesNotSendEmail_WhenTimeDoesNotMatch()
        {
            // Arrange
            var currentTime = DateTime.UtcNow;
            var preferredTime = currentTime.TimeOfDay.Add(new TimeSpan(1, 0, 0)); // 1 hour later
            
            var emailSettings = new EmailSettings
            {
                Id = "test-id",
                UserId = _testUserId,
                Email = _testEmail,
                PreferredTime = preferredTime,
                IsEnabled = true,
                LastEmailSent = currentTime.Date.AddDays(-1) // Last sent yesterday
            };

            var diaryEntries = new List<DiaryEntry>
            {
                new DiaryEntry { Id = "entry1", UserId = _testUserId, Date = currentTime.AddDays(-1) }
            };

            // Set up mock services
            _mockDiaryService
                .Setup(s => s.GetEntriesAsync(_testEmail))
                .ReturnsAsync(diaryEntries);

            _mockCosmosDbService
                .Setup(s => s.GetEmailSettingsAsync(_testUserId))
                .ReturnsAsync(emailSettings);

            var service = CreateEmailService();

            // Act
            var result = await service.CheckAndSendScheduledEmailsAsync();

            // Assert
            Assert.False(result);
            
            // Verify the email was NOT sent
            _mockDiaryService.Verify(s => s.GetEntriesAsync(_testUserId), Times.Never);
            _mockOpenAIService.Verify(s => s.GetRecommendationAsync(_testUserId), Times.Never);
        }

        [Fact]
        public async Task CheckAndSendScheduledEmailsAsync_DoesNotSendEmail_WhenAlreadySentToday()
        {
            // Arrange
            var currentTime = DateTime.UtcNow;
            var preferredTime = currentTime.TimeOfDay;
            
            var emailSettings = new EmailSettings
            {
                Id = "test-id",
                UserId = _testUserId,
                Email = _testEmail,
                PreferredTime = preferredTime,
                IsEnabled = true,
                LastEmailSent = currentTime.Date // Already sent today
            };

            var diaryEntries = new List<DiaryEntry>
            {
                new DiaryEntry { Id = "entry1", UserId = _testUserId, Date = currentTime.AddDays(-1) }
            };

            // Set up mock services
            _mockDiaryService
                .Setup(s => s.GetEntriesAsync(_testUserId))
                .ReturnsAsync(diaryEntries);

            _mockCosmosDbService
                .Setup(s => s.GetEmailSettingsAsync(_testUserId))
                .ReturnsAsync(emailSettings);

            var service = CreateEmailService();

            // Act
            var result = await service.CheckAndSendScheduledEmailsAsync();

            // Assert
            Assert.False(result);
            
            // Verify the email was NOT sent
            _mockDiaryService.Verify(s => s.GetEntriesAsync(_testUserId), Times.Never);
            _mockOpenAIService.Verify(s => s.GetRecommendationAsync(_testUserId), Times.Never);
        }

        [Fact]
        public async Task CheckAndSendScheduledEmailsAsync_DoesNotSendEmail_WhenSettingsDisabled()
        {
            // Arrange
            var currentTime = DateTime.UtcNow;
            var preferredTime = currentTime.TimeOfDay;
            
            var emailSettings = new EmailSettings
            {
                Id = "test-id",
                UserId = _testUserId,
                Email = _testEmail,
                PreferredTime = preferredTime,
                IsEnabled = false, // Disabled settings
                LastEmailSent = currentTime.Date.AddDays(-1)
            };

            var diaryEntries = new List<DiaryEntry>
            {
                new DiaryEntry { Id = "entry1", UserId = _testUserId, Date = currentTime.AddDays(-1) }
            };

            // Set up mock services
            _mockDiaryService
                .Setup(s => s.GetEntriesAsync(_testUserId))
                .ReturnsAsync(diaryEntries);

            _mockCosmosDbService
                .Setup(s => s.GetEmailSettingsAsync(_testUserId))
                .ReturnsAsync(emailSettings);

            var service = CreateEmailService();

            // Act
            var result = await service.CheckAndSendScheduledEmailsAsync();

            // Assert
            Assert.False(result);
            
            // Verify the email was NOT sent
            _mockDiaryService.Verify(s => s.GetEntriesAsync(_testUserId), Times.Never);
            _mockOpenAIService.Verify(s => s.GetRecommendationAsync(_testUserId), Times.Never);
        }

        [Fact]
        public async Task CheckAndSendScheduledEmailsAsync_SendsEmail_AtCorrectTimeInUserTimezone()
        {
            // Arrange
            // Set up a fixed current UTC time - April 6, 2025 14:02 UTC
            DateTime currentUtcDateTime = new DateTime(2025, 4, 6, 14, 2, 0, DateTimeKind.Utc);

            // User's preferred time is 10:00 AM in Eastern Time (UTC-4)
            string userTimezone = "Eastern Standard Time"; // Windows timezone ID
            TimeSpan preferredLocalTime = new TimeSpan(10, 0, 0); // 10:00 AM
            
            // The equivalent UTC time would be 14:00 UTC (10:00 + 4 hours offset)
            // Our current time is 14:02 UTC, so it's 2 minutes past the preferred time
            // This should be within the 5-minute window for sending emails

            EmailSettings emailSettings = new EmailSettings
            {
                Id = "test-timezone-id",
                UserId = _testUserId,
                Email = _testEmail,
                PreferredTime = preferredLocalTime,
                TimeZone = userTimezone,
                IsEnabled = true,
                LastEmailSent = currentUtcDateTime.Date.AddDays(-1) // Last sent yesterday
            };

            // Set up mock services
            _mockCosmosDbService
                .Setup(s => s.GetAllEmailSettingsAsync())
                .ReturnsAsync(new List<EmailSettings> { emailSettings });

            _mockCosmosDbService
                .Setup(s => s.GetEmailSettingsAsync(_testUserId))
                .ReturnsAsync(emailSettings);

            _mockCosmosDbService
                .Setup(s => s.UpdateEmailSettingsAsync(It.IsAny<EmailSettings>()))
                .ReturnsAsync((EmailSettings settings) => settings);

            _mockOpenAIService
                .Setup(s => s.GetRecommendationAsync(_testUserId))
                .ReturnsAsync("Test recommendation");

            // Create the service with our mocks
            EmailService service = CreateEmailService();

            // Use the EmailServiceForTesting class instead of reflection
            EmailServiceForTesting customEmailService = new EmailServiceForTesting(
                _mockOptions.Object,
                _mockLogger.Object,
                _mockCosmosDbService.Object,
                _mockOpenAIService.Object,
                currentUtcDateTime);

            // Replace the EmailClient with our mock
            FieldInfo? emailClientField = typeof(EmailService).GetField("_emailClient", BindingFlags.NonPublic | BindingFlags.Instance);
            if (emailClientField != null)
                emailClientField.SetValue(customEmailService, _mockEmailClient.Object);

            // Setup EmailClient mock to return a mock operation
            Mock<EmailSendOperation> mockOperation = new Mock<EmailSendOperation>();
            mockOperation.Setup(o => o.Id).Returns("test-operation-id");

            // Setup EmailClient.SendAsync to return the mock operation
            _mockEmailClient
                .Setup(client => client.SendAsync(
                    It.IsAny<WaitUntil>(),
                    It.IsAny<EmailMessage>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockOperation.Object);

            // Act
            bool result = await customEmailService.CheckAndSendScheduledEmailsAsync();

            // Assert
            Assert.True(result, "Email should be sent when current UTC time is within 5 minutes of preferred time in user's timezone");
            
            _mockOpenAIService.Verify(
                s => s.GetRecommendationAsync(_testUserId),
                Times.Once,
                "GetRecommendationAsync should be called once when email is scheduled for sending");

            // Verify the email was sent
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => (v.ToString() ?? string.Empty).Contains("Sent scheduled email")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()!),
                Times.Once);
        }

        [Fact]
        public async Task CheckAndSendScheduledEmailsAsync_DoesNotSendEmail_WhenOutsideTimezoneWindow()
        {
            // Arrange
            // Set up a fixed current UTC time - April 6, 2025 16:00 UTC (too late)
            DateTime currentUtcDateTime = new DateTime(2025, 4, 6, 16, 0, 0, DateTimeKind.Utc);

            // User's preferred time is 10:00 AM in Eastern Time (UTC-4)
            string userTimezone = "Eastern Standard Time"; // Windows timezone ID  
            TimeSpan preferredLocalTime = new TimeSpan(10, 0, 0); // 10:00 AM
            
            // The equivalent UTC time would be 14:00 UTC (10:00 + 4 hours offset)
            // Our current time is 16:00 UTC, so it's 2 hours past the preferred time
            // This should be outside the 5-minute window for sending emails

            EmailSettings emailSettings = new EmailSettings
            {
                Id = "test-timezone-id",
                UserId = _testUserId,
                Email = _testEmail,
                PreferredTime = preferredLocalTime,
                TimeZone = userTimezone,
                IsEnabled = true,
                LastEmailSent = currentUtcDateTime.Date.AddDays(-1) // Last sent yesterday
            };

            // Set up mock services
            _mockCosmosDbService
                .Setup(s => s.GetAllEmailSettingsAsync())
                .ReturnsAsync(new List<EmailSettings> { emailSettings });

            // Create the service with our mocks
            EmailService service = CreateEmailService();

            // Use reflection to create a method that will allow us to pass our fixed current time
            MethodInfo? methodInfo = typeof(EmailService).GetMethod(
                "CheckAndSendScheduledEmailsAsync", 
                BindingFlags.Public | BindingFlags.Instance);
                
            Assert.NotNull(methodInfo); // Ensure method exists

            // Use our EmailServiceForTesting class instead of reflection
            EmailServiceForTesting customEmailService = new EmailServiceForTesting(
                _mockOptions.Object,
                _mockLogger.Object,
                _mockCosmosDbService.Object,
                _mockOpenAIService.Object,
                currentUtcDateTime);

            // Replace the EmailClient with our mock
            FieldInfo? emailClientField = typeof(EmailService).GetField("_emailClient", BindingFlags.NonPublic | BindingFlags.Instance);
            if (emailClientField != null)
                emailClientField.SetValue(customEmailService, _mockEmailClient.Object);

            // Act
            bool result = await customEmailService.CheckAndSendScheduledEmailsAsync();

            // Assert
            Assert.False(result, "Email should not be sent when current UTC time is outside 5 minutes of preferred time in user's timezone");
            
            _mockOpenAIService.Verify(
                s => s.GetRecommendationAsync(_testUserId),
                Times.Never,
                "GetRecommendationAsync should not be called when email is not scheduled for sending");

            // Verify the email was not sent
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => (v.ToString() ?? string.Empty).Contains("Sent scheduled email")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()!),
                Times.Never);
        }

        [Fact]
        public async Task CheckAndSendScheduledEmailsAsync_SendsEmailUsingUserTimezone()
        {
            // Arrange
            // Use a fixed UTC time for testing - April 6, 2025 14:02 UTC
            DateTime testUtcTime = new DateTime(2025, 4, 6, 14, 2, 0, DateTimeKind.Utc);
            
            // User in Eastern Time (UTC-4) with preferred email time at 10:00 AM
            // At 14:02 UTC, it's 10:02 AM in Eastern Time, so the email should be sent
            TimeSpan preferredTime = new TimeSpan(10, 0, 0); // 10:00 AM
            string timeZone = "Eastern Standard Time"; // Windows timezone ID

            EmailSettings emailSettings = new EmailSettings
            {
                Id = "timezone-test-id",
                UserId = _testUserId,
                Email = _testEmail,
                PreferredTime = preferredTime,
                TimeZone = timeZone,
                IsEnabled = true,
                LastEmailSent = testUtcTime.AddDays(-1) // Last sent yesterday
            };

            // Set up mock services
            _mockCosmosDbService
                .Setup(s => s.GetAllEmailSettingsAsync())
                .ReturnsAsync(new List<EmailSettings> { emailSettings });

            _mockCosmosDbService
                .Setup(s => s.GetEmailSettingsAsync(_testUserId))
                .ReturnsAsync(emailSettings);

            _mockCosmosDbService
                .Setup(s => s.UpdateEmailSettingsAsync(It.IsAny<EmailSettings>()))
                .ReturnsAsync((EmailSettings settings) => settings);

            _mockOpenAIService
                .Setup(s => s.GetRecommendationAsync(_testUserId))
                .ReturnsAsync("Test recommendation for timezone test");

            EmailServiceForTesting customEmailService = new EmailServiceForTesting(
                _mockOptions.Object,
                _mockLogger.Object,
                _mockCosmosDbService.Object,
                _mockOpenAIService.Object,
                testUtcTime);

            // Replace the EmailClient with our mock
            FieldInfo? emailClientField = typeof(EmailService).GetField("_emailClient", BindingFlags.NonPublic | BindingFlags.Instance);
            if (emailClientField != null)
                emailClientField.SetValue(customEmailService, _mockEmailClient.Object);

            // Setup EmailClient mock to return a mock operation
            Mock<EmailSendOperation> mockOperation = new Mock<EmailSendOperation>();
            mockOperation.Setup(o => o.Id).Returns("test-operation-id");

            // Setup EmailClient.SendAsync to return the mock operation
            _mockEmailClient
                .Setup(client => client.SendAsync(
                    It.IsAny<WaitUntil>(),
                    It.IsAny<EmailMessage>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockOperation.Object);

            // Act
            bool result = await customEmailService.CheckAndSendScheduledEmailsAsync();

            // Assert
            Assert.True(result, "Email should be sent when current UTC time corresponds to the user's preferred time in their timezone");

            // Verify recommendation was generated
            _mockOpenAIService.Verify(
                s => s.GetRecommendationAsync(_testUserId),
                Times.Once,
                "GetRecommendationAsync should be called once");

            // Verify email was sent
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => (v.ToString() ?? string.Empty).Contains("Sent scheduled email")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()!),
                Times.Once);
        }

        [Fact]
        public async Task CheckAndSendScheduledEmailsAsync_DoesNotSendEmail_WhenOutsideUserTimezoneWindow()
        {
            // Arrange
            // Use a fixed UTC time for testing - April 6, 2025 15:30 UTC
            DateTime testUtcTime = new DateTime(2025, 4, 6, 15, 30, 0, DateTimeKind.Utc);
            
            // User in Eastern Time (UTC-4) with preferred email time at 10:00 AM
            // At 15:30 UTC, it's 11:30 AM in Eastern Time, which is more than 5 minutes past 10:00 AM
            // So the email should NOT be sent
            TimeSpan preferredTime = new TimeSpan(10, 0, 0); // 10:00 AM
            string timeZone = "Eastern Standard Time"; // Windows timezone ID

            EmailSettings emailSettings = new EmailSettings
            {
                Id = "timezone-test-id",
                UserId = _testUserId,
                Email = _testEmail,
                PreferredTime = preferredTime,
                TimeZone = timeZone,
                IsEnabled = true,
                LastEmailSent = testUtcTime.AddDays(-1) // Last sent yesterday
            };

            // Set up mock services
            _mockCosmosDbService
                .Setup(s => s.GetAllEmailSettingsAsync())
                .ReturnsAsync(new List<EmailSettings> { emailSettings });

            // Create a custom EmailService subclass that overrides DateTime.UtcNow
            EmailServiceForTesting customEmailService = new EmailServiceForTesting(
                _mockOptions.Object,
                _mockLogger.Object,
                _mockCosmosDbService.Object,
                _mockOpenAIService.Object,
                testUtcTime);

            // Replace the EmailClient with our mock
            FieldInfo? emailClientField = typeof(EmailService).GetField("_emailClient", BindingFlags.NonPublic | BindingFlags.Instance);
            if (emailClientField != null)
                emailClientField.SetValue(customEmailService, _mockEmailClient.Object);

            // Act
            bool result = await customEmailService.CheckAndSendScheduledEmailsAsync();

            // Assert
            Assert.False(result, "Email should not be sent when current UTC time is outside window for user's preferred time in their timezone");

            // Verify recommendation was not generated
            _mockOpenAIService.Verify(
                s => s.GetRecommendationAsync(_testUserId),
                Times.Never,
                "GetRecommendationAsync should not be called");

            // Verify no email was sent
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => (v.ToString() ?? string.Empty).Contains("Sent scheduled email")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()!),
                Times.Never);
        }
        private class EmailServiceForTesting : EmailService
        {
            private readonly DateTime _fixedUtcNow;

            public EmailServiceForTesting(
                IOptions<CommunicationServiceSettings> communicationSettings,
                ILogger<EmailService> logger,
                ICosmosDbService cosmosDbService,
                IOpenAIService openAIService,
                DateTime fixedUtcNow)
                : base(communicationSettings, logger, cosmosDbService, openAIService)
            {
                _fixedUtcNow = fixedUtcNow;
            }

            protected override DateTime GetCurrentUtcTime()
            {
                return _fixedUtcNow;
            }
        }
        
        private EmailService CreateEmailService()
        {
            // Create EmailService with constructor
            var service = new EmailService(
                _mockOptions.Object,
                _mockLogger.Object,
                _mockCosmosDbService.Object,
                _mockOpenAIService.Object);

            // Use reflection to replace the EmailClient with our mock
            FieldInfo? emailClientField = typeof(EmailService).GetField("_emailClient", BindingFlags.NonPublic | BindingFlags.Instance);
            if (emailClientField != null)
                emailClientField.SetValue(service, _mockEmailClient.Object);

            // Setup EmailClient mock to return a mock operation
            var mockOperation = new Mock<EmailSendOperation>();
            mockOperation.Setup(o => o.Id).Returns("test-operation-id");

            // Setup EmailClient.SendAsync to return the mock operation
            _mockEmailClient
                .Setup(client => client.SendAsync(
                    It.IsAny<WaitUntil>(),
                    It.IsAny<EmailMessage>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockOperation.Object);

            return service;
        }
    }
}
