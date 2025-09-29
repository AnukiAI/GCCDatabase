using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using FluentAssertions;
using GCCWebApp.Services;
using System.Net.Mail;

namespace GCCWebApp.Tests.Services
{
    public class EmailServiceTests
    {
        private readonly Mock<IConfiguration> _configurationMock;
        private readonly Mock<ILogger<EmailService>> _loggerMock;
        private readonly Mock<ILogger<MockEmailService>> _mockLoggerMock;
        private readonly EmailService _emailService;
        private readonly MockEmailService _mockEmailService;

        public EmailServiceTests()
        {
            _configurationMock = new Mock<IConfiguration>();
            _loggerMock = new Mock<ILogger<EmailService>>();
            _mockLoggerMock = new Mock<ILogger<MockEmailService>>();
            
            SetupValidConfiguration();
            
            _emailService = new EmailService(_configurationMock.Object, _loggerMock.Object);
            _mockEmailService = new MockEmailService(_mockLoggerMock.Object);
        }

        private void SetupValidConfiguration()
        {
            var smtpSection = new Mock<IConfigurationSection>();
            smtpSection.Setup(x => x["Host"]).Returns("smtp.example.com");
            smtpSection.Setup(x => x["Port"]).Returns("587");
            smtpSection.Setup(x => x["EnableSsl"]).Returns("true");
            smtpSection.Setup(x => x["Username"]).Returns("testuser@example.com");
            smtpSection.Setup(x => x["Password"]).Returns("testpassword");
            smtpSection.Setup(x => x["FromEmail"]).Returns("noreply@example.com");
            smtpSection.Setup(x => x["FromName"]).Returns("GCC Database Test");
            
            _configurationMock.Setup(x => x.GetSection("SmtpSettings")).Returns(smtpSection.Object);
        }

        private void SetupInvalidConfiguration()
        {
            var smtpSection = new Mock<IConfigurationSection>();
            smtpSection.Setup(x => x["Host"]).Returns("");
            smtpSection.Setup(x => x["Port"]).Returns("");
            
            _configurationMock.Setup(x => x.GetSection("SmtpSettings")).Returns(smtpSection.Object);
        }

        #region SendEmailAsync Tests

        [Fact]
        public async Task SendEmailAsync_WithInvalidConfiguration_ShouldThrowInvalidOperationException()
        {
            // Arrange
            SetupInvalidConfiguration();
            var service = new EmailService(_configurationMock.Object, _loggerMock.Object);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => 
                service.SendEmailAsync("test@example.com", "Test Subject", "Test Body"));
        }

        [Fact]
        public async Task SendEmailAsync_ValidatesEmailConfiguration()
        {
            // Arrange
            var smtpSection = new Mock<IConfigurationSection>();
            smtpSection.Setup(x => x["Host"]).Returns("smtp.example.com");
            smtpSection.Setup(x => x["Port"]).Returns("invalid-port"); // Invalid port
            smtpSection.Setup(x => x["EnableSsl"]).Returns("true");
            smtpSection.Setup(x => x["Username"]).Returns("user@example.com");
            smtpSection.Setup(x => x["Password"]).Returns("password");
            smtpSection.Setup(x => x["FromEmail"]).Returns("from@example.com");
            
            _configurationMock.Setup(x => x.GetSection("SmtpSettings")).Returns(smtpSection.Object);
            var service = new EmailService(_configurationMock.Object, _loggerMock.Object);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => 
                service.SendEmailAsync("test@example.com", "Subject", "Body"));
        }

        [Fact]
        public async Task SendEmailAsync_WithInvalidFromEmail_ShouldThrowException()
        {
            // Arrange
            var smtpSection = new Mock<IConfigurationSection>();
            smtpSection.Setup(x => x["Host"]).Returns("smtp.example.com");
            smtpSection.Setup(x => x["Port"]).Returns("587");
            smtpSection.Setup(x => x["EnableSsl"]).Returns("true");
            smtpSection.Setup(x => x["Username"]).Returns("user@example.com");
            smtpSection.Setup(x => x["Password"]).Returns("password");
            smtpSection.Setup(x => x["FromEmail"]).Returns("invalid-email-format"); // Invalid email
            
            _configurationMock.Setup(x => x.GetSection("SmtpSettings")).Returns(smtpSection.Object);
            var service = new EmailService(_configurationMock.Object, _loggerMock.Object);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => 
                service.SendEmailAsync("test@example.com", "Subject", "Body"));
        }

        #endregion

        #region TestConnectionAsync Tests

        [Fact]
        public async Task TestConnectionAsync_WithInvalidConfiguration_ShouldReturnFalse()
        {
            // Arrange
            SetupInvalidConfiguration();
            var service = new EmailService(_configurationMock.Object, _loggerMock.Object);

            // Act
            var result = await service.TestConnectionAsync();

            // Assert
            result.Should().BeFalse();
            _loggerMock.Verify(x => x.LogError(It.IsAny<string>()), Times.AtLeastOnce);
        }

        [Fact]
        public async Task TestConnectionAsync_WithValidConfiguration_HandlesSmtpException()
        {
            // Note: In a real test, you might want to mock SmtpClient, but that's complex
            // This test verifies the logic flow
            
            // Act
            var result = await _emailService.TestConnectionAsync();

            // Assert
            // Will fail to connect to fake SMTP server
            result.Should().BeFalse();
        }

        #endregion

        #region Configuration Validation Tests

        [Theory]
        [InlineData("Host", "")]
        [InlineData("Port", "")]
        [InlineData("EnableSsl", "")]
        [InlineData("Username", "")]
        [InlineData("Password", "")]
        [InlineData("FromEmail", "")]
        public async Task SendEmailAsync_WithMissingRequiredSetting_ShouldThrowException(string settingName, string settingValue)
        {
            // Arrange
            var smtpSection = new Mock<IConfigurationSection>();
            smtpSection.Setup(x => x["Host"]).Returns(settingName == "Host" ? settingValue : "smtp.example.com");
            smtpSection.Setup(x => x["Port"]).Returns(settingName == "Port" ? settingValue : "587");
            smtpSection.Setup(x => x["EnableSsl"]).Returns(settingName == "EnableSsl" ? settingValue : "true");
            smtpSection.Setup(x => x["Username"]).Returns(settingName == "Username" ? settingValue : "user@example.com");
            smtpSection.Setup(x => x["Password"]).Returns(settingName == "Password" ? settingValue : "password");
            smtpSection.Setup(x => x["FromEmail"]).Returns(settingName == "FromEmail" ? settingValue : "from@example.com");
            
            _configurationMock.Setup(x => x.GetSection("SmtpSettings")).Returns(smtpSection.Object);
            var service = new EmailService(_configurationMock.Object, _loggerMock.Object);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => 
                service.SendEmailAsync("test@example.com", "Subject", "Body"));
        }

        [Theory]
        [InlineData("0")]
        [InlineData("-1")]
        [InlineData("65536")]
        [InlineData("not-a-number")]
        public async Task SendEmailAsync_WithInvalidPort_ShouldThrowException(string invalidPort)
        {
            // Arrange
            var smtpSection = new Mock<IConfigurationSection>();
            smtpSection.Setup(x => x["Host"]).Returns("smtp.example.com");
            smtpSection.Setup(x => x["Port"]).Returns(invalidPort);
            smtpSection.Setup(x => x["EnableSsl"]).Returns("true");
            smtpSection.Setup(x => x["Username"]).Returns("user@example.com");
            smtpSection.Setup(x => x["Password"]).Returns("password");
            smtpSection.Setup(x => x["FromEmail"]).Returns("from@example.com");
            
            _configurationMock.Setup(x => x.GetSection("SmtpSettings")).Returns(smtpSection.Object);
            var service = new EmailService(_configurationMock.Object, _loggerMock.Object);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => 
                service.SendEmailAsync("test@example.com", "Subject", "Body"));
        }

        [Theory]
        [InlineData("not-a-boolean")]
        [InlineData("yes")]
        [InlineData("1")]
        public async Task SendEmailAsync_WithInvalidEnableSsl_ShouldThrowException(string invalidSsl)
        {
            // Arrange
            var smtpSection = new Mock<IConfigurationSection>();
            smtpSection.Setup(x => x["Host"]).Returns("smtp.example.com");
            smtpSection.Setup(x => x["Port"]).Returns("587");
            smtpSection.Setup(x => x["EnableSsl"]).Returns(invalidSsl);
            smtpSection.Setup(x => x["Username"]).Returns("user@example.com");
            smtpSection.Setup(x => x["Password"]).Returns("password");
            smtpSection.Setup(x => x["FromEmail"]).Returns("from@example.com");
            
            _configurationMock.Setup(x => x.GetSection("SmtpSettings")).Returns(smtpSection.Object);
            var service = new EmailService(_configurationMock.Object, _loggerMock.Object);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => 
                service.SendEmailAsync("test@example.com", "Subject", "Body"));
        }

        #endregion

        #region MockEmailService Tests

        [Fact]
        public async Task MockEmailService_SendEmailAsync_ShouldLogEmailDetails()
        {
            // Arrange
            var to = "test@example.com";
            var subject = "Test Subject";
            var body = "Test Body";

            // Act
            await _mockEmailService.SendEmailAsync(to, subject, body);

            // Assert
            _mockLoggerMock.Verify(x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"To={to}") && v.ToString()!.Contains($"Subject={subject}")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()
            ), Times.Once);

            _mockLoggerMock.Verify(x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(body)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()
            ), Times.Once);
        }

        [Fact]
        public async Task MockEmailService_TestConnectionAsync_ShouldAlwaysReturnTrue()
        {
            // Act
            var result = await _mockEmailService.TestConnectionAsync();

            // Assert
            result.Should().BeTrue();
            _mockLoggerMock.Verify(x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Mock email service")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()
            ), Times.Once);
        }

        #endregion
    }
}