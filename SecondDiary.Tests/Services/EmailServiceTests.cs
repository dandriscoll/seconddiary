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

            _mockOpenAIService
                .Setup(s => s.GetRecommendationsAsync(_testUserId))
                .ReturnsAsync("Test recommendation");

            var service = CreateEmailService();

            // Act
            var result = await service.CheckAndSendScheduledEmailsAsync();

            // Assert
            Assert.True(result);
            
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
            _mockOpenAIService.Verify(s => s.GetRecommendationsAsync(_testUserId), Times.Never);
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
            _mockOpenAIService.Verify(s => s.GetRecommendationsAsync(_testUserId), Times.Never);
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
            _mockOpenAIService.Verify(s => s.GetRecommendationsAsync(_testUserId), Times.Never);
        }

        private EmailService CreateEmailService()
        {
            // Create EmailService with constructor
            var service = new EmailService(
                _mockOptions.Object,
                _mockLogger.Object,
                _mockCosmosDbService.Object,
                _mockDiaryService.Object,
                _mockOpenAIService.Object);

            // Use reflection to replace the EmailClient with our mock
            var emailClientField = typeof(EmailService).GetField("_emailClient", BindingFlags.NonPublic | BindingFlags.Instance);
            emailClientField?.SetValue(service, _mockEmailClient.Object);

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
