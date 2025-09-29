using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using FluentAssertions;
using GCCWebApp.Services;

namespace GCCWebApp.Tests.Services
{
    // Define TwoFactorData class for testing since it's in EnhancedTwoFactorService
    public class TwoFactorData
    {
        public string Code { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public int Attempts { get; set; }
    }
    
    public class TwoFactorServiceTests
    {
        private readonly Mock<ILogger<TwoFactorService>> _loggerMock;
        private readonly Mock<IEmailService> _emailServiceMock;
        private readonly Mock<IConfiguration> _configurationMock;
        private readonly IMemoryCache _memoryCache;
        private readonly TwoFactorService _service;

        public TwoFactorServiceTests()
        {
            _loggerMock = new Mock<ILogger<TwoFactorService>>();
            _emailServiceMock = new Mock<IEmailService>();
            _configurationMock = new Mock<IConfiguration>();
            _memoryCache = new MemoryCache(new MemoryCacheOptions());

            _service = new TwoFactorService(
                _loggerMock.Object,
                _emailServiceMock.Object,
                _memoryCache,
                _configurationMock.Object
            );
        }

        #region GenerateCode Tests

        [Fact]
        public void GenerateCode_ShouldReturn6DigitCode()
        {
            // Act
            var code = _service.GenerateCode();

            // Assert
            code.Should().NotBeNullOrEmpty();
            code.Should().HaveLength(6);
            code.Should().MatchRegex(@"^\d{6}$", "Code should be exactly 6 digits");
        }

        [Fact]
        public void GenerateCode_ShouldGenerateDifferentCodes()
        {
            // Act
            var codes = new List<string>();
            for (int i = 0; i < 10; i++)
            {
                codes.Add(_service.GenerateCode());
            }

            // Assert
            codes.Distinct().Count().Should().BeGreaterThan(1, 
                "Multiple calls should generate different codes");
        }

        #endregion

        #region StoreCode Tests

        [Fact]
        public void StoreCode_ShouldStoreTwoFactorDataInCache()
        {
            // Arrange
            var userId = "user123";
            var code = "123456";

            // Act
            _service.StoreCode(userId, code);

            // Assert
            var cacheKey = $"2fa_code_{userId}";
            _memoryCache.TryGetValue(cacheKey, out TwoFactorData? data).Should().BeTrue();
            data.Should().NotBeNull();
            data!.Code.Should().Be(code);
            data.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
            data.Attempts.Should().Be(0);
        }

        [Fact]
        public void StoreCode_ShouldLogInformation()
        {
            // Arrange
            var userId = "user123";
            var code = "123456";

            // Act
            _service.StoreCode(userId, code);

            // Assert
            _loggerMock.Verify(x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"2FA code stored for user {userId}")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()
            ), Times.Once);
        }

        #endregion

        #region ValidateCode Tests

        [Fact]
        public void ValidateCode_WithCorrectCode_ShouldReturnTrue()
        {
            // Arrange
            var userId = "user123";
            var code = "123456";
            _service.StoreCode(userId, code);

            // Act
            var result = _service.ValidateCode(userId, code);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void ValidateCode_WithIncorrectCode_ShouldReturnFalse()
        {
            // Arrange
            var userId = "user123";
            var correctCode = "123456";
            var incorrectCode = "654321";
            _service.StoreCode(userId, correctCode);

            // Act
            var result = _service.ValidateCode(userId, incorrectCode);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void ValidateCode_WithNoStoredCode_ShouldReturnFalse()
        {
            // Arrange
            var userId = "user123";
            var code = "123456";

            // Act
            var result = _service.ValidateCode(userId, code);

            // Assert
            result.Should().BeFalse();
            _loggerMock.Verify(x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"No 2FA code found for user {userId}")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()
            ), Times.Once);
        }

        [Fact]
        public void ValidateCode_AfterTooManyAttempts_ShouldReturnFalse()
        {
            // Arrange
            var userId = "user123";
            var code = "123456";
            _service.StoreCode(userId, code);
            
            // Make 3 failed attempts
            _service.ValidateCode(userId, "wrong1");
            _service.ValidateCode(userId, "wrong2");
            _service.ValidateCode(userId, "wrong3");

            // Act - 4th attempt with correct code should still fail
            var result = _service.ValidateCode(userId, code);

            // Assert
            result.Should().BeFalse();
            _loggerMock.Verify(x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"Too many 2FA attempts for user {userId}")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()
            ), Times.Once);
        }

        [Fact]
        public void ValidateCode_WithExpiredCode_ShouldReturnFalse()
        {
            // Arrange
            var userId = "user123";
            var code = "123456";
            var cacheKey = $"2fa_code_{userId}";
            
            // Store code with expired timestamp
            var expiredData = new TwoFactorData
            {
                Code = code,
                CreatedAt = DateTime.UtcNow.AddMinutes(-6), // Expired
                Attempts = 0
            };
            
            var cacheOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5),
                Priority = CacheItemPriority.High,
                Size = 1
            };
            _memoryCache.Set(cacheKey, expiredData, cacheOptions);

            // Act
            var result = _service.ValidateCode(userId, code);

            // Assert
            result.Should().BeFalse();
            _loggerMock.Verify(x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"2FA code expired for user {userId}")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()
            ), Times.Once);
        }

        [Fact]
        public void ValidateCode_WithCorrectCode_ShouldRemoveFromCache()
        {
            // Arrange
            var userId = "user123";
            var code = "123456";
            _service.StoreCode(userId, code);

            // Act
            var result = _service.ValidateCode(userId, code);

            // Assert
            result.Should().BeTrue();
            
            // Verify code and attempts are removed from cache
            var cacheKey = $"2fa_code_{userId}";
            var attemptsKey = $"2fa_attempts_{userId}";
            _memoryCache.TryGetValue(cacheKey, out _).Should().BeFalse();
            _memoryCache.TryGetValue(attemptsKey, out _).Should().BeFalse();
        }

        #endregion

        #region SendCodeByEmailAsync Tests

        [Fact]
        public async Task SendCodeByEmailAsync_ShouldCallEmailService()
        {
            // Arrange
            var email = "test@example.com";
            var code = "123456";

            // Act
            await _service.SendCodeByEmailAsync(email, code);

            // Assert
            _emailServiceMock.Verify(x => x.SendEmailAsync(
                email,
                "Your GCC Database 2FA Code",
                It.Is<string>(body => body.Contains(code) && body.Contains("Two-Factor Authentication"))
            ), Times.Once);
        }

        [Fact]
        public async Task SendCodeByEmailAsync_ShouldLogInformation()
        {
            // Arrange
            var email = "test@example.com";
            var code = "123456";

            // Act
            await _service.SendCodeByEmailAsync(email, code);

            // Assert
            _loggerMock.Verify(x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"2FA code sent to {email}")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()
            ), Times.Once);
        }

        [Fact]
        public async Task SendCodeByEmailAsync_EmailBodyShouldContainRequiredElements()
        {
            // Arrange
            var email = "test@example.com";
            var code = "123456";
            string? capturedBody = null;

            _emailServiceMock
                .Setup(x => x.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Callback<string, string, string>((to, subject, body) => capturedBody = body)
                .Returns(Task.CompletedTask);

            // Act
            await _service.SendCodeByEmailAsync(email, code);

            // Assert
            capturedBody.Should().NotBeNull();
            capturedBody.Should().Contain(code, "Body should contain the code");
            capturedBody.Should().Contain("5 minutes", "Body should mention expiry time");
            capturedBody.Should().Contain("GreenCircle Certified", "Body should contain company name");
            capturedBody.Should().Contain("verification", "Body should mention verification");
        }

        #endregion

        #region ResendCodeAsync Tests

        [Fact]
        public async Task ResendCodeAsync_FirstTime_ShouldSucceed()
        {
            // Arrange
            var userId = "user123";
            var email = "test@example.com";

            // Act
            var result = await _service.ResendCodeAsync(userId, email);

            // Assert
            result.Should().BeTrue();
            _emailServiceMock.Verify(x => x.SendEmailAsync(
                email,
                It.IsAny<string>(),
                It.IsAny<string>()
            ), Times.Once);
        }

        [Fact]
        public async Task ResendCodeAsync_WithinRateLimit_ShouldFail()
        {
            // Arrange
            var userId = "user123";
            var email = "test@example.com";
            
            // First resend
            await _service.ResendCodeAsync(userId, email);

            // Act - Second resend within rate limit
            var result = await _service.ResendCodeAsync(userId, email);

            // Assert
            result.Should().BeFalse();
            _loggerMock.Verify(x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"Resend rate limit hit for user {userId}")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()
            ), Times.Once);
        }

        [Fact]
        public async Task ResendCodeAsync_ShouldGenerateNewCode()
        {
            // Arrange
            var userId = "user123";
            var email = "test@example.com";
            string? capturedBody = null;

            _emailServiceMock
                .Setup(x => x.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Callback<string, string, string>((to, subject, body) => capturedBody = body)
                .Returns(Task.CompletedTask);

            // Act
            var result = await _service.ResendCodeAsync(userId, email);

            // Assert
            result.Should().BeTrue();
            capturedBody.Should().NotBeNull();
            capturedBody.Should().MatchRegex(@"\d{6}", "Body should contain a 6-digit code");
        }

        [Fact]
        public async Task ResendCodeAsync_ShouldStoreNewCodeInCache()
        {
            // Arrange
            var userId = "user123";
            var email = "test@example.com";

            // Act
            await _service.ResendCodeAsync(userId, email);

            // Assert
            var cacheKey = $"2fa_code_{userId}";
            _memoryCache.TryGetValue(cacheKey, out TwoFactorData? data).Should().BeTrue();
            data.Should().NotBeNull();
            data!.Code.Should().MatchRegex(@"^\d{6}$");
        }

        [Fact]
        public async Task ResendCodeAsync_ShouldLogSuccess()
        {
            // Arrange
            var userId = "user123";
            var email = "test@example.com";

            // Act
            await _service.ResendCodeAsync(userId, email);

            // Assert
            _loggerMock.Verify(x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"2FA code resent to {email} for user {userId}")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()
            ), Times.Once);
        }

        #endregion
    }
}