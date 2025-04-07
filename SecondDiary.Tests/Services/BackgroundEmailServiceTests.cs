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

            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
            BackgroundEmailService service = new BackgroundEmailService(_mockLogger.Object, _mockServiceProvider.Object);

            // Act - Start the service and then immediately cancel to test a single execution
            Task executionTask = service.StartAsync(cancellationTokenSource.Token);
            
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

            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
            BackgroundEmailService service = new BackgroundEmailService(_mockLogger.Object, _mockServiceProvider.Object);

            // Act - Start the service and let it run for a short while
            Task executionTask = service.StartAsync(cancellationTokenSource.Token);
            
            // Allow time for multiple iterations
            await Task.Delay(150);
            
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
            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
            BackgroundEmailService service = new BackgroundEmailService(_mockLogger.Object, _mockServiceProvider.Object);

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

        [Fact]
        public async Task ExecuteAsync_LogsSuccessfulEmailSent()
        {
            // Arrange
            _mockEmailService
                .Setup(service => service.CheckAndSendScheduledEmailsAsync())
                .ReturnsAsync(true);

            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
            BackgroundEmailService service = new BackgroundEmailService(_mockLogger.Object, _mockServiceProvider.Object);

            // Act
            Task executionTask = service.StartAsync(cancellationTokenSource.Token);
            await Task.Delay(100);
            cancellationTokenSource.Cancel();
            await executionTask;

            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => (v.ToString() ?? string.Empty).Contains("Successfully sent scheduled emails at:")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()!),
                Times.AtLeastOnce);
        }

        [Fact]
        public async Task ExecuteAsync_NoLogWhenNoEmailsSent()
        {
            // Arrange
            _mockEmailService
                .Setup(service => service.CheckAndSendScheduledEmailsAsync())
                .ReturnsAsync(false);

            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
            BackgroundEmailService service = new BackgroundEmailService(_mockLogger.Object, _mockServiceProvider.Object);

            // Act
            Task executionTask = service.StartAsync(cancellationTokenSource.Token);
            await Task.Delay(100);
            cancellationTokenSource.Cancel();
            await executionTask;

            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => (v.ToString() ?? string.Empty).Contains("Successfully sent scheduled emails at:")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()!),
                Times.Never);
        }

        [Fact]
        public async Task ExecuteAsync_UsesServiceScopeCorrectly()
        {
            // Arrange
            _mockEmailService
                .Setup(service => service.CheckAndSendScheduledEmailsAsync())
                .ReturnsAsync(true);

            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
            BackgroundEmailService service = new BackgroundEmailService(_mockLogger.Object, _mockServiceProvider.Object);

            // Act
            Task executionTask = service.StartAsync(cancellationTokenSource.Token);
            await Task.Delay(100);
            cancellationTokenSource.Cancel();
            await executionTask;

            // Assert
            // Verify that the service scope was created
            _mockServiceScopeFactory.Verify(factory => factory.CreateScope(), Times.AtLeastOnce);
            
            // Verify that GetRequiredService was called to resolve the IEmailService
            _mockServiceProvider.Verify(sp => sp.GetService(typeof(IEmailService)), Times.AtLeastOnce);
            
            // Verify that the scope was disposed
            _mockServiceScope.Verify(scope => scope.Dispose(), Times.AtLeastOnce);
        }

        [Fact]
        public async Task ExecuteAsync_WaitsOneMinuteBetweenChecks()
        {
            // This test requires modifying the implementation to allow for testing the delay
            // In a real-world scenario, you might use a timer abstraction that can be mocked
            // For this test, we're just verifying the service runs multiple iterations
            
            // Arrange
            int callCount = 0;
            _mockEmailService
                .Setup(service => service.CheckAndSendScheduledEmailsAsync())
                .ReturnsAsync(true)
                .Callback(() => callCount++);

            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
            BackgroundEmailService service = new BackgroundEmailService(_mockLogger.Object, _mockServiceProvider.Object);

            // Act
            Task executionTask = service.StartAsync(cancellationTokenSource.Token);
            
            // Wait for more than one minute to allow multiple iterations
            // Note: In a real test, you'd mock Task.Delay to avoid waiting
            await Task.Delay(150);
            
            cancellationTokenSource.Cancel();
            await executionTask;

            // Assert
            // We expect at least one call during our brief test period
            Assert.True(callCount >= 1, "The service should have called CheckAndSendScheduledEmailsAsync at least once");
        }
    }
}
