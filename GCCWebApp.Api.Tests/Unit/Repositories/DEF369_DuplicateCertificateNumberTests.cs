using Xunit;
using FluentAssertions;
using GCCWebApp.Data;
using GCCWebApp.Models;
using GCCWebApp.Repositories;
using GCCWebApp.Api.Tests.TestUtilities;

namespace GCCWebApp.Api.Tests.Unit.Repositories
{
    /// <summary>
    /// DEF-369 — CR-038 allowed several certificates to share one number. Every public route was
    /// keyed on the number alone, so one certificate was reachable and its siblings silently served
    /// that one's page and PDF. On the test environment that left 175 certificates with no address.
    ///
    /// The fix adds an optional `cid` disambiguator. These tests pin the two properties that make
    /// it safe, because both are easy to "simplify" away later:
    ///
    ///   1. A number+id that MATCH return that exact certificate — not the number's default winner.
    ///   2. A number+id that DO NOT match return null. It must NOT fall back to the number, because
    ///      a silent fallback recreates the original defect: the visitor asked for a specific
    ///      certificate and would be shown a different one.
    ///
    /// The fixture deliberately mirrors the client's real case: certificate 26-0182, four
    /// certificates, one company, four different products and four different PDFs.
    /// </summary>
    public class DEF369_DuplicateCertificateNumberTests : IDisposable
    {
        private const string SharedNumber = "26-0182";
        private const string UniqueNumber = "26-9001";

        private readonly ApplicationDbContext _context;
        private readonly CertificateRepository _repository;

        public DEF369_DuplicateCertificateNumberTests()
        {
            _context = TestDbContextFactory.CreateInMemoryContext();
            _repository = new CertificateRepository(_context);
            SeedTestData();
        }

        public void Dispose() => _context.Dispose();

        private void SeedTestData()
        {
            var company = new Company { Id = 900, Name = "Regupol America", Email = "regupol@test.com" };
            _context.Companies.Add(company);

            // CertificationType is a REQUIRED navigation on Certificate (non-nullable `int`
            // CertificationTypeId + `CertificationType = null!`). The single-view lookup Includes
            // it, and a required Include behaves as an inner join — so a certificate whose
            // CertificationTypeId does not resolve is dropped from the result entirely. Omitting
            // this made every by-number lookup return null and looked exactly like a broken query.
            _context.CertificationTypes.Add(
                new CertificationType { Id = 940, Name = "Recycled Content", IsActive = true });

            // Product.Category is required too, and it is Included via ThenInclude on the
            // single-view chain. Verified by bisecting the Include list: with every other Include
            // present the row is returned, and adding ThenInclude(p => p.Category) alone drops it.
            _context.ProductCategories.Add(
                new ProductCategory { Id = 950, Name = "Flooring", IsActive = true });

            // The product is what actually differs between the four — the certificates share a
            // number but certify different products, which is why serving one for all four is wrong.
            _context.Products.AddRange(
                new Product { Id = 930, Name = "REGUPOL sonus core", CompanyId = 900, CategoryId = 950, CreatedAt = DateTime.UtcNow },
                new Product { Id = 931, Name = "REGUPOL 6510S", CompanyId = 900, CategoryId = 950, CreatedAt = DateTime.UtcNow },
                new Product { Id = 932, Name = "Impacta ProBase", CompanyId = 900, CategoryId = 950, CreatedAt = DateTime.UtcNow },
                new Product { Id = 933, Name = "Thunder Flat", CompanyId = 900, CategoryId = 950, CreatedAt = DateTime.UtcNow },
                new Product { Id = 934, Name = "Solo Product", CompanyId = 900, CategoryId = 950, CreatedAt = DateTime.UtcNow }
            );

            var effective = new DateTime(2026, 5, 1);
            var expiration = new DateTime(2027, 4, 30);

            // Four certificates sharing ONE number — the CR-038 condition.
            // Same effective date on purpose: PickOneByNumber then falls through to Id DESC, so the
            // "winner" is 1633 and the other three are the ones that had no URL.
            _context.Certificates.AddRange(
                NewCert(1630, SharedNumber, 930, 900, effective, expiration, "uploads/certificates/26-0182_a.pdf"),
                NewCert(1631, SharedNumber, 931, 900, effective, expiration, "uploads/certificates/26-0182_b.pdf"),
                NewCert(1632, SharedNumber, 932, 900, effective, expiration, "uploads/certificates/26-0182_c.pdf"),
                NewCert(1633, SharedNumber, 933, 900, effective, expiration, "uploads/certificates/26-0182_d.pdf"),
                // A normal, unique number — the overwhelming majority of the data.
                NewCert(1700, UniqueNumber, 934, 900, effective, expiration, "uploads/certificates/26-9001.pdf")
            );

            _context.SaveChanges();
        }

        private static Certificate NewCert(int id, string number, int productId, int companyId,
                                           DateTime effective, DateTime expiration, string file) =>
            new Certificate
            {
                Id = id,
                Number = number,
                ProductId = productId,
                CompanyId = companyId,
                CertificationTypeId = 940,
                Status = CertificateStatusHelper.StatusClientApprovedPaid,
                EffectiveDate = effective,
                ExpirationDate = expiration,
                CertificateFile = file
            };

        [Theory]
        [InlineData(1630)]
        [InlineData(1631)]
        [InlineData(1632)]
        [InlineData(1633)]
        public async Task NumberPlusId_ReturnsThatExactCertificate_ForEverySibling(int certificateId)
        {
            // Every one of the four must be reachable — including the three that previously were not.
            var result = await _repository.GetCertificateByNumberAndIdAsync(SharedNumber, certificateId);

            result.Should().NotBeNull("certificate {0} shares number {1} and must be addressable", certificateId, SharedNumber);
            result!.Id.Should().Be(certificateId);
            result.Number.Should().Be(SharedNumber);
        }

        [Fact]
        public async Task NumberPlusId_ReturnsTheSiblingsOwnPdf_NotTheWinners()
        {
            // The client's report was "they all are pulling the same PDF". Assert the actual file,
            // not just the id — the id matching while the file does not would still be the bug.
            var impacta = await _repository.GetCertificateByNumberAndIdAsync(SharedNumber, 1632);
            var thunderFlat = await _repository.GetCertificateByNumberAndIdAsync(SharedNumber, 1633);

            impacta!.CertificateFile.Should().Be("uploads/certificates/26-0182_c.pdf");
            thunderFlat!.CertificateFile.Should().Be("uploads/certificates/26-0182_d.pdf");
            impacta.CertificateFile.Should().NotBe(thunderFlat.CertificateFile);
        }

        [Fact]
        public async Task NumberOnly_StillResolvesToASingleDeterministicCertificate()
        {
            // The bare URL must keep working unchanged — shared and printed links depend on it.
            var first = await _repository.GetCertificateByNumberAsync(SharedNumber);
            var second = await _repository.GetCertificateByNumberAsync(SharedNumber);

            first.Should().NotBeNull();
            first!.Id.Should().Be(1633, "PickOneByNumber orders by EffectiveDate then Id descending");
            second!.Id.Should().Be(first.Id, "the choice must be stable, not arbitrary per call");
        }

        [Fact]
        public async Task MismatchedId_ReturnsNull_AndDoesNotFallBackToTheNumber()
        {
            // THE important one. A wrong/stale cid must 404, never silently serve the number's
            // default certificate — that is precisely the wrong-certificate defect being fixed.
            var wrongPairing = await _repository.GetCertificateByNumberAndIdAsync(SharedNumber, 1700);
            wrongPairing.Should().BeNull("1700 exists, but not under number {0}", SharedNumber);

            var notACertificate = await _repository.GetCertificateByNumberAndIdAsync(SharedNumber, 999999);
            notACertificate.Should().BeNull();
        }

        [Fact]
        public async Task UniqueNumber_IsUnaffectedByTheDisambiguator()
        {
            var withCid = await _repository.GetCertificateByNumberAndIdAsync(UniqueNumber, 1700);
            var withoutCid = await _repository.GetCertificateByNumberAsync(UniqueNumber);

            withCid.Should().NotBeNull();
            withoutCid.Should().NotBeNull();
            withCid!.Id.Should().Be(withoutCid!.Id, "for a unique number both forms must agree");
        }

        [Fact]
        public async Task CountByNumber_DistinguishesSharedFromUnique()
        {
            (await _repository.CountCertificatesByNumberAsync(SharedNumber)).Should().Be(4);
            (await _repository.CountCertificatesByNumberAsync(UniqueNumber)).Should().Be(1);
            (await _repository.CountCertificatesByNumberAsync("99-9999")).Should().Be(0);
            (await _repository.CountCertificatesByNumberAsync("")).Should().Be(0);
        }
    }
}
