using FluentAssertions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System.Security.Claims;
using System.Text.Encodings.Web;
using GCCWebApp.Authentication;
using GCCWebApp.Models;
using GCCWebApp.Services;

namespace GCCWebApp.Tests.Authentication
{
    public class ApiKeyAuthenticationHandlerTests : IDisposable
    {
        private readonly Mock<IApiKeyService> _apiKeyServiceMock;
        private readonly Mock<IOptionsMonitor<ApiKeyAuthenticationOptions>> _optionsMock;
        private readonly Mock<ILoggerFactory> _loggerFactoryMock;
        private readonly Mock<ILogger<ApiKeyAuthenticationHandler>> _loggerMock;
        private readonly Mock<UrlEncoder> _urlEncoderMock;
        // Removed ISystemClock as it's obsolete and not used in constructor
        private readonly DefaultHttpContext _httpContext;
        private readonly ApiKeyAuthenticationHandler _handler;

        public ApiKeyAuthenticationHandlerTests()
        {
            _apiKeyServiceMock = new Mock<IApiKeyService>();
            _optionsMock = new Mock<IOptionsMonitor<ApiKeyAuthenticationOptions>>();
            _loggerFactoryMock = new Mock<ILoggerFactory>();
            _loggerMock = new Mock<ILogger<ApiKeyAuthenticationHandler>>();
            _urlEncoderMock = new Mock<UrlEncoder>();
            // Removed ISystemClock initialization
            _httpContext = new DefaultHttpContext();

            var options = new ApiKeyAuthenticationOptions
            {
                HeaderName = "X-API-Key"
            };

            _optionsMock.Setup(o => o.CurrentValue).Returns(options);
            // Fix: Mock the underlying CreateLogger method, not the extension method
            _loggerFactoryMock.Setup(lf => lf.CreateLogger(It.IsAny<string>())).Returns(_loggerMock.Object);

            var authScheme = new AuthenticationScheme("ApiKey", "ApiKey", typeof(ApiKeyAuthenticationHandler));

            _handler = new ApiKeyAuthenticationHandler(
                _optionsMock.Object,
                _loggerFactoryMock.Object,
                _urlEncoderMock.Object,
                _apiKeyServiceMock.Object);

            // Initialize the handler
            _handler.InitializeAsync(authScheme, _httpContext).Wait();
        }

        [Fact]
        public async Task HandleAuthenticateAsync_ValidApiKeyInHeader_ShouldSucceed()
        {
            // Arrange
            var apiKey = "valid-api-key";
            var apiKeyModel = new ApiKey
            {
                Id = 1,
                Key = apiKey,
                Name = "Test Key",
                IsActive = true,
                AllowedIPs = "*",
                AllowedOrigins = "*"
            };

            _httpContext.Request.Headers["X-API-Key"] = apiKey;
            _httpContext.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("192.168.1.100");

            _apiKeyServiceMock.Setup(s => s.ValidateApiKeyAsync(apiKey))
                .ReturnsAsync(apiKeyModel);
            _apiKeyServiceMock.Setup(s => s.IsValidIP(apiKeyModel, It.IsAny<string>()))
                .Returns(true);
            _apiKeyServiceMock.Setup(s => s.IsValidOrigin(apiKeyModel, It.IsAny<string>()))
                .Returns(true);

            // Act
            var result = await _handler.AuthenticateAsync();

            // Assert
            result.Succeeded.Should().BeTrue();
            result.Principal.Should().NotBeNull();
            result.Principal.FindFirst("ApiKeyId")?.Value.Should().Be(apiKeyModel.Id.ToString());
            result.Principal.FindFirst("ApiKeyName")?.Value.Should().Be(apiKeyModel.Name);
        }

        [Fact]
        public async Task HandleAuthenticateAsync_ValidApiKeyInQueryParameter_ShouldSucceed()
        {
            // Arrange
            var apiKey = "valid-api-key";
            var apiKeyModel = new ApiKey
            {
                Id = 1,
                Key = apiKey,
                Name = "Test Key",
                IsActive = true,
                AllowedIPs = "*",
                AllowedOrigins = "*"
            };

            _httpContext.Request.QueryString = new QueryString($"?api_key={apiKey}");
            _httpContext.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("192.168.1.100");

            _apiKeyServiceMock.Setup(s => s.ValidateApiKeyAsync(apiKey))
                .ReturnsAsync(apiKeyModel);
            _apiKeyServiceMock.Setup(s => s.IsValidIP(apiKeyModel, It.IsAny<string>()))
                .Returns(true);
            _apiKeyServiceMock.Setup(s => s.IsValidOrigin(apiKeyModel, It.IsAny<string>()))
                .Returns(true);

            // Act
            var result = await _handler.AuthenticateAsync();

            // Assert
            result.Succeeded.Should().BeTrue();
            result.Principal.Should().NotBeNull();
        }

        [Fact]
        public async Task HandleAuthenticateAsync_NoApiKey_ShouldFail()
        {
            // Act
            var result = await _handler.AuthenticateAsync();

            // Assert
            result.Succeeded.Should().BeFalse();
            result.Failure?.Message.Should().Contain("API key not found");
        }

        [Fact]
        public async Task HandleAuthenticateAsync_InvalidApiKey_ShouldFail()
        {
            // Arrange
            var apiKey = "invalid-api-key";
            _httpContext.Request.Headers["X-API-Key"] = apiKey;

            _apiKeyServiceMock.Setup(s => s.ValidateApiKeyAsync(apiKey))
                .ReturnsAsync((ApiKey)null);

            // Act
            var result = await _handler.AuthenticateAsync();

            // Assert
            result.Succeeded.Should().BeFalse();
            result.Failure?.Message.Should().Contain("Invalid API key");
        }

        [Fact]
        public async Task HandleAuthenticateAsync_InvalidIPAddress_ShouldFailAndRecordViolation()
        {
            // Arrange
            var apiKey = "valid-api-key";
            var apiKeyModel = new ApiKey
            {
                Id = 1,
                Key = apiKey,
                Name = "Test Key",
                IsActive = true,
                AllowedIPs = "10.0.0.1",
                AllowedOrigins = "*"
            };

            _httpContext.Request.Headers["X-API-Key"] = apiKey;
            _httpContext.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("192.168.1.100");

            _apiKeyServiceMock.Setup(s => s.ValidateApiKeyAsync(apiKey))
                .ReturnsAsync(apiKeyModel);
            _apiKeyServiceMock.Setup(s => s.IsValidIP(apiKeyModel, "192.168.1.100"))
                .Returns(false);

            // Act
            var result = await _handler.AuthenticateAsync();

            // Assert
            result.Succeeded.Should().BeFalse();
            result.Failure?.Message.Should().Contain("IP address not allowed");

            _apiKeyServiceMock.Verify(
                s => s.RecordSecurityViolationAsync(
                    apiKeyModel.Id, 
                    "INVALID_IP", 
                    It.IsAny<string>()),
                Times.Once);
        }

        [Fact]
        public async Task HandleAuthenticateAsync_InvalidOrigin_ShouldFailAndRecordViolation()
        {
            // Arrange
            var apiKey = "valid-api-key";
            var apiKeyModel = new ApiKey
            {
                Id = 1,
                Key = apiKey,
                Name = "Test Key",
                IsActive = true,
                AllowedIPs = "*",
                AllowedOrigins = "https://allowed.com"
            };

            _httpContext.Request.Headers["X-API-Key"] = apiKey;
            _httpContext.Request.Headers["Origin"] = "https://malicious.com";
            _httpContext.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("192.168.1.100");

            _apiKeyServiceMock.Setup(s => s.ValidateApiKeyAsync(apiKey))
                .ReturnsAsync(apiKeyModel);
            _apiKeyServiceMock.Setup(s => s.IsValidIP(apiKeyModel, It.IsAny<string>()))
                .Returns(true);
            _apiKeyServiceMock.Setup(s => s.IsValidOrigin(apiKeyModel, "https://malicious.com"))
                .Returns(false);

            // Act
            var result = await _handler.AuthenticateAsync();

            // Assert
            result.Succeeded.Should().BeFalse();
            result.Failure?.Message.Should().Contain("Origin not allowed");

            _apiKeyServiceMock.Verify(
                s => s.RecordSecurityViolationAsync(
                    apiKeyModel.Id, 
                    "INVALID_ORIGIN", 
                    It.IsAny<string>()),
                Times.Once);
        }

        [Fact]
        public async Task HandleAuthenticateAsync_RateLimitExceeded_ShouldFailAndRecordViolation()
        {
            // Arrange
            var apiKey = "valid-api-key";
            var apiKeyModel = new ApiKey
            {
                Id = 1,
                Key = apiKey,
                Name = "Test Key",
                IsActive = true,
                AllowedIPs = "*",
                AllowedOrigins = "*"
            };

            _httpContext.Request.Headers["X-API-Key"] = apiKey;
            _httpContext.Request.Path = "/api/v1/certificates";
            _httpContext.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("192.168.1.100");

            _apiKeyServiceMock.Setup(s => s.ValidateApiKeyAsync(apiKey))
                .ReturnsAsync(apiKeyModel);
            _apiKeyServiceMock.Setup(s => s.IsValidIP(apiKeyModel, It.IsAny<string>()))
                .Returns(true);
            _apiKeyServiceMock.Setup(s => s.IsValidOrigin(apiKeyModel, It.IsAny<string>()))
                .Returns(true);
            _apiKeyServiceMock.Setup(s => s.IsRateLimitExceededAsync(apiKeyModel.Id, "/api/v1/certificates"))
                .ReturnsAsync(true);

            // Act
            var result = await _handler.AuthenticateAsync();

            // Assert
            result.Succeeded.Should().BeFalse();
            result.Failure?.Message.Should().Contain("Rate limit exceeded");

            _apiKeyServiceMock.Verify(
                s => s.RecordSecurityViolationAsync(
                    apiKeyModel.Id, 
                    "RATE_LIMIT_EXCEEDED", 
                    It.IsAny<string>()),
                Times.Once);
        }

        [Fact]
        public async Task HandleAuthenticateAsync_BlockedApiKey_ShouldFail()
        {
            // Arrange
            var apiKey = "blocked-api-key";
            var apiKeyModel = new ApiKey
            {
                Id = 1,
                Key = apiKey,
                Name = "Blocked Key",
                IsActive = true,
                IsTemporarilyBlocked = true,
                BlockedUntil = DateTime.UtcNow.AddHours(1),
                AllowedIPs = "*",
                AllowedOrigins = "*"
            };

            _httpContext.Request.Headers["X-API-Key"] = apiKey;
            _httpContext.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("192.168.1.100");

            _apiKeyServiceMock.Setup(s => s.ValidateApiKeyAsync(apiKey))
                .ReturnsAsync(apiKeyModel);

            // Act
            var result = await _handler.AuthenticateAsync();

            // Assert
            result.Succeeded.Should().BeFalse();
            result.Failure?.Message.Should().Contain("API key is blocked");
        }

        [Fact]
        public async Task HandleAuthenticateAsync_SuccessfulAuthentication_ShouldUpdateLastUsed()
        {
            // Arrange
            var apiKey = "valid-api-key";
            var apiKeyModel = new ApiKey
            {
                Id = 1,
                Key = apiKey,
                Name = "Test Key",
                IsActive = true,
                AllowedIPs = "*",
                AllowedOrigins = "*"
            };

            _httpContext.Request.Headers["X-API-Key"] = apiKey;
            _httpContext.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("192.168.1.100");

            _apiKeyServiceMock.Setup(s => s.ValidateApiKeyAsync(apiKey))
                .ReturnsAsync(apiKeyModel);
            _apiKeyServiceMock.Setup(s => s.IsValidIP(apiKeyModel, It.IsAny<string>()))
                .Returns(true);
            _apiKeyServiceMock.Setup(s => s.IsValidOrigin(apiKeyModel, It.IsAny<string>()))
                .Returns(true);

            // Act
            var result = await _handler.AuthenticateAsync();

            // Assert
            result.Succeeded.Should().BeTrue();
            
            _apiKeyServiceMock.Verify(
                s => s.UpdateLastUsedAsync(apiKey),
                Times.Once);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null)]
        public async Task HandleAuthenticateAsync_EmptyApiKey_ShouldFail(string apiKey)
        {
            // Arrange
            if (apiKey != null)
            {
                _httpContext.Request.Headers["X-API-Key"] = apiKey;
            }

            // Act
            var result = await _handler.AuthenticateAsync();

            // Assert
            result.Succeeded.Should().BeFalse();
        }

        [Fact]
        public async Task HandleAuthenticateAsync_HeaderTakesPrecedenceOverQueryParameter()
        {
            // Arrange
            var headerApiKey = "header-api-key";
            var queryApiKey = "query-api-key";
            
            var apiKeyModel = new ApiKey
            {
                Id = 1,
                Key = headerApiKey,
                Name = "Header Key",
                IsActive = true,
                AllowedIPs = "*",
                AllowedOrigins = "*"
            };

            _httpContext.Request.Headers["X-API-Key"] = headerApiKey;
            _httpContext.Request.QueryString = new QueryString($"?api_key={queryApiKey}");
            _httpContext.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("192.168.1.100");

            _apiKeyServiceMock.Setup(s => s.ValidateApiKeyAsync(headerApiKey))
                .ReturnsAsync(apiKeyModel);
            _apiKeyServiceMock.Setup(s => s.IsValidIP(apiKeyModel, It.IsAny<string>()))
                .Returns(true);
            _apiKeyServiceMock.Setup(s => s.IsValidOrigin(apiKeyModel, It.IsAny<string>()))
                .Returns(true);

            // Act
            var result = await _handler.AuthenticateAsync();

            // Assert
            result.Succeeded.Should().BeTrue();
            
            // Verify that header API key was used, not query parameter
            _apiKeyServiceMock.Verify(s => s.ValidateApiKeyAsync(headerApiKey), Times.Once);
            _apiKeyServiceMock.Verify(s => s.ValidateApiKeyAsync(queryApiKey), Times.Never);
        }

        [Fact]
        public async Task HandleAuthenticateAsync_SqlInjectionAttempt_ShouldFail()
        {
            // Arrange
            var maliciousApiKey = "'; DROP TABLE ApiKeys; --";
            _httpContext.Request.Headers["X-API-Key"] = maliciousApiKey;

            _apiKeyServiceMock.Setup(s => s.ValidateApiKeyAsync(maliciousApiKey))
                .ReturnsAsync((ApiKey)null);

            // Act
            var result = await _handler.AuthenticateAsync();

            // Assert
            result.Succeeded.Should().BeFalse();
            result.Failure?.Message.Should().Contain("Invalid API key");
        }

        [Fact]
        public async Task HandleAuthenticateAsync_ExcessivelyLongApiKey_ShouldFail()
        {
            // Arrange
            var longApiKey = new string('A', 1000); // 1000 character API key
            _httpContext.Request.Headers["X-API-Key"] = longApiKey;

            _apiKeyServiceMock.Setup(s => s.ValidateApiKeyAsync(longApiKey))
                .ReturnsAsync((ApiKey)null);

            // Act
            var result = await _handler.AuthenticateAsync();

            // Assert
            result.Succeeded.Should().BeFalse();
            result.Failure?.Message.Should().Contain("Invalid API key");
        }

        [Fact]
        public async Task HandleAuthenticateAsync_ApiKeyWithSpecialCharacters_ShouldHandleCorrectly()
        {
            // Arrange
            var specialCharApiKey = "api-key_123.!@#$%^&*()";
            var apiKeyModel = new ApiKey
            {
                Id = 1,
                Key = specialCharApiKey,
                Name = "Special Char Key",
                IsActive = true,
                AllowedIPs = "*",
                AllowedOrigins = "*"
            };

            _httpContext.Request.Headers["X-API-Key"] = specialCharApiKey;
            _httpContext.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("192.168.1.100");

            _apiKeyServiceMock.Setup(s => s.ValidateApiKeyAsync(specialCharApiKey))
                .ReturnsAsync(apiKeyModel);
            _apiKeyServiceMock.Setup(s => s.IsValidIP(apiKeyModel, It.IsAny<string>()))
                .Returns(true);
            _apiKeyServiceMock.Setup(s => s.IsValidOrigin(apiKeyModel, It.IsAny<string>()))
                .Returns(true);

            // Act
            var result = await _handler.AuthenticateAsync();

            // Assert
            result.Succeeded.Should().BeTrue();
            result.Principal.Should().NotBeNull();
        }

        [Fact]
        public async Task HandleAuthenticateAsync_ConcurrentRequests_ShouldHandleCorrectly()
        {
            // Arrange
            var apiKey = "concurrent-test-key";
            var apiKeyModel = new ApiKey
            {
                Id = 1,
                Key = apiKey,
                Name = "Concurrent Key",
                IsActive = true,
                AllowedIPs = "*",
                AllowedOrigins = "*"
            };

            var tasks = new List<Task<AuthenticateResult>>();
            
            for (int i = 0; i < 10; i++)
            {
                var context = new DefaultHttpContext();
                context.Request.Headers["X-API-Key"] = apiKey;
                context.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("192.168.1.100");

                var handler = new ApiKeyAuthenticationHandler(
                    _optionsMock.Object,
                    _loggerFactoryMock.Object,
                    _urlEncoderMock.Object,
                    _apiKeyServiceMock.Object);

                var authScheme = new AuthenticationScheme("ApiKey", "ApiKey", typeof(ApiKeyAuthenticationHandler));
                await handler.InitializeAsync(authScheme, context);

                tasks.Add(handler.AuthenticateAsync());
            }

            _apiKeyServiceMock.Setup(s => s.ValidateApiKeyAsync(apiKey))
                .ReturnsAsync(apiKeyModel);
            _apiKeyServiceMock.Setup(s => s.IsValidIP(apiKeyModel, It.IsAny<string>()))
                .Returns(true);
            _apiKeyServiceMock.Setup(s => s.IsValidOrigin(apiKeyModel, It.IsAny<string>()))
                .Returns(true);

            // Act
            var results = await Task.WhenAll(tasks);

            // Assert
            results.Should().HaveCount(10);
            results.Should().OnlyContain(r => r.Succeeded);
        }

        [Fact]
        public async Task HandleAuthenticateAsync_ExpiredApiKey_ShouldFail()
        {
            // Arrange
            var apiKey = "expired-api-key";
            var expiredApiKeyModel = new ApiKey
            {
                Id = 1,
                Key = apiKey,
                Name = "Expired Key",
                IsActive = true,
                AllowedIPs = "*",
                AllowedOrigins = "*",
                ExpiresAt = DateTime.UtcNow.AddDays(-1) // Expired yesterday
            };

            _httpContext.Request.Headers["X-API-Key"] = apiKey;
            _httpContext.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("192.168.1.100");

            _apiKeyServiceMock.Setup(s => s.ValidateApiKeyAsync(apiKey))
                .ReturnsAsync(expiredApiKeyModel);

            // Act
            var result = await _handler.AuthenticateAsync();

            // Assert
            result.Succeeded.Should().BeFalse();
            result.Failure?.Message.Should().Contain("API key has expired");
        }

        public void Dispose()
        {
            _httpContext?.Response.Body?.Dispose();
        }
    }
}