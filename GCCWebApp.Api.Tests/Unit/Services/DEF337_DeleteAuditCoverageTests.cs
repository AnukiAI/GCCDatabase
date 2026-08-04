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

namespace GCCWebApp.Api.Tests.Unit.Services
{
    /// <summary>
    /// DEF-337 — delete-audit coverage.
    ///
    /// Background: several delete paths wrote no audit row at all. The worst was
    /// CertificateService.DeleteCertificateAsync — the system's core entity, deleted with no
    /// snapshot, taking seven ON DELETE CASCADE child tables with it. On 2026-08-04 this meant
    /// the audit log could not answer "was this record deleted?"; only row counts could.
    ///
    /// These tests assert the CONTRACT (a delete must leave a restorable record), not the
    /// implementation. A future entity whose delete path forgets to audit should fail here.
    /// </summary>
    public class DEF337_DeleteAuditCoverageTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly Mock<IAuditService> _auditMock;

        public DEF337_DeleteAuditCoverageTests()
        {
            _context = TestDbContextFactory.CreateInMemoryContext();
            _auditMock = new Mock<IAuditService>();
            SeedTestData();
        }

        public void Dispose() => _context.Dispose();

        private void SeedTestData()
        {
            _context.Companies.Add(new Company { Id = 500, Name = "Audit Co", Email = "a@audit.test" });
            _context.ProductCategories.Add(new ProductCategory
            {
                Id = 500, Name = "Audit Category", Description = "for delete-audit tests", IsActive = true
            });
            // Category 501 deliberately has NO child types. DeleteProductCategoryAsync refuses
            // while children exist (correctly), so the delete-audit assertion needs a clean one.
            _context.ProductCategories.Add(new ProductCategory
            {
                Id = 501, Name = "Empty Audit Category", Description = "no children", IsActive = true
            });
            _context.ProductTypes.Add(new ProductType
            {
                Id = 500, Name = "Audit Type", CategoryId = 500, Level = 0, IsActive = true
            });
            _context.CertificationTypes.Add(new CertificationType { Id = 500, Name = "Audit Cert Type", IsActive = true });
            _context.Tags.Add(new Tag { Id = 500, Name = "Audit Tag", TagType = "Internal", IsActive = true });

            _context.Certificates.Add(new Certificate
            {
                Id = 500,
                Number = "26-9500",
                CompanyId = 500,
                CertificationTypeId = 500,
                Status = "Client Approved - Paid",
                EffectiveDate = DateTime.UtcNow.AddDays(-30),
                ExpirationDate = DateTime.UtcNow.AddDays(300)
            });

            _context.SaveChanges();
        }

        private CertificateService CreateCertificateService() =>
            new CertificateService(
                new Mock<ILogger<CertificateService>>().Object,
                _context,
                new Mock<IProductIdService>().Object,
                _auditMock.Object);

        // ── The headline gap: certificates were deleted with no audit whatsoever ──────────

        [Fact]
        public async Task DeleteCertificate_WritesDeleteAudit_WithCertificateNumberAsEntityName()
        {
            var service = CreateCertificateService();

            var result = await service.DeleteCertificateAsync(500, "Admin", userId: 1, username: "admin");

            result.Should().BeTrue();
            _auditMock.Verify(a => a.LogEntityDeleteWithChildrenAsync(
                    "500",
                    "26-9500",                       // entityName must identify the cert to a human
                    It.IsAny<Certificate>(),
                    It.IsAny<IReadOnlyList<AuditChildSnapshot>>(),
                    1,
                    "admin",
                    It.IsAny<string>(),
                    It.IsAny<string>()),
                Times.Once,
                "deleting a certificate must leave a restorable audit record — this is the gap DEF-337 was raised for");
        }

        [Fact]
        public async Task DeleteCertificate_CapturesCascadeChildren_SoTheyAreNotSilentlyDestroyed()
        {
            // Every FK pointing at Certificates is ON DELETE CASCADE. These rows vanish at the
            // database layer, so unless they are captured first there is no record they existed.
            _context.CertificateProductIds.Add(new CertificateProductId
            {
                Id = 5001, CertificateId = 500, ProductIdType = "ASIN", ProductIdValue = "B00AUDIT01", Status = "Active"
            });
            _context.CertificateProductIds.Add(new CertificateProductId
            {
                Id = 5002, CertificateId = 500, ProductIdType = "ASIN", ProductIdValue = "B00AUDIT02", Status = "Active"
            });
            await _context.SaveChangesAsync();

            IReadOnlyList<AuditChildSnapshot>? captured = null;
            _auditMock.Setup(a => a.LogEntityDeleteWithChildrenAsync(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Certificate>(),
                    It.IsAny<IReadOnlyList<AuditChildSnapshot>>(),
                    It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Callback<string, string?, Certificate, IReadOnlyList<AuditChildSnapshot>, int, string, string?, string?>(
                    (_, _, _, children, _, _, _, _) => captured = children)
                .ReturnsAsync(1);

            await CreateCertificateService().DeleteCertificateAsync(500, "Admin", 1, "admin");

            captured.Should().NotBeNull();
            var productIdChildren = captured!.Where(c => c.EntityType == nameof(CertificateProductId)).ToList();
            productIdChildren.Should().HaveCount(2,
                "both cascade-deleted product IDs must be captured before the parent is removed");
            productIdChildren.Select(c => c.EntityName)
                .Should().BeEquivalentTo(new[] { "B00AUDIT01", "B00AUDIT02" });
            productIdChildren.Should().OnlyContain(c => c.Entity != null,
                "a snapshot with no entity cannot be restored from");
        }

        [Fact]
        public async Task DeleteCertificate_WhenCertificateMissing_WritesNoAudit()
        {
            var result = await CreateCertificateService().DeleteCertificateAsync(999999, "Admin", 1, "admin");

            result.Should().BeFalse();
            _auditMock.Verify(a => a.LogEntityDeleteWithChildrenAsync(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Certificate>(),
                    It.IsAny<IReadOnlyList<AuditChildSnapshot>>(),
                    It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()),
                Times.Never,
                "a no-op delete must not fabricate an audit trail");
        }

        // ── Taxonomy deletes: previously unaudited, which is why row counts were the only
        //    way to answer 'was this deleted?' during the 2026-08-04 investigation ──────────

        [Fact]
        public async Task DeleteProductCategory_WritesDeleteAudit()
        {
            var admin = CreateAdminService();

            var (success, _) = await admin.DeleteProductCategoryAsync(501, 1, "admin");

            success.Should().BeTrue();
            _auditMock.Verify(a => a.LogEntityDeleteWithChildrenAsync(
                    "501", "Empty Audit Category", It.IsAny<ProductCategory>(),
                    It.IsAny<IReadOnlyList<AuditChildSnapshot>>(),
                    1, "admin", It.IsAny<string>(), It.IsAny<string>()),
                Times.Once);
        }

        [Fact]
        public async Task DeleteProductType_WritesDeleteAudit()
        {
            var admin = CreateAdminService();

            var (success, _) = await admin.DeleteProductTypeAsync(500, 1, "admin");

            success.Should().BeTrue();
            _auditMock.Verify(a => a.LogEntityDeleteWithChildrenAsync(
                    "500", "Audit Type", It.IsAny<ProductType>(),
                    It.IsAny<IReadOnlyList<AuditChildSnapshot>>(),
                    1, "admin", It.IsAny<string>(), It.IsAny<string>()),
                Times.Once);
        }

        [Fact]
        public async Task DeleteTag_CapturesItsCertificateAndProductAssociations()
        {
            // A tag delete is NOT guarded against existing links: the join rows cascade away,
            // losing which certificates carried the tag. Capturing them is the only record.
            var tag = await _context.Tags.Include(t => t.Certificates).FirstAsync(t => t.Id == 500);
            var cert = await _context.Certificates.FirstAsync(c => c.Id == 500);
            tag.Certificates.Add(cert);
            await _context.SaveChangesAsync();

            IReadOnlyList<AuditChildSnapshot>? captured = null;
            _auditMock.Setup(a => a.LogEntityDeleteWithChildrenAsync(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Tag>(),
                    It.IsAny<IReadOnlyList<AuditChildSnapshot>>(),
                    It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Callback<string, string?, Tag, IReadOnlyList<AuditChildSnapshot>, int, string, string?, string?>(
                    (_, _, _, children, _, _, _, _) => captured = children)
                .ReturnsAsync(1);

            var success = await CreateAdminService().DeleteTagAsync(500, 1, "admin");

            success.Should().BeTrue();
            captured.Should().NotBeNull();
            captured!.Should().ContainSingle(c => c.EntityType == "CertificateTag",
                "the tag→certificate association is destroyed by cascade and must be recorded");
            captured!.Single(c => c.EntityType == "CertificateTag").EntityName.Should().Be("26-9500");
        }

        // ── DEF-337 completion: the gaps left open by the first pass ──────────────────────
        //
        // The original fix captured FK children (rows with a real CertificateId column). It did
        // not capture JOIN TABLES, and several delete paths still wrote nothing at all. These
        // tests pin the remaining contract.

        /// <summary>
        /// Captures the certificate→partner link rows. Partner membership decides who can see a
        /// certificate, so a restore that loses it silently changes visibility rather than
        /// failing loudly — the worst kind of data loss.
        /// </summary>
        [Fact]
        public async Task DeleteCertificate_CapturesPartnerAndTagLinkRows_NotJustFkChildren()
        {
            _context.Partners.Add(new Partner { Id = 500, Name = "Audit Partner", IsActive = true });
            await _context.SaveChangesAsync();

            var cert = await _context.Certificates
                .Include(c => c.Partners)
                .Include(c => c.Tags)
                .FirstAsync(c => c.Id == 500);
            cert.Partners.Add(await _context.Partners.FirstAsync(p => p.Id == 500));
            cert.Tags.Add(await _context.Tags.FirstAsync(t => t.Id == 500));
            await _context.SaveChangesAsync();

            var captured = await CaptureCertificateDeleteChildren(500);

            captured.Should().NotBeNull();

            var partnerLinks = LinkRows(captured).Where(r => r.RightEntity == nameof(Partner)).ToList();
            partnerLinks.Should().ContainSingle(
                "the certificate→partner association cascades away and governs who can see this " +
                "certificate; restoring without it silently changes visibility");
            partnerLinks.Single().RightId.Should().Be("500");

            LinkRows(captured).Should().Contain(r => r.RightEntity == nameof(Tag),
                "the certificate→tag association cascades away too");
        }

        /// <summary>
        /// The link row has to carry enough to write the restore INSERT. Entity names and ids
        /// alone are not enough — you need the physical table and both column names.
        /// </summary>
        [Fact]
        public async Task CapturedLinkRow_CarriesJoinTableAndColumnNames_SoARestoreCanBeWritten()
        {
            _context.Partners.Add(new Partner { Id = 501, Name = "Mapping Partner", IsActive = true });
            await _context.SaveChangesAsync();

            var cert = await _context.Certificates.Include(c => c.Partners).FirstAsync(c => c.Id == 500);
            cert.Partners.Add(await _context.Partners.FirstAsync(p => p.Id == 501));
            await _context.SaveChangesAsync();

            var captured = await CaptureCertificateDeleteChildren(500);

            var link = captured!
                .Select(c => c.Entity)
                .OfType<AuditLinkRow>()
                .Single(r => r.RightEntity == nameof(Partner));

            link.JoinTable.Should().Be("CertificatePartners");
            link.LeftEntity.Should().Be(nameof(Certificate));
            link.LeftId.Should().Be("500");
            link.RightId.Should().Be("501");
            link.LeftColumn.Should().NotBeNullOrWhiteSpace(
                "without the column names the audit row does not tell you how to re-insert the link");
            link.RightColumn.Should().NotBeNullOrWhiteSpace();
        }

        /// <summary>
        /// Bulk delete used to RemoveRange the whole set with no audit — the same core-entity
        /// hole DEF-337 closed for single deletes, still open on the bulk path.
        /// </summary>
        [Fact]
        public async Task BulkDelete_AuditsEveryCertificate_NotJustTheFirst()
        {
            _context.Certificates.Add(new Certificate
            {
                Id = 501,
                Number = "26-9501",
                CompanyId = 500,
                CertificationTypeId = 500,
                Status = "Client Approved - Paid",
                EffectiveDate = DateTime.UtcNow.AddDays(-30),
                ExpirationDate = DateTime.UtcNow.AddDays(300)
            });
            await _context.SaveChangesAsync();

            var result = await CreateCertificateService().PerformBulkActionAsync(new BulkActionRequest
            {
                Action = "delete",
                UserRole = "Admin",
                CertificateIds = new List<int> { 500, 501 }
            });

            result.AffectedCount.Should().Be(2);
            _auditMock.Verify(a => a.LogEntityDeleteWithChildrenAsync(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Certificate>(),
                    It.IsAny<IReadOnlyList<AuditChildSnapshot>>(),
                    It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()),
                Times.Exactly(2),
                "a bulk delete must not be a way to destroy certificates without an audit trail");
        }

        /// <summary>
        /// Deleting a custom field definition cascades its recorded values — real certificate
        /// data, not configuration.
        /// </summary>
        [Fact]
        public async Task DeleteCertificateField_CapturesTheValuesCertificatesHadRecorded()
        {
            _context.CustomFields.Add(new CustomField { Id = 500, Name = "Audit Field", FieldType = "Text" });
            await _context.SaveChangesAsync();
            _context.CustomFieldValues.Add(new CustomFieldValue
            {
                Id = 5001, CertificateId = 500, FieldId = 500, TextValue = "recorded-result"
            });
            await _context.SaveChangesAsync();

            IReadOnlyList<AuditChildSnapshot>? captured = null;
            _auditMock.Setup(a => a.LogEntityDeleteWithChildrenAsync(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CustomField>(),
                    It.IsAny<IReadOnlyList<AuditChildSnapshot>>(),
                    It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Callback<string, string?, CustomField, IReadOnlyList<AuditChildSnapshot>, int, string, string?, string?>(
                    (_, _, _, children, _, _, _, _) => captured = children)
                .ReturnsAsync(1);

            var success = await CreateAdminService().DeleteCertificateFieldAsync(500, 1, "admin");

            success.Should().BeTrue();
            captured.Should().NotBeNull();
            captured!.Should().ContainSingle(c => c.EntityType == nameof(CustomFieldValue),
                "the value a certificate recorded for this field is destroyed by cascade and is " +
                "certificate data, not configuration");
        }

        [Fact]
        public async Task DeleteRole_CapturesWhoHeldItAndWhatItGranted()
        {
            _context.Roles.Add(new Role { Id = 500, Name = "Audit Role", IsSystem = false });
            await _context.SaveChangesAsync();

            var role = await _context.Roles.Include(r => r.Users).FirstAsync(r => r.Id == 500);
            var user = new User
            {
                Id = 500, Email = "audit.user@anukiconsulting.com", Username = "audituser",
                PasswordHash = "x", Status = "active", TwoFactorMethod = "Email"
            };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            role.Users.Add(user);
            await _context.SaveChangesAsync();

            IReadOnlyList<AuditChildSnapshot>? captured = null;
            _auditMock.Setup(a => a.LogEntityDeleteWithChildrenAsync(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Role>(),
                    It.IsAny<IReadOnlyList<AuditChildSnapshot>>(),
                    It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Callback<string, string?, Role, IReadOnlyList<AuditChildSnapshot>, int, string, string?, string?>(
                    (_, _, _, children, _, _, _, _) => captured = children)
                .ReturnsAsync(1);

            var success = await CreateAdminService().DeleteRoleAsync(500, 1, "admin");

            success.Should().BeTrue();
            captured.Should().NotBeNull();
            LinkRows(captured).Should().Contain(r => r.RightEntity == nameof(User),
                "deleting a role erases which users held it — the fact you need to answer " +
                "'why did this user lose access?'");
        }

        /// <summary>
        /// Shared setup for the certificate-delete capture tests: runs the delete against a
        /// mocked audit service and hands back the children it was given.
        /// </summary>
        private async Task<IReadOnlyList<AuditChildSnapshot>?> CaptureCertificateDeleteChildren(int certificateId)
        {
            IReadOnlyList<AuditChildSnapshot>? captured = null;
            _auditMock.Setup(a => a.LogEntityDeleteWithChildrenAsync(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Certificate>(),
                    It.IsAny<IReadOnlyList<AuditChildSnapshot>>(),
                    It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Callback<string, string?, Certificate, IReadOnlyList<AuditChildSnapshot>, int, string, string?, string?>(
                    (_, _, _, children, _, _, _, _) => captured = children)
                .ReturnsAsync(1);

            await CreateCertificateService().DeleteCertificateAsync(certificateId, "Admin", 1, "admin");
            return captured;
        }

        /// <summary>Link rows out of a captured child set (FluentAssertions predicates cannot
        /// use pattern matching — they compile to expression trees).</summary>
        private static List<AuditLinkRow> LinkRows(IReadOnlyList<AuditChildSnapshot>? captured) =>
            (captured ?? Array.Empty<AuditChildSnapshot>())
                .Select(c => c.Entity)
                .OfType<AuditLinkRow>()
                .ToList();

        private AdminService CreateAdminService() =>
            new AdminService(_context, new Mock<ILogger<AdminService>>().Object, _auditMock.Object);
    }
}
