using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using FluentAssertions;
using GCCWebApp.Data;
using GCCWebApp.Models;
using GCCWebApp.Services;
using GCCWebApp.Api.Tests.TestUtilities;
using Microsoft.EntityFrameworkCore;

namespace GCCWebApp.Api.Tests.Unit.Services
{
    /// <summary>
    /// Tests for ProductIdService.CheckDuplicateAsync — QC #99 (duplicate check scoped to certificate)
    /// Verifies that the same product ID on different certificates is allowed when scoped,
    /// and that duplicates within the same certificate are still blocked.
    /// </summary>
    public class QC_ProductIdServiceScopeTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly Mock<ILogger<ProductIdService>> _loggerMock;
        private readonly ProductIdService _service;

        public QC_ProductIdServiceScopeTests()
        {
            _context = TestDbContextFactory.CreateInMemoryContext();
            _loggerMock = new Mock<ILogger<ProductIdService>>();
            _service = new ProductIdService(_context, _loggerMock.Object,
                new Mock<GCCWebApp.Services.Audit.IAuditService>().Object);

            SeedTestData();
        }

        public void Dispose()
        {
            _context.Dispose();
        }

        private void SeedTestData()
        {
            var company = new Company { Id = 1, Name = "Test Co", Email = "test@test.com" };
            _context.Companies.Add(company);

            // Certificate A (id=100) has UPC "012345678901"
            // Certificate B (id=101) has the same UPC "012345678901"
            _context.Certificates.Add(new Certificate { Id = 100, Number = "26-0001", Status = "Draft", CompanyId = 1, CreatedAt = DateTime.UtcNow });
            _context.Certificates.Add(new Certificate { Id = 101, Number = "26-0002", Status = "Draft", CompanyId = 1, CreatedAt = DateTime.UtcNow });

            _context.CertificateProductIds.Add(new CertificateProductId
            {
                Id = 1, CertificateId = 100, ProductIdType = "UPC", ProductIdValue = "012345678901", Status = "Active"
            });
            _context.CertificateProductIds.Add(new CertificateProductId
            {
                Id = 2, CertificateId = 101, ProductIdType = "UPC", ProductIdValue = "012345678901", Status = "Active"
            });
            _context.CertificateProductIds.Add(new CertificateProductId
            {
                Id = 3, CertificateId = 100, ProductIdType = "ASIN", ProductIdValue = "B08XQR1234", Status = "Active"
            });

            _context.SaveChanges();
        }

        #region Certificate-Scoped Duplicate Check (QC #99)

        [Fact]
        public async Task CheckDuplicateAsync_SameValueSameCert_ReturnsTrue()
        {
            // UPC "012345678901" exists on cert 100 — checking for same value on cert 100 = duplicate
            var result = await _service.CheckDuplicateAsync("UPC", "012345678901", certificateId: 100);
            result.Should().BeTrue();
        }

        [Fact]
        public async Task CheckDuplicateAsync_SameValueDifferentCert_WithScope_ReturnsFalse()
        {
            // UPC "012345678901" exists on cert 101, but we scope to cert 100
            // This should NOT find the cert 101 entry — only cert 100
            // Since cert 100 also has it, this is true. Let's test a value that only cert 101 has.

            // Add a unique value to cert 101 only
            _context.CertificateProductIds.Add(new CertificateProductId
            {
                Id = 10, CertificateId = 101, ProductIdType = "GTIN", ProductIdValue = "99887766554433", Status = "Active"
            });
            await _context.SaveChangesAsync();

            // Check if GTIN "99887766554433" is a duplicate on cert 100 — should be false (it's on 101 only)
            var result = await _service.CheckDuplicateAsync("GTIN", "99887766554433", certificateId: 100);
            result.Should().BeFalse();
        }

        [Fact]
        public async Task CheckDuplicateAsync_SameValueAcrossCerts_WithoutScope_ReturnsTrue()
        {
            // Without certificateId scope, "012345678901" UPC exists globally = duplicate
            var result = await _service.CheckDuplicateAsync("UPC", "012345678901");
            result.Should().BeTrue();
        }

        [Fact]
        public async Task CheckDuplicateAsync_SameValueSameCert_ExcludingSelf_ReturnsFalse()
        {
            // UPC "012345678901" on cert 100 has Id=1. Excluding Id=1 and scoping to cert 100 = no duplicate
            var result = await _service.CheckDuplicateAsync("UPC", "012345678901", excludeId: 1, certificateId: 100);
            result.Should().BeFalse();
        }

        [Fact]
        public async Task CheckDuplicateAsync_NewValue_WithCertScope_ReturnsFalse()
        {
            // A completely new value should not be a duplicate on any cert
            var result = await _service.CheckDuplicateAsync("UPC", "999999999999", certificateId: 100);
            result.Should().BeFalse();
        }

        [Fact]
        public async Task CheckDuplicateAsync_SameValueSameCert_ExcludingOther_ReturnsTrue()
        {
            // ASIN "B08XQR1234" on cert 100 has Id=3. Excluding Id=999 (non-existent) on cert 100 = still duplicate
            var result = await _service.CheckDuplicateAsync("ASIN", "B08XQR1234", excludeId: 999, certificateId: 100);
            result.Should().BeTrue();
        }

        #endregion
    }
}
