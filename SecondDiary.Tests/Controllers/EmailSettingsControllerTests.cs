using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using SecondDiary.Controllers;
using SecondDiary.Models;
using SecondDiary.Services;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Xunit;

namespace SecondDiary.Tests.Controllers
{
    public class EmailSettingsControllerTests
    {
        private readonly Mock<ICosmosDbService> _mockCosmosDbService;
        private readonly Mock<IUserContext> _mockUserContext;
        private readonly Mock<ILogger<EmailSettingsController>> _mockLogger;
        private readonly Mock<IEmailService> _mockEmailService;
        private readonly EmailSettingsController _controller;
        private readonly string _testUserId = "test-user";
        private readonly string _testEmail = "test@example.com";

        public EmailSettingsControllerTests()
        {
            _mockCosmosDbService = new Mock<ICosmosDbService>();
            _mockUserContext = new Mock<IUserContext>();
            _mockLogger = new Mock<ILogger<EmailSettingsController>>();
            
            // Set up user context
            _mockUserContext
                .Setup(u => u.UserId)
                .Returns(_testUserId);
            
            // Create mock email service
            _mockEmailService = new Mock<IEmailService>();
            
            _controller = new EmailSettingsController(
                _mockCosmosDbService.Object, 
                _mockUserContext.Object,
                _mockLogger.Object,
                _mockEmailService.Object);
            
            // Setup ClaimsPrincipal with email claim
            var claims = new List<Claim>
            {
                new Claim("email", _testEmail)
            };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            var claimsPrincipal = new ClaimsPrincipal(identity);
            
            // Assign the ClaimsPrincipal to the controller's User property
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = claimsPrincipal
                }
            };
        }

        [Fact]
        public async Task GetEmailSettings_ReturnsOkResult_WhenSettingsExist()
        {
            // Arrange
            var expectedSettings = new EmailSettings
            {
                Id = "test-id",
                UserId = _testUserId,
                Email = _testEmail,
                PreferredTime = new TimeSpan(9, 0, 0), // 9:00 AM
                IsEnabled = true
            };
            
            _mockCosmosDbService.Setup(service => service.GetEmailSettingsAsync(_testUserId))
                .ReturnsAsync(expectedSettings);

            // Act
            var result = await _controller.GetEmailSettings();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnedSettings = Assert.IsType<EmailSettings>(okResult.Value);
            Assert.Equal(expectedSettings.Id, returnedSettings.Id);
            Assert.Equal(expectedSettings.Email, returnedSettings.Email);
            Assert.Equal(expectedSettings.PreferredTime, returnedSettings.PreferredTime);
            Assert.Equal(expectedSettings.IsEnabled, returnedSettings.IsEnabled);
        }

        [Fact]
        public async Task GetEmailSettings_ReturnsNotFound_WhenSettingsDontExist()
        {
            // Arrange
            _mockCosmosDbService.Setup(service => service.GetEmailSettingsAsync(_testUserId))
                .ReturnsAsync((EmailSettings)null!);

            // Act
            var result = await _controller.GetEmailSettings();

            // Assert
            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task CreateOrUpdateEmailSettings_CreatesNewSettings_WhenNoExistingSettings()
        {
            // Arrange
            var inputSettings = new EmailSettings
            {
                PreferredTime = new TimeSpan(10, 0, 0), // 10:00 AM
                IsEnabled = true
            };
            
            _mockCosmosDbService.Setup(service => service.GetEmailSettingsAsync(_testUserId))
                .ReturnsAsync((EmailSettings)null!);
            
            _mockCosmosDbService.Setup(service => service.CreateEmailSettingsAsync(It.IsAny<EmailSettings>()))
                .ReturnsAsync((EmailSettings settings) => settings);

            // Act
            var result = await _controller.CreateOrUpdateEmailSettings(inputSettings);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var createdSettings = Assert.IsType<EmailSettings>(okResult.Value);
            Assert.Equal(_testUserId, createdSettings.UserId);
            Assert.Equal(_testEmail, createdSettings.Email); // Email should come from claims
            Assert.Equal(inputSettings.PreferredTime, createdSettings.PreferredTime);
            Assert.Equal(inputSettings.IsEnabled, createdSettings.IsEnabled);
            
            _mockCosmosDbService.Verify(service => service.CreateEmailSettingsAsync(It.IsAny<EmailSettings>()), Times.Once);
        }

        [Fact]
        public async Task CreateOrUpdateEmailSettings_UpdatesExistingSettings_WhenSettingsExist()
        {
            // Arrange
            var existingSettings = new EmailSettings
            {
                Id = "test-id",
                UserId = _testUserId,
                Email = "old-email@example.com", // This should be updated
                PreferredTime = new TimeSpan(9, 0, 0),
                IsEnabled = false
            };
            
            var inputSettings = new EmailSettings
            {
                PreferredTime = new TimeSpan(10, 0, 0), // 10:00 AM
                IsEnabled = true
            };
            
            _mockCosmosDbService.Setup(service => service.GetEmailSettingsAsync(_testUserId))
                .ReturnsAsync(existingSettings);
            
            _mockCosmosDbService.Setup(service => service.UpdateEmailSettingsAsync(It.IsAny<EmailSettings>()))
                .ReturnsAsync((EmailSettings settings) => settings);

            // Act
            var result = await _controller.CreateOrUpdateEmailSettings(inputSettings);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var updatedSettings = Assert.IsType<EmailSettings>(okResult.Value);
            Assert.Equal(existingSettings.Id, updatedSettings.Id);
            Assert.Equal(_testEmail, updatedSettings.Email); // Email should be updated from claims
            Assert.Equal(inputSettings.PreferredTime, updatedSettings.PreferredTime);
            Assert.Equal(inputSettings.IsEnabled, updatedSettings.IsEnabled);
            
            _mockCosmosDbService.Verify(service => service.UpdateEmailSettingsAsync(It.IsAny<EmailSettings>()), Times.Once);
        }

        [Fact]
        public async Task CreateOrUpdateEmailSettings_ReturnsUnauthorized_WhenUserIdNotFound()
        {
            // Arrange
            _mockUserContext.Setup(u => u.UserId)
                .Returns((string)null!);
            
            var inputSettings = new EmailSettings
            {
                PreferredTime = new TimeSpan(10, 0, 0),
                IsEnabled = true
            };

            // Act
            var result = await _controller.CreateOrUpdateEmailSettings(inputSettings);

            // Assert
            Assert.IsType<UnauthorizedObjectResult>(result);
        }

        [Fact]
        public async Task CreateOrUpdateEmailSettings_ReturnsBadRequest_WhenEmailNotFound()
        {
            // Arrange
            // Clear the claims identity to remove the email claim
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity())
                }
            };
            
            var inputSettings = new EmailSettings
            {
                PreferredTime = new TimeSpan(10, 0, 0),
                IsEnabled = true
            };

            // Act
            var result = await _controller.CreateOrUpdateEmailSettings(inputSettings);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task DeleteEmailSettings_ReturnsOk_WhenSuccessful()
        {
            // Arrange
            _mockCosmosDbService.Setup(service => service.DeleteEmailSettingsAsync(_testUserId))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.DeleteEmailSettings();

            // Assert
            Assert.IsType<OkResult>(result);
            _mockCosmosDbService.Verify(service => service.DeleteEmailSettingsAsync(_testUserId), Times.Once);
        }

        [Fact]
        public async Task DeleteEmailSettings_ReturnsUnauthorized_WhenUserIdNotFound()
        {
            // Arrange
            _mockUserContext.Setup(u => u.UserId)
                .Returns((string)null!);

            // Act
            var result = await _controller.DeleteEmailSettings();

            // Assert
            Assert.IsType<UnauthorizedObjectResult>(result);
            _mockCosmosDbService.Verify(service => service.DeleteEmailSettingsAsync(It.IsAny<string>()), Times.Never);
        }
    }
}
