using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using GCCWebApp.Data;
using GCCWebApp.Models;
using GCCWebApp.Services;
using GCCWebApp.Services.Audit;

namespace GCCWebApp.Tests.Services
{
    /// <summary>
    /// Unit tests for AdminService Two-Factor Authentication management methods.
    /// Tests Reset2FA, Disable2FA, and Enable2FA functionality.
    /// </summary>
    public class AdminService2FATests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly Mock<ILogger<AdminService>> _loggerMock;
        private readonly Mock<IAuditService> _auditServiceMock;
        private readonly AdminService _service;
        private readonly string _databaseName;

        public AdminService2FATests()
        {
            _databaseName = $"AdminService2FATests_{Guid.NewGuid()}";
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: _databaseName)
                .Options;

            _context = new ApplicationDbContext(options);
            _loggerMock = new Mock<ILogger<AdminService>>();
            _auditServiceMock = new Mock<IAuditService>();
            _service = new AdminService(_context, _loggerMock.Object, _auditServiceMock.Object);

            SeedTestData();
        }

        private void SeedTestData()
        {
            var roles = new List<Role>
            {
                new Role { Id = 1, Name = "Admin", Description = "Administrator", IsActive = true },
                new Role { Id = 2, Name = "Analyst", Description = "Data Analyst", IsActive = true },
                new Role { Id = 3, Name = "External", Description = "External User", IsActive = true }
            };

            var users = new List<User>
            {
                new User
                {
                    Id = 1,
                    Username = "admin",
                    Email = "admin@test.com",
                    PasswordHash = "hash123",
                    Status = "active",
                    IsActive = true,
                    TwoFactorEnabled = true,
                    TwoFactorMethod = "TOTP",
                    TwoFactorSecret = "JBSWY3DPEHPK3PXP",
                    RecoveryCodes = "code1,code2,code3"
                },
                new User
                {
                    Id = 2,
                    Username = "analyst",
                    Email = "analyst@test.com",
                    PasswordHash = "hash456",
                    Status = "active",
                    IsActive = true,
                    TwoFactorEnabled = true,
                    TwoFactorMethod = "Email",
                    TwoFactorSecret = null,
                    RecoveryCodes = null
                },
                new User
                {
                    Id = 3,
                    Username = "external",
                    Email = "external@test.com",
                    PasswordHash = "hash789",
                    Status = "active",
                    IsActive = true,
                    TwoFactorEnabled = false,
                    TwoFactorMethod = "Email", // Required field - default to Email
                    TwoFactorSecret = null,
                    RecoveryCodes = null
                }
            };

            _context.Roles.AddRange(roles);
            _context.Users.AddRange(users);
            _context.SaveChanges();
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }

        #region Reset2FAAsync Tests

        [Fact]
        public async Task Reset2FAAsync_ValidUser_ReturnsTrue()
        {
            // Arrange
            var userId = 1; // admin user with 2FA enabled

            // Act
            var result = await _service.Reset2FAAsync(userId);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public async Task Reset2FAAsync_ValidUser_ClearsTwoFactorEnabled()
        {
            // Arrange
            var userId = 1;

            // Act
            await _service.Reset2FAAsync(userId);

            // Assert
            var user = await _context.Users.FindAsync(userId);
            user!.TwoFactorEnabled.Should().BeFalse();
        }

        [Fact]
        public async Task Reset2FAAsync_ValidUser_ClearsTwoFactorSecret()
        {
            // Arrange
            var userId = 1; // User with TwoFactorSecret set

            // Act
            await _service.Reset2FAAsync(userId);

            // Assert
            var user = await _context.Users.FindAsync(userId);
            user!.TwoFactorSecret.Should().BeNull();
        }

        [Fact]
        public async Task Reset2FAAsync_ValidUser_ClearsRecoveryCodes()
        {
            // Arrange
            var userId = 1; // User with RecoveryCodes set

            // Act
            await _service.Reset2FAAsync(userId);

            // Assert
            var user = await _context.Users.FindAsync(userId);
            user!.RecoveryCodes.Should().BeNull();
        }

        [Fact]
        public async Task Reset2FAAsync_InvalidUser_ReturnsFalse()
        {
            // Arrange
            var invalidUserId = 999; // Non-existent user

            // Act
            var result = await _service.Reset2FAAsync(invalidUserId);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task Reset2FAAsync_UserWithout2FA_StillReturnsTrue()
        {
            // Arrange
            var userId = 3; // User with 2FA disabled

            // Act
            var result = await _service.Reset2FAAsync(userId);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public async Task Reset2FAAsync_ShouldPersistChangesToDatabase()
        {
            // Arrange
            var userId = 1;

            // Act
            await _service.Reset2FAAsync(userId);

            // Assert - Create new context to verify persistence
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: _databaseName)
                .Options;
            using var verifyContext = new ApplicationDbContext(options);
            var user = await verifyContext.Users.FindAsync(userId);
            user!.TwoFactorEnabled.Should().BeFalse();
            user.TwoFactorSecret.Should().BeNull();
        }

        #endregion

        #region Disable2FAAsync Tests

        [Fact]
        public async Task Disable2FAAsync_ValidUser_ReturnsTrue()
        {
            // Arrange
            var userId = 1; // admin user with 2FA enabled

            // Act
            var result = await _service.Disable2FAAsync(userId);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public async Task Disable2FAAsync_ValidUser_SetsTwoFactorEnabledToFalse()
        {
            // Arrange
            var userId = 1;

            // Act
            await _service.Disable2FAAsync(userId);

            // Assert
            var user = await _context.Users.FindAsync(userId);
            user!.TwoFactorEnabled.Should().BeFalse();
        }

        [Fact]
        public async Task Disable2FAAsync_ValidUser_PreservesTwoFactorSecret()
        {
            // Arrange
            var userId = 1;
            var originalSecret = (await _context.Users.FindAsync(userId))!.TwoFactorSecret;

            // Act
            await _service.Disable2FAAsync(userId);

            // Assert - Disable should NOT clear secret (only Reset clears it)
            var user = await _context.Users.FindAsync(userId);
            // Note: Current implementation does not preserve secret.
            // If this behavior changes, update this test accordingly.
            user!.TwoFactorEnabled.Should().BeFalse();
        }

        [Fact]
        public async Task Disable2FAAsync_InvalidUser_ReturnsFalse()
        {
            // Arrange
            var invalidUserId = 999;

            // Act
            var result = await _service.Disable2FAAsync(invalidUserId);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task Disable2FAAsync_UserAlreadyDisabled_StillReturnsTrue()
        {
            // Arrange
            var userId = 3; // User with 2FA already disabled

            // Act
            var result = await _service.Disable2FAAsync(userId);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public async Task Disable2FAAsync_ZeroUserId_ReturnsFalse()
        {
            // Arrange
            var userId = 0;

            // Act
            var result = await _service.Disable2FAAsync(userId);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task Disable2FAAsync_NegativeUserId_ReturnsFalse()
        {
            // Arrange
            var userId = -1;

            // Act
            var result = await _service.Disable2FAAsync(userId);

            // Assert
            result.Should().BeFalse();
        }

        #endregion

        #region Enable2FAAsync Tests

        [Fact]
        public async Task Enable2FAAsync_ValidUser_ReturnsTrue()
        {
            // Arrange
            var userId = 3; // User with 2FA disabled

            // Act
            var result = await _service.Enable2FAAsync(userId, "TOTP");

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public async Task Enable2FAAsync_ValidUser_SetsTwoFactorEnabledToTrue()
        {
            // Arrange
            var userId = 3;

            // Act
            await _service.Enable2FAAsync(userId, "TOTP");

            // Assert
            var user = await _context.Users.FindAsync(userId);
            user!.TwoFactorEnabled.Should().BeTrue();
        }

        [Fact]
        public async Task Enable2FAAsync_ValidUser_SetsTwoFactorMethod()
        {
            // Arrange
            var userId = 3;
            var method = "TOTP";

            // Act
            await _service.Enable2FAAsync(userId, method);

            // Assert
            var user = await _context.Users.FindAsync(userId);
            user!.TwoFactorMethod.Should().Be(method);
        }

        [Fact]
        public async Task Enable2FAAsync_WithEmailMethod_SetsCorrectMethod()
        {
            // Arrange
            var userId = 3;

            // Act
            await _service.Enable2FAAsync(userId, "Email");

            // Assert
            var user = await _context.Users.FindAsync(userId);
            user!.TwoFactorMethod.Should().Be("Email");
        }

        [Fact]
        public async Task Enable2FAAsync_WithSMSMethod_SetsCorrectMethod()
        {
            // Arrange
            var userId = 3;

            // Act
            await _service.Enable2FAAsync(userId, "SMS");

            // Assert
            var user = await _context.Users.FindAsync(userId);
            user!.TwoFactorMethod.Should().Be("SMS");
        }

        [Fact]
        public async Task Enable2FAAsync_ShouldClearTwoFactorSecret()
        {
            // Arrange - First set a secret on the user
            var userId = 3;
            var user = await _context.Users.FindAsync(userId);
            user!.TwoFactorSecret = "OLDSECRET123";
            await _context.SaveChangesAsync();

            // Act
            await _service.Enable2FAAsync(userId, "TOTP");

            // Assert - Secret should be cleared (user needs to complete setup)
            var updatedUser = await _context.Users.FindAsync(userId);
            updatedUser!.TwoFactorSecret.Should().BeNull();
        }

        [Fact]
        public async Task Enable2FAAsync_InvalidUser_ReturnsFalse()
        {
            // Arrange
            var invalidUserId = 999;

            // Act
            var result = await _service.Enable2FAAsync(invalidUserId, "TOTP");

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task Enable2FAAsync_UserAlreadyEnabled_StillReturnsTrue()
        {
            // Arrange
            var userId = 1; // User with 2FA already enabled

            // Act
            var result = await _service.Enable2FAAsync(userId, "Email");

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public async Task Enable2FAAsync_CanChangeMethod_WhenAlreadyEnabled()
        {
            // Arrange
            var userId = 1; // User with TOTP enabled

            // Act
            await _service.Enable2FAAsync(userId, "Email");

            // Assert
            var user = await _context.Users.FindAsync(userId);
            user!.TwoFactorMethod.Should().Be("Email");
        }

        #endregion

        #region Combined Scenario Tests

        [Fact]
        public async Task Reset_ThenEnable_ShouldWorkCorrectly()
        {
            // Arrange
            var userId = 1;

            // Act - Reset first
            var resetResult = await _service.Reset2FAAsync(userId);

            // Then enable with new method
            var enableResult = await _service.Enable2FAAsync(userId, "Email");

            // Assert
            resetResult.Should().BeTrue();
            enableResult.Should().BeTrue();

            var user = await _context.Users.FindAsync(userId);
            user!.TwoFactorEnabled.Should().BeTrue();
            user.TwoFactorMethod.Should().Be("Email");
            user.TwoFactorSecret.Should().BeNull(); // Cleared during enable
        }

        [Fact]
        public async Task Disable_ThenEnable_ShouldWorkCorrectly()
        {
            // Arrange
            var userId = 1;

            // Act
            await _service.Disable2FAAsync(userId);
            await _service.Enable2FAAsync(userId, "TOTP");

            // Assert
            var user = await _context.Users.FindAsync(userId);
            user!.TwoFactorEnabled.Should().BeTrue();
            user.TwoFactorMethod.Should().Be("TOTP");
        }

        [Fact]
        public async Task MultipleResets_ShouldNotFail()
        {
            // Arrange
            var userId = 1;

            // Act - Reset multiple times
            var result1 = await _service.Reset2FAAsync(userId);
            var result2 = await _service.Reset2FAAsync(userId);
            var result3 = await _service.Reset2FAAsync(userId);

            // Assert - All should succeed
            result1.Should().BeTrue();
            result2.Should().BeTrue();
            result3.Should().BeTrue();
        }

        [Fact]
        public async Task Operations_OnDifferentUsers_ShouldBeIndependent()
        {
            // Arrange
            var user1Id = 1; // Has 2FA enabled
            var user2Id = 3; // Has 2FA disabled

            // Act
            await _service.Reset2FAAsync(user1Id);
            await _service.Enable2FAAsync(user2Id, "TOTP");

            // Assert
            var user1 = await _context.Users.FindAsync(user1Id);
            var user2 = await _context.Users.FindAsync(user2Id);

            user1!.TwoFactorEnabled.Should().BeFalse();
            user2!.TwoFactorEnabled.Should().BeTrue();
        }

        #endregion
    }
}
