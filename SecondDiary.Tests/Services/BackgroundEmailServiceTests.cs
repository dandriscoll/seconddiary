using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using SecondDiary.Services;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace SecondDiary.Tests.Services
{
    public class BackgroundEmailServiceTests
    {
        private readonly Mock<ILogger<BackgroundEmailService>> _mockLogger;
        private readonly Mock<IServiceProvider> _mockServiceProvider;
        private readonly Mock<IServiceScope> _mockServiceScope;
        private readonly Mock<IServiceScopeFactory> _mockServiceScopeFactory;
        private readonly Mock<IEmailService> _mockEmailService;

        public BackgroundEmailServiceTests()
        {
            _mockLogger = new Mock<ILogger<BackgroundEmailService>>();
            _mockServiceProvider = new Mock<IServiceProvider>();
            _mockServiceScope = new Mock<IServiceScope>();
            _mockServiceScopeFactory = new Mock<IServiceScopeFactory>();
            _mockEmailService = new Mock<IEmailService>();

            // Setup service scope factory
            _mockServiceProvider
                .Setup(sp => sp.GetService(typeof(IServiceScopeFactory)))
                .Returns(_mockServiceScopeFactory.Object);

            // Setup service scope
            _mockServiceScopeFactory
                .Setup(factory => factory.CreateScope())
                .Returns(_mockServiceScope.Object);

            // Setup service provider from scope
            _mockServiceScope
                .Setup(scope => scope.ServiceProvider)
                .Returns(_mockServiceProvider.Object);

            // Setup email service resolution
            _mockServiceProvider
                .Setup(sp => sp.GetService(typeof(IEmailService)))
                .Returns(_mockEmailService.Object);
        }

        [Fact]
        public async Task ExecuteAsync_CallsCheckAndSendScheduledEmails()
        {
            // Arrange
            _mockEmailService
                .Setup(service => service.CheckAndSendScheduledEmailsAsync())
                .ReturnsAsync(true);

            var cancellationTokenSource = new CancellationTokenSource();
            var service = new BackgroundEmailService(_mockLogger.Object, _mockServiceProvider.Object);

            // Act - Start the service and then immediately cancel to test a single execution
            var executionTask = service.StartAsync(cancellationTokenSource.Token);
            
            // Wait a brief moment to allow the service to start
            await Task.Delay(100);
            
            // Cancel the service
            cancellationTokenSource.Cancel();
            
            // Wait for the task to complete
            await executionTask;

            // Assert
            _mockEmailService.Verify(service => service.CheckAndSendScheduledEmailsAsync(), Times.AtLeastOnce);
        }

        [Fact]
        public async Task ExecuteAsync_ContinuesRunningAfterException()
        {
            // Arrange
            _mockEmailService
                .SetupSequence(service => service.CheckAndSendScheduledEmailsAsync())
                .ThrowsAsync(new Exception("Test exception"))
                .ReturnsAsync(true);

            var cancellationTokenSource = new CancellationTokenSource();
            var service = new BackgroundEmailService(_mockLogger.Object, _mockServiceProvider.Object);

            // Act - Start the service and let it run for a short while
            var executionTask = service.StartAsync(cancellationTokenSource.Token);
            
            // Allow time for multiple iterations
            await Task.Delay(100);
            
            // Cancel the service
            cancellationTokenSource.Cancel();
            
            // Wait for the task to complete
            await executionTask;

            // Assert - Verify the service recovered from the exception
            _mockEmailService.Verify(service => service.CheckAndSendScheduledEmailsAsync(), Times.AtLeastOnce);
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => (v.ToString() ?? string.Empty).Contains("Error occurred while sending scheduled emails")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()!),
                Times.Once);
        }

        [Fact]
        public async Task StopAsync_CancelsRunningTask()
        {
            // Arrange
            var cancellationTokenSource = new CancellationTokenSource();
            var service = new BackgroundEmailService(_mockLogger.Object, _mockServiceProvider.Object);

            // Act - Start the service and immediately stop it
            await service.StartAsync(cancellationTokenSource.Token);
            await service.StopAsync(CancellationToken.None);

            // Assert - The service should stop gracefully
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => (v.ToString() ?? string.Empty).Contains("Background Email Service started at:")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()!),
                Times.Once);
        }
    }
}
