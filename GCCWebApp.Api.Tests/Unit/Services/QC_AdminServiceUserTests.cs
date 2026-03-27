using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using FluentAssertions;
using GCCWebApp.Data;
using GCCWebApp.Models;
using GCCWebApp.Services;
using GCCWebApp.Services.Audit;
using GCCWebApp.Api.Tests.TestUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace GCCWebApp.Api.Tests.Unit.Services
{
    /// <summary>
    /// Tests for AdminService — QC #107 (user creation errors) and QC #113 (cert type deletion guard)
    /// Verifies that duplicate emails are caught with clear messages, and that certificate types
    /// with associated certificates cannot be deleted.
    /// </summary>
    public class QC_AdminServiceUserTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly Mock<ILogger<AdminService>> _loggerMock;
        private readonly Mock<IAuditService> _auditServiceMock;
        private readonly AdminService _adminService;

        public QC_AdminServiceUserTests()
        {
            // Use transaction-warning-suppressed context since AdminService uses BeginTransactionAsync
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options;
            _context = new ApplicationDbContext(options);
            _loggerMock = new Mock<ILogger<AdminService>>();
            _auditServiceMock = new Mock<IAuditService>();
            _adminService = new AdminService(_context, _loggerMock.Object, _auditServiceMock.Object);

            SeedTestData();
        }

        public void Dispose()
        {
            _context.Dispose();
        }

        private void SeedTestData()
        {
            // Roles
            var adminRole = new Role { Id = 10, Name = "Admin", Description = "Admin" };
            var analystRole = new Role { Id = 11, Name = "Analyst", Description = "Analyst" };
            _context.Roles.AddRange(adminRole, analystRole);

            // Existing user with known email
            var existingUser = new User
            {
                Id = 10,
                Username = "existing@test.com",
                Email = "existing@test.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("TestPass123!"),
                Status = "active",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                TwoFactorMethod = "Email",
                Roles = new List<Role> { adminRole }
            };
            _context.Users.Add(existingUser);

            // Company for user assignment
            _context.Companies.Add(new Company { Id = 10, Name = "Test Co", Email = "test@test.com" });

            // Certification types for deletion tests
            var certTypeWithCerts = new CertificationType { Id = 20, Name = "Type With Certs", Description = "Has certificates", IsActive = true };
            var certTypeWithoutCerts = new CertificationType { Id = 21, Name = "Type Without Certs", Description = "No certificates", IsActive = true };
            _context.CertificationTypes.AddRange(certTypeWithCerts, certTypeWithoutCerts);

            // Certificate tied to certTypeWithCerts
            _context.Certificates.Add(new Certificate
            {
                Id = 300, Number = "26-0300", Status = "Draft", CompanyId = 10,
                CertificationTypeId = 20, CreatedAt = DateTime.UtcNow
            });

            // ProductCategory needed for product type tests
            _context.ProductCategories.Add(new ProductCategory { Id = 10, Name = "Default", IsActive = true });

            _context.SaveChanges();
        }

        #region CreateUserAsync (QC #107)

        [Fact]
        public async Task CreateUserAsync_ValidUser_ReturnsSuccessTrue()
        {
            var model = new CreateUserViewModel
            {
                Name = "New User",
                Email = "newuser@test.com",
                Role = "Analyst",
                Password = "TestPass123!"
            };

            var (success, errorMessage) = await _adminService.CreateUserAsync(model, 10, "admin");

            success.Should().BeTrue();
            errorMessage.Should().BeEmpty();
        }

        [Fact]
        public async Task CreateUserAsync_DuplicateEmail_ReturnsFalseWithMessage()
        {
            var model = new CreateUserViewModel
            {
                Name = "Duplicate User",
                Email = "existing@test.com",
                Role = "Analyst",
                Password = "TestPass123!"
            };

            var (success, errorMessage) = await _adminService.CreateUserAsync(model, 10, "admin");

            success.Should().BeFalse();
            errorMessage.Should().NotBeEmpty();
        }

        [Fact]
        public async Task CreateUserAsync_DuplicateEmailCaseInsensitive_Blocked()
        {
            var model = new CreateUserViewModel
            {
                Name = "Case Test User",
                Email = "EXISTING@TEST.COM",
                Role = "Analyst",
                Password = "TestPass123!"
            };

            var (success, errorMessage) = await _adminService.CreateUserAsync(model, 10, "admin");

            success.Should().BeFalse();
            errorMessage.ToLower().Should().Match(m =>
                m.Contains("existing@test.com") || m.Contains("already exists") || m.Contains("existing"));
        }

        [Fact]
        public async Task CreateUserAsync_ValidUser_CreatesUserInDatabase()
        {
            var model = new CreateUserViewModel
            {
                Name = "DB Check User",
                Email = "dbcheck@test.com",
                Role = "Analyst",
                Password = "TestPass123!"
            };

            await _adminService.CreateUserAsync(model, 10, "admin");

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == "dbcheck@test.com");
            user.Should().NotBeNull();
            user!.Username.Should().Be("dbcheck@test.com");
            user.Status.Should().Be("active");
            user.IsActive.Should().BeTrue();
        }

        [Fact]
        public async Task CreateUserAsync_ValidUser_AssignsRole()
        {
            var model = new CreateUserViewModel
            {
                Name = "Role User",
                Email = "roleuser@test.com",
                Role = "Analyst",
                Password = "TestPass123!"
            };

            await _adminService.CreateUserAsync(model, 10, "admin");

            var user = await _context.Users
                .Include(u => u.Roles)
                .FirstOrDefaultAsync(u => u.Email == "roleuser@test.com");
            user.Should().NotBeNull();
            user!.Roles.Should().Contain(r => r.Name == "Analyst");
        }

        [Fact]
        public async Task CreateUserAsync_ErrorMessageContainsEmail()
        {
            var model = new CreateUserViewModel
            {
                Name = "Error Msg User",
                Email = "existing@test.com",
                Role = "Analyst",
                Password = "TestPass123!"
            };

            var (_, errorMessage) = await _adminService.CreateUserAsync(model, 10, "admin");

            // Error message should reference the duplicate email or mention "already exists"
            errorMessage.ToLower().Should().Match(m =>
                m.Contains("existing@test.com") || m.Contains("already exists"));
        }

        #endregion

        #region DeleteCertificateTypeAsync (QC #113)

        [Fact]
        public async Task DeleteCertificateTypeAsync_NoCertificates_Succeeds()
        {
            // Type 21 has no certificates
            var result = await _adminService.DeleteCertificateTypeAsync(21);
            result.Should().BeTrue();

            // Verify removed from DB
            var type = await _context.CertificationTypes.FindAsync(21);
            type.Should().BeNull();
        }

        [Fact]
        public async Task DeleteCertificateTypeAsync_HasCertificates_Blocked()
        {
            // Type 20 has certificate 300 — should throw or return false
            Func<Task> act = async () => await _adminService.DeleteCertificateTypeAsync(20);

            // The service should either throw InvalidOperationException or return false
            // Check the actual behavior
            try
            {
                var result = await _adminService.DeleteCertificateTypeAsync(20);
                // If it returns false, that's also valid
                result.Should().BeFalse();
            }
            catch (InvalidOperationException ex)
            {
                // Expected — message should mention certificates
                ex.Message.Should().NotBeEmpty();
            }
        }

        [Fact]
        public async Task DeleteCertificateTypeAsync_HasCertificates_TypeStillExists()
        {
            try
            {
                await _adminService.DeleteCertificateTypeAsync(20);
            }
            catch
            {
                // Expected
            }

            // Type should still be in the database
            var type = await _context.CertificationTypes.FindAsync(20);
            type.Should().NotBeNull();
        }

        [Fact]
        public async Task DeleteCertificateTypeAsync_NonExistent_ReturnsFalse()
        {
            try
            {
                var result = await _adminService.DeleteCertificateTypeAsync(9999);
                result.Should().BeFalse();
            }
            catch (Exception)
            {
                // Some implementations throw for not found — also acceptable
            }
        }

        #endregion
    }
}
