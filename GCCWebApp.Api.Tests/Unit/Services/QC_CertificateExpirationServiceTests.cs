using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using FluentAssertions;
using GCCWebApp.Data;
using GCCWebApp.Models;
using GCCWebApp.Services;
using GCCWebApp.Api.Tests.TestUtilities;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

namespace GCCWebApp.Api.Tests.Unit.Services
{
    /// <summary>
    /// Tests for CertificateExpirationService — QC #23 (auto-expire certificates)
    /// Verifies that certificates past their expiration date are automatically transitioned
    /// to "Expired" status with proper audit trail entries.
    /// </summary>
    public class QC_CertificateExpirationServiceTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly Mock<ILogger<CertificateExpirationService>> _loggerMock;
        private readonly CertificateExpirationService _service;

        public QC_CertificateExpirationServiceTests()
        {
            _context = TestDbContextFactory.CreateInMemoryContext();
            _loggerMock = new Mock<ILogger<CertificateExpirationService>>();
            var productIdServiceMock = new Mock<IProductIdService>();
            _service = new CertificateExpirationService(_context, _loggerMock.Object, productIdServiceMock.Object);

            // Reset the static throttle so each test can run
            ResetThrottle();
        }

        public void Dispose()
        {
            _context.Dispose();
        }

        /// <summary>
        /// Resets the static _lastRunUtc field to DateTime.MinValue so the throttle doesn't block tests.
        /// </summary>
        private static void ResetThrottle()
        {
            typeof(CertificateExpirationService)
                .GetField("_lastRunUtc", BindingFlags.NonPublic | BindingFlags.Static)!
                .SetValue(null, DateTime.MinValue);
        }

        private Certificate CreateCert(int id, string number, string status, DateTime expirationDate)
        {
            return new Certificate
            {
                Id = id,
                Number = number,
                Status = status,
                ExpirationDate = expirationDate,
                CompanyId = 1,
                CreatedAt = DateTime.UtcNow,
                EffectiveDate = DateTime.UtcNow.AddYears(-1)
            };
        }

        private void SeedCompany()
        {
            if (!_context.Companies.Any())
            {
                _context.Companies.Add(new Company { Id = 1, Name = "Test Co", Email = "test@test.com" });
                _context.SaveChanges();
            }
        }

        #region Expiration Logic Tests

        [Fact]
        public async Task CheckAndExpireAsync_CertPastExpiration_UpdatesStatusToExpired()
        {
            // Arrange
            SeedCompany();
            _context.Certificates.Add(CreateCert(100, "26-0001", CertificateStatusHelper.StatusClientApprovedPaid, DateTime.UtcNow.AddDays(-5)));
            await _context.SaveChangesAsync();

            // Act
            await _service.CheckAndExpireAsync();

            // Assert
            var cert = await _context.Certificates.FindAsync(100);
            cert!.Status.Should().Be(CertificateStatusHelper.StatusExpired);
        }

        [Fact]
        public async Task CheckAndExpireAsync_AlreadyExpiredStatus_SkipsCert()
        {
            // Arrange
            SeedCompany();
            _context.Certificates.Add(CreateCert(101, "26-0002", CertificateStatusHelper.StatusExpired, DateTime.UtcNow.AddDays(-5)));
            await _context.SaveChangesAsync();

            // Act
            await _service.CheckAndExpireAsync();

            // Assert — status should remain Expired, no new history records
            var cert = await _context.Certificates.FindAsync(101);
            cert!.Status.Should().Be(CertificateStatusHelper.StatusExpired);
            var historyCount = await _context.CertificateStatusHistory.CountAsync();
            historyCount.Should().Be(0);
        }

        [Fact]
        public async Task CheckAndExpireAsync_ExpiredNoAnnualVerification_SkipsCert()
        {
            // Arrange
            SeedCompany();
            _context.Certificates.Add(CreateCert(102, "26-0003", CertificateStatusHelper.StatusExpiredNoAnnualVerification, DateTime.UtcNow.AddDays(-10)));
            await _context.SaveChangesAsync();

            // Act
            await _service.CheckAndExpireAsync();

            // Assert
            var cert = await _context.Certificates.FindAsync(102);
            cert!.Status.Should().Be(CertificateStatusHelper.StatusExpiredNoAnnualVerification);
            var historyCount = await _context.CertificateStatusHistory.CountAsync();
            historyCount.Should().Be(0);
        }

        [Fact]
        public async Task CheckAndExpireAsync_FutureExpirationDate_SkipsCert()
        {
            // Arrange
            SeedCompany();
            _context.Certificates.Add(CreateCert(103, "26-0004", CertificateStatusHelper.StatusClientApprovedPaid, DateTime.UtcNow.AddDays(30)));
            await _context.SaveChangesAsync();

            // Act
            await _service.CheckAndExpireAsync();

            // Assert
            var cert = await _context.Certificates.FindAsync(103);
            cert!.Status.Should().Be(CertificateStatusHelper.StatusClientApprovedPaid);
        }

        [Fact]
        public async Task CheckAndExpireAsync_NoCertsToExpire_NoChanges()
        {
            // Arrange — all certs have future dates or are already expired
            SeedCompany();
            _context.Certificates.Add(CreateCert(104, "26-0005", CertificateStatusHelper.StatusClientApprovedPaid, DateTime.UtcNow.AddDays(60)));
            _context.Certificates.Add(CreateCert(105, "26-0006", CertificateStatusHelper.StatusExpired, DateTime.UtcNow.AddDays(-10)));
            await _context.SaveChangesAsync();

            // Act
            await _service.CheckAndExpireAsync();

            // Assert
            var historyCount = await _context.CertificateStatusHistory.CountAsync();
            historyCount.Should().Be(0);
            var auditCount = await _context.AuditHistory.CountAsync();
            auditCount.Should().Be(0);
        }

        [Fact]
        public async Task CheckAndExpireAsync_MultipleCerts_ExpiresAll()
        {
            // Arrange
            SeedCompany();
            _context.Certificates.Add(CreateCert(106, "26-0007", CertificateStatusHelper.StatusClientApprovedPaid, DateTime.UtcNow.AddDays(-1)));
            _context.Certificates.Add(CreateCert(107, "26-0008", CertificateStatusHelper.StatusRepublished, DateTime.UtcNow.AddDays(-2)));
            _context.Certificates.Add(CreateCert(108, "26-0009", CertificateStatusHelper.StatusDraft, DateTime.UtcNow.AddDays(-3)));
            await _context.SaveChangesAsync();

            // Act
            await _service.CheckAndExpireAsync();

            // Assert — all 3 should be expired
            var certs = await _context.Certificates.Where(c => c.Id >= 106 && c.Id <= 108).ToListAsync();
            certs.Should().AllSatisfy(c => c.Status.Should().Be(CertificateStatusHelper.StatusExpired));

            var historyCount = await _context.CertificateStatusHistory.CountAsync();
            historyCount.Should().Be(3);

            var auditCount = await _context.AuditHistory.CountAsync();
            auditCount.Should().Be(3);
        }

        [Fact]
        public async Task CheckAndExpireAsync_DraftCertPastExpiration_Expires()
        {
            // Arrange
            SeedCompany();
            _context.Certificates.Add(CreateCert(109, "26-0010", CertificateStatusHelper.StatusDraft, DateTime.UtcNow.AddDays(-1)));
            await _context.SaveChangesAsync();

            // Act
            await _service.CheckAndExpireAsync();

            // Assert
            var cert = await _context.Certificates.FindAsync(109);
            cert!.Status.Should().Be(CertificateStatusHelper.StatusExpired);
        }

        [Fact]
        public async Task CheckAndExpireAsync_HoldCertPastExpiration_Expires()
        {
            // Arrange
            SeedCompany();
            _context.Certificates.Add(CreateCert(110, "26-0011", CertificateStatusHelper.StatusHold, DateTime.UtcNow.AddDays(-1)));
            await _context.SaveChangesAsync();

            // Act
            await _service.CheckAndExpireAsync();

            // Assert
            var cert = await _context.Certificates.FindAsync(110);
            cert!.Status.Should().Be(CertificateStatusHelper.StatusExpired);
        }

        #endregion

        #region Audit Trail Tests

        [Fact]
        public async Task CheckAndExpireAsync_CertPastExpiration_CreatesStatusHistoryRecord()
        {
            // Arrange
            SeedCompany();
            _context.Certificates.Add(CreateCert(111, "26-0012", CertificateStatusHelper.StatusClientApprovedPaid, DateTime.UtcNow.AddDays(-1)));
            await _context.SaveChangesAsync();

            // Act
            await _service.CheckAndExpireAsync();

            // Assert
            var history = await _context.CertificateStatusHistory.FirstOrDefaultAsync();
            history.Should().NotBeNull();
            history!.CertificateId.Should().Be(111);
            history.CertificateNumber.Should().Be("26-0012");
            history.PreviousStatus.Should().Be(CertificateStatusHelper.StatusClientApprovedPaid);
            history.NewStatus.Should().Be(CertificateStatusHelper.StatusExpired);
            history.IsAutomatedTransition.Should().BeTrue();
            history.WorkflowStep.Should().Be("Auto-Expiration");
            history.ChangeSource.Should().Be(AuditChangeSource.Automated);
            history.ChangedByUsername.Should().Be("System");
        }

        [Fact]
        public async Task CheckAndExpireAsync_CertPastExpiration_CreatesAuditHistoryRecord()
        {
            // Arrange
            SeedCompany();
            _context.Certificates.Add(CreateCert(112, "26-0013", CertificateStatusHelper.StatusRepublished, DateTime.UtcNow.AddDays(-1)));
            await _context.SaveChangesAsync();

            // Act
            await _service.CheckAndExpireAsync();

            // Assert
            var audit = await _context.AuditHistory.FirstOrDefaultAsync();
            audit.Should().NotBeNull();
            audit!.EntityType.Should().Be(AuditEntityType.Certificate);
            audit.EntityId.Should().Be("112");
            audit.EntityName.Should().Be("26-0013");
            audit.Action.Should().Be(AuditAction.StatusChange);
            audit.FieldName.Should().Be("Status");
            audit.OldValue.Should().Be(CertificateStatusHelper.StatusRepublished);
            audit.NewValue.Should().Be(CertificateStatusHelper.StatusExpired);
            audit.ChangedByUsername.Should().Be("System");
            audit.ChangeSource.Should().Be(AuditChangeSource.Automated);
        }

        [Fact]
        public async Task CheckAndExpireAsync_SetsUpdatedAtOnCertificate()
        {
            // Arrange
            SeedCompany();
            var beforeRun = DateTime.UtcNow;
            _context.Certificates.Add(CreateCert(113, "26-0014", CertificateStatusHelper.StatusClientApprovedPaid, DateTime.UtcNow.AddDays(-1)));
            await _context.SaveChangesAsync();

            // Act
            await _service.CheckAndExpireAsync();

            // Assert
            var cert = await _context.Certificates.FindAsync(113);
            cert!.UpdatedAt.Should().BeOnOrAfter(beforeRun);
        }

        #endregion
    }
}
