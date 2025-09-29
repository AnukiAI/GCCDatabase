using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using GCCWebApp.Data;
using GCCWebApp.Models;
using GCCWebApp.Services;

namespace GCCWebApp.Tests.Services
{
    public class ApiKeyServiceTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly Mock<ILogger<ApiKeyService>> _loggerMock;
        private readonly ApiKeyService _service;

        public ApiKeyServiceTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new ApplicationDbContext(options);
            _loggerMock = new Mock<ILogger<ApiKeyService>>();
            _service = new ApiKeyService(_context, _loggerMock.Object);
        }

        [Fact]
        public async Task CreateApiKeyAsync_ShouldCreateValidApiKey()
        {
            // Arrange
            var name = "Test API Key";
            var description = "Test Description";
            var createdBy = "test@example.com";

            // Act
            var result = await _service.CreateApiKeyAsync(name, description, createdBy);

            // Assert
            result.Should().NotBeNull();
            result.Name.Should().Be(name);
            result.Description.Should().Be(description);
            result.CreatedBy.Should().Be(createdBy);
            result.Key.Should().NotBeNullOrEmpty();
            result.IsActive.Should().BeTrue();
            result.ExpiresAt.Should().BeAfter(DateTime.UtcNow);
        }

        [Fact]
        public async Task CreateApiKeyAsync_ShouldGenerateUniqueKeys()
        {
            // Arrange
            var name1 = "API Key 1";
            var name2 = "API Key 2";
            var description = "Test Description";
            var createdBy = "test@example.com";

            // Act
            var key1 = await _service.CreateApiKeyAsync(name1, description, createdBy);
            var key2 = await _service.CreateApiKeyAsync(name2, description, createdBy);

            // Assert
            key1.Key.Should().NotBe(key2.Key);
        }

        [Fact]
        public async Task ValidateApiKeyAsync_ValidActiveKey_ShouldReturnApiKey()
        {
            // Arrange
            var apiKey = await _service.CreateApiKeyAsync("Test Key", "Test", "test@example.com");

            // Act
            var result = await _service.ValidateApiKeyAsync(apiKey.Key);

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().Be(apiKey.Id);
        }

        [Fact]
        public async Task ValidateApiKeyAsync_InvalidKey_ShouldReturnNull()
        {
            // Act
            var result = await _service.ValidateApiKeyAsync("invalid-key");

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task ValidateApiKeyAsync_ExpiredKey_ShouldReturnNull()
        {
            // Arrange
            var apiKey = await _service.CreateApiKeyAsync("Test Key", "Test", "test@example.com", DateTime.UtcNow.AddDays(-1));

            // Act
            var result = await _service.ValidateApiKeyAsync(apiKey.Key);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task ValidateApiKeyAsync_InactiveKey_ShouldReturnNull()
        {
            // Arrange
            var apiKey = await _service.CreateApiKeyAsync("Test Key", "Test", "test@example.com");
            await _service.RevokeApiKeyAsync(apiKey.Id);

            // Act
            var result = await _service.ValidateApiKeyAsync(apiKey.Key);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task RevokeApiKeyAsync_ValidId_ShouldDeactivateKey()
        {
            // Arrange
            var apiKey = await _service.CreateApiKeyAsync("Test Key", "Test", "test@example.com");

            // Act
            var result = await _service.RevokeApiKeyAsync(apiKey.Id);

            // Assert
            result.Should().BeTrue();
            
            var updatedKey = await _service.GetApiKeyByIdAsync(apiKey.Id);
            updatedKey.IsActive.Should().BeFalse();
        }

        [Fact]
        public async Task RevokeApiKeyAsync_InvalidId_ShouldReturnFalse()
        {
            // Act
            var result = await _service.RevokeApiKeyAsync(999);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task UpdateLastUsedAsync_ValidKey_ShouldUpdateTimestamp()
        {
            // Arrange
            var apiKey = await _service.CreateApiKeyAsync("Test Key", "Test", "test@example.com");
            var originalLastUsed = apiKey.LastUsedAt;

            // Add small delay to ensure timestamp difference
            await Task.Delay(10);

            // Act
            await _service.UpdateLastUsedAsync(apiKey.Key);

            // Assert
            var updatedKey = await _service.GetApiKeyByIdAsync(apiKey.Id);
            updatedKey.LastUsedAt.Should().BeAfter(originalLastUsed ?? DateTime.MinValue);
        }

        [Theory]
        [InlineData("http://localhost:3000", "localhost", true)]
        [InlineData("https://example.com", "example.com", true)]
        [InlineData("https://malicious.com", "example.com", false)]
        [InlineData("", "example.com", false)]
        public void IsValidOrigin_ShouldValidateOriginCorrectly(string origin, string allowedOrigin, bool expected)
        {
            // Arrange
            var apiKey = new ApiKey
            {
                Name = "Test Key",
                Key = "test-key-123",
                AllowedOrigins = allowedOrigin
            };

            // Act
            var result = _service.IsValidOrigin(apiKey, origin);

            // Assert
            result.Should().Be(expected);
        }

        [Theory]
        [InlineData("192.168.1.100", "192.168.1.100", true)]
        [InlineData("10.0.0.1", "10.0.0.1,192.168.1.0/24", true)]
        [InlineData("192.168.1.50", "192.168.1.0/24", true)]
        [InlineData("10.0.0.1", "192.168.1.0/24", false)]
        [InlineData("", "192.168.1.100", false)]
        public void IsValidIP_ShouldValidateIPCorrectly(string ipAddress, string allowedIPs, bool expected)
        {
            // Arrange
            var apiKey = new ApiKey
            {
                Name = "Test Key",
                Key = "test-key-123",
                AllowedIPs = allowedIPs
            };

            // Act
            var result = _service.IsValidIP(apiKey, ipAddress);

            // Assert
            result.Should().Be(expected);
        }

        [Fact]
        public async Task RecordSecurityViolationAsync_ShouldLogViolation()
        {
            // Arrange
            var apiKey = await _service.CreateApiKeyAsync("Test Key", "Test", "test@example.com");
            var violationType = "INVALID_ORIGIN";
            var details = "Origin not in allowed list";

            // Act
            await _service.RecordSecurityViolationAsync(apiKey.Id, violationType, details);

            // Assert
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Security violation")),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task BlockApiKeyAsync_ShouldBlockKeyTemporarily()
        {
            // Arrange
            var apiKey = await _service.CreateApiKeyAsync("Test Key", "Test", "test@example.com");
            var duration = TimeSpan.FromHours(1);
            var reason = "Rate limit exceeded";

            // Act
            var result = await _service.BlockApiKeyAsync(apiKey.Id, duration, reason);

            // Assert
            result.Should().BeTrue();
            
            var blockedKey = await _service.GetApiKeyByIdAsync(apiKey.Id);
            blockedKey.IsBlocked.Should().BeTrue();
            blockedKey.BlockedUntil.Should().BeAfter(DateTime.UtcNow);
        }

        [Fact]
        public async Task UnblockApiKeyAsync_ShouldUnblockKey()
        {
            // Arrange
            var apiKey = await _service.CreateApiKeyAsync("Test Key", "Test", "test@example.com");
            await _service.BlockApiKeyAsync(apiKey.Id, TimeSpan.FromHours(1), "Test block");

            // Act
            var result = await _service.UnblockApiKeyAsync(apiKey.Id);

            // Assert
            result.Should().BeTrue();
            
            var unblockedKey = await _service.GetApiKeyByIdAsync(apiKey.Id);
            unblockedKey.IsBlocked.Should().BeFalse();
            unblockedKey.BlockedUntil.Should().BeNull();
        }

        [Theory]
        [InlineData("short", false)]
        [InlineData("abc123", false)]
        [InlineData("AVeryStrongApiKeyWith64CharactersAndMixedCaseAndNumbers123456789", true)]
        public async Task ValidateApiKeyStrengthAsync_ShouldValidateKeyStrength(string key, bool expected)
        {
            // Act
            var result = await _service.ValidateApiKeyStrengthAsync(key);

            // Assert
            result.Should().Be(expected);
        }

        [Fact]
        public async Task RotateApiKeyAsync_ShouldGenerateNewKey()
        {
            // Arrange
            var apiKey = await _service.CreateApiKeyAsync("Test Key", "Test", "test@example.com");
            var originalKey = apiKey.Key;

            // Act
            var result = await _service.RotateApiKeyAsync(apiKey.Id);

            // Assert
            result.Should().BeTrue();
            
            var rotatedKey = await _service.GetApiKeyByIdAsync(apiKey.Id);
            rotatedKey.Key.Should().NotBe(originalKey);
            rotatedKey.LastRotatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        }

        [Fact]
        public async Task GetApiKeyUsageStatsAsync_ShouldReturnUsageData()
        {
            // Arrange
            var apiKey = await _service.CreateApiKeyAsync("Test Key", "Test", "test@example.com");
            var fromDate = DateTime.UtcNow.AddDays(-7);

            // Act
            var stats = await _service.GetApiKeyUsageStatsAsync(apiKey.Id, fromDate);

            // Assert
            stats.Should().NotBeNull();
            stats.Should().ContainKey("totalRequests");
            stats.Should().ContainKey("lastUsed");
        }

        [Fact]
        public async Task IsRateLimitExceededAsync_ShouldEnforceRateLimit()
        {
            // Arrange
            var apiKey = await _service.CreateApiKeyAsync("Test Key", "Test", "test@example.com");
            var endpoint = "/api/v1/certificates";

            // Act - Make multiple requests rapidly
            var results = new List<bool>();
            for (int i = 0; i < 10; i++)
            {
                results.Add(await _service.IsRateLimitExceededAsync(apiKey.Id, endpoint));
            }

            // Assert - Should eventually hit rate limit
            results.Should().Contain(true, "Rate limiting should kick in after multiple requests");
        }

        public void Dispose()
        {
            _context?.Dispose();
        }
    }
}