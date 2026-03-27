using Xunit;
using FluentAssertions;
using GCCWebApp.Data;
using GCCWebApp.Models;
using GCCWebApp.Repositories;
using GCCWebApp.Api.Tests.TestUtilities;
using Microsoft.EntityFrameworkCore;

namespace GCCWebApp.Api.Tests.Unit.Repositories
{
    /// <summary>
    /// Tests for CertificateRepository — QC #22 (status filtering), #25/#26 (sorting), #67/#68/#70 (dashboard stats)
    /// Verifies that the public view only shows approved/republished certs, that sorting works correctly,
    /// and that dashboard statistics count only active certificates.
    /// </summary>
    public class QC_CertificateRepositoryTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly CertificateRepository _repository;

        public QC_CertificateRepositoryTests()
        {
            _context = TestDbContextFactory.CreateInMemoryContext();
            _repository = new CertificateRepository(_context);

            SeedTestData();
        }

        public void Dispose()
        {
            _context.Dispose();
        }

        private void SeedTestData()
        {
            // Companies
            var company1 = new Company { Id = 10, Name = "Acme Corp", Email = "acme@test.com" };
            var company2 = new Company { Id = 11, Name = "Beta Inc", Email = "beta@test.com" };
            var company3 = new Company { Id = 12, Name = "Charlie LLC", Email = "charlie@test.com" };
            _context.Companies.AddRange(company1, company2, company3);

            // Certification Types
            var certType1 = new CertificationType { Id = 10, Name = "Recycled Content", IsActive = true };
            var certType2 = new CertificationType { Id = 11, Name = "Energy Savings", IsActive = true };
            _context.CertificationTypes.AddRange(certType1, certType2);

            // Products
            var product1 = new Product { Id = 10, Name = "Paper Towels", CompanyId = 10, CreatedAt = DateTime.UtcNow };
            var product2 = new Product { Id = 11, Name = "Cleaning Spray", CompanyId = 11, CreatedAt = DateTime.UtcNow };
            _context.Products.AddRange(product1, product2);

            // Certificates with different statuses and expiration dates
            _context.Certificates.AddRange(
                // Active/public: Client Approved - Paid, future expiry
                new Certificate { Id = 200, Number = "26-0100", Status = CertificateStatusHelper.StatusClientApprovedPaid,
                    CompanyId = 10, ProductId = 10, CertificationTypeId = 10,
                    EffectiveDate = DateTime.UtcNow.AddDays(-30), ExpirationDate = DateTime.UtcNow.AddDays(335),
                    Location = "Philadelphia", Country = "USA", CreatedAt = DateTime.UtcNow },

                // Active/public: Republished, future expiry
                new Certificate { Id = 201, Number = "26-0101", Status = CertificateStatusHelper.StatusRepublished,
                    CompanyId = 11, ProductId = 11, CertificationTypeId = 11,
                    EffectiveDate = DateTime.UtcNow.AddDays(-60), ExpirationDate = DateTime.UtcNow.AddDays(305),
                    Location = "Chicago", Country = "USA", CreatedAt = DateTime.UtcNow },

                // NOT public: Draft
                new Certificate { Id = 202, Number = "26-0102", Status = CertificateStatusHelper.StatusDraft,
                    CompanyId = 10, ProductId = 10, CertificationTypeId = 10,
                    EffectiveDate = DateTime.UtcNow.AddDays(-10), ExpirationDate = DateTime.UtcNow.AddDays(355),
                    Location = "Denver", Country = "Canada", CreatedAt = DateTime.UtcNow },

                // NOT public: Hold
                new Certificate { Id = 203, Number = "26-0103", Status = CertificateStatusHelper.StatusHold,
                    CompanyId = 12, ProductId = 10, CertificationTypeId = 10,
                    EffectiveDate = DateTime.UtcNow.AddDays(-5), ExpirationDate = DateTime.UtcNow.AddDays(360),
                    Location = "Austin", Country = "Mexico", CreatedAt = DateTime.UtcNow },

                // NOT public: Expired status
                new Certificate { Id = 204, Number = "26-0104", Status = CertificateStatusHelper.StatusExpired,
                    CompanyId = 10, ProductId = 10, CertificationTypeId = 10,
                    EffectiveDate = DateTime.UtcNow.AddYears(-2), ExpirationDate = DateTime.UtcNow.AddDays(-30),
                    Location = "Portland", Country = "USA", CreatedAt = DateTime.UtcNow },

                // Public status but PAST expiration
                new Certificate { Id = 205, Number = "26-0105", Status = CertificateStatusHelper.StatusClientApprovedPaid,
                    CompanyId = 11, ProductId = 11, CertificationTypeId = 11,
                    EffectiveDate = DateTime.UtcNow.AddYears(-2), ExpirationDate = DateTime.UtcNow.AddDays(-5),
                    Location = "Seattle", Country = "UK", CreatedAt = DateTime.UtcNow },

                // NOT public: Confidential
                new Certificate { Id = 206, Number = "26-0106", Status = CertificateStatusHelper.StatusClientApprovedConfidential,
                    CompanyId = 12, ProductId = 10, CertificationTypeId = 10,
                    EffectiveDate = DateTime.UtcNow.AddDays(-15), ExpirationDate = DateTime.UtcNow.AddDays(350),
                    Location = "Tokyo", Country = "Japan", CreatedAt = DateTime.UtcNow },

                // NOT public: Expired - No Annual Verification
                new Certificate { Id = 207, Number = "26-0107", Status = CertificateStatusHelper.StatusExpiredNoAnnualVerification,
                    CompanyId = 10, ProductId = 10, CertificationTypeId = 10,
                    EffectiveDate = DateTime.UtcNow.AddYears(-3), ExpirationDate = DateTime.UtcNow.AddDays(-90),
                    Location = "Berlin", Country = "Germany", CreatedAt = DateTime.UtcNow }
            );

            _context.SaveChanges();
        }

        #region GetActiveCertificatesAsync — Public View Filtering (QC #22)

        [Fact]
        public async Task GetActiveCertificatesAsync_ReturnsOnlyPublicStatuses()
        {
            var result = (await _repository.GetActiveCertificatesAsync()).ToList();

            // Only IDs 200 (Approved-Paid, future) and 201 (Republished, future) should be returned
            result.Should().HaveCount(2);
            result.Select(c => c.Id).Should().BeEquivalentTo(new[] { 200, 201 });
        }

        [Fact]
        public async Task GetActiveCertificatesAsync_ExcludesExpiredByDate()
        {
            // ID 205 has public status but past expiration — should NOT appear
            var result = (await _repository.GetActiveCertificatesAsync()).ToList();
            result.Select(c => c.Id).Should().NotContain(205);
        }

        [Fact]
        public async Task GetActiveCertificatesAsync_ExcludesDraftStatus()
        {
            var result = (await _repository.GetActiveCertificatesAsync()).ToList();
            result.Select(c => c.Id).Should().NotContain(202);
        }

        [Fact]
        public async Task GetActiveCertificatesAsync_ExcludesHoldStatus()
        {
            var result = (await _repository.GetActiveCertificatesAsync()).ToList();
            result.Select(c => c.Id).Should().NotContain(203);
        }

        [Fact]
        public async Task GetActiveCertificatesAsync_ExcludesConfidentialStatus()
        {
            var result = (await _repository.GetActiveCertificatesAsync()).ToList();
            result.Select(c => c.Id).Should().NotContain(206);
        }

        [Fact]
        public async Task GetActiveCertificatesAsync_IncludesRepublishedWithFutureExpiry()
        {
            var result = (await _repository.GetActiveCertificatesAsync()).ToList();
            result.Select(c => c.Id).Should().Contain(201);
        }

        #endregion

        #region GetCertificateStatisticsAsync — Dashboard Stats (QC #67, #68, #70)

        [Fact]
        public async Task GetCertificateStatisticsAsync_TotalCountsAllCerts()
        {
            var stats = await _repository.GetCertificateStatisticsAsync();
            stats["Total"].Should().Be(8); // All 8 seeded certificates
        }

        [Fact]
        public async Task GetCertificateStatisticsAsync_ActiveCountFiltersByStatusAndDate()
        {
            var stats = await _repository.GetCertificateStatisticsAsync();
            // Only 200 and 201 are active (public status + not expired by date)
            stats["Active"].Should().Be(2);
        }

        [Fact]
        public async Task GetCertificateStatisticsAsync_ExpiredCountsByStatusOnly()
        {
            var stats = await _repository.GetCertificateStatisticsAsync();
            // IDs 204 (Expired) and 207 (Expired - No Annual Verification) = 2
            // ID 205 has past expiration date but status "Client Approved - Paid" — should NOT be counted as Expired
            stats["Expired"].Should().Be(2);
        }

        [Fact]
        public async Task GetCertificateStatisticsAsync_ActiveVsTotalDiffer()
        {
            var stats = await _repository.GetCertificateStatisticsAsync();
            stats["Active"].Should().BeLessThan(stats["Total"]);
        }

        [Fact]
        public async Task GetCertificateStatisticsAsync_CompaniesWithActiveCerts_DistinctCount()
        {
            var stats = await _repository.GetCertificateStatisticsAsync();
            // Active certs: 200 (company 10) and 201 (company 11) = 2 distinct companies
            stats["CompaniesWithActiveCerts"].Should().Be(2);
        }

        [Fact]
        public async Task GetCertificateStatisticsAsync_CertTypesWithActiveCerts_DistinctCount()
        {
            var stats = await _repository.GetCertificateStatisticsAsync();
            // Active certs: 200 (type 10) and 201 (type 11) = 2 distinct cert types
            stats["CertTypesWithActiveCerts"].Should().Be(2);
        }

        [Fact]
        public async Task GetCertificateStatisticsAsync_StatusBreakdownHasAllStatuses()
        {
            var stats = await _repository.GetCertificateStatisticsAsync();
            // Should have Status_* keys for each status that has at least one cert
            stats.Keys.Should().Contain(k => k.StartsWith("Status_"));
        }

        #endregion

        #region GetAdvancedFilteredCertificatesAsync — Sorting (QC #25, #26)

        [Fact]
        public async Task GetAdvancedFilteredCertificatesAsync_SortByNumber_Ascending()
        {
            var filter = new CertificateSearchFilter { SortBy = "Number", SortDescending = false, PageSize = 50 };
            var (certs, _) = await _repository.GetAdvancedFilteredCertificatesAsync(filter);

            var numbers = certs.Select(c => c.Number).ToList();
            numbers.Should().BeInAscendingOrder();
        }

        [Fact]
        public async Task GetAdvancedFilteredCertificatesAsync_SortByNumber_Descending()
        {
            var filter = new CertificateSearchFilter { SortBy = "Number", SortDescending = true, PageSize = 50 };
            var (certs, _) = await _repository.GetAdvancedFilteredCertificatesAsync(filter);

            var numbers = certs.Select(c => c.Number).ToList();
            numbers.Should().BeInDescendingOrder();
        }

        [Fact]
        public async Task GetAdvancedFilteredCertificatesAsync_SortByCompany_Ascending()
        {
            var filter = new CertificateSearchFilter { SortBy = "Company", SortDescending = false, PageSize = 50 };
            var (certs, _) = await _repository.GetAdvancedFilteredCertificatesAsync(filter);

            var companyNames = certs.Select(c => c.Company?.Name ?? "").ToList();
            companyNames.Should().BeInAscendingOrder();
        }

        [Fact]
        public async Task GetAdvancedFilteredCertificatesAsync_UnknownSortField_DefaultsToNumber()
        {
            var filterUnknown = new CertificateSearchFilter { SortBy = "foobar", SortDescending = false, PageSize = 50 };
            var (certsUnknown, _) = await _repository.GetAdvancedFilteredCertificatesAsync(filterUnknown);

            var filterNumber = new CertificateSearchFilter { SortBy = "Number", SortDescending = false, PageSize = 50 };
            var (certsNumber, _) = await _repository.GetAdvancedFilteredCertificatesAsync(filterNumber);

            // Unknown sort should produce same order as explicit Number sort
            certsUnknown.Select(c => c.Id).Should().BeEquivalentTo(certsNumber.Select(c => c.Id), o => o.WithStrictOrdering());
        }

        [Fact]
        public async Task GetAdvancedFilteredCertificatesAsync_StatusFilter_ReturnsOnlyMatchingStatus()
        {
            var filter = new CertificateSearchFilter
            {
                Statuses = new List<string> { CertificateStatusHelper.StatusDraft },
                PageSize = 50
            };
            var (certs, totalCount) = await _repository.GetAdvancedFilteredCertificatesAsync(filter);

            certs.Should().AllSatisfy(c => c.Status.Should().Be(CertificateStatusHelper.StatusDraft));
            totalCount.Should().Be(1); // Only ID 202
        }

        [Fact]
        public async Task GetAdvancedFilteredCertificatesAsync_ExcludeExpiredByDate_FiltersCorrectly()
        {
            var filter = new CertificateSearchFilter { ExcludeExpiredByDate = true, PageSize = 50 };
            var (certs, _) = await _repository.GetAdvancedFilteredCertificatesAsync(filter);

            // Certs 204, 205, 207 have past expiration dates — should be excluded
            certs.Select(c => c.Id).Should().NotContain(204);
            certs.Select(c => c.Id).Should().NotContain(205);
            certs.Select(c => c.Id).Should().NotContain(207);
        }

        [Fact]
        public async Task GetAdvancedFilteredCertificatesAsync_ReturnsTotalCount()
        {
            var filter = new CertificateSearchFilter { PageSize = 50 };
            var (_, totalCount) = await _repository.GetAdvancedFilteredCertificatesAsync(filter);
            totalCount.Should().Be(8);
        }

        #endregion

        #region Certification Type Statistics (QC #70)

        [Fact]
        public async Task GetCertificationTypeStatisticsAsync_GroupsByTypeName()
        {
            var stats = (await _repository.GetCertificationTypeStatisticsAsync()).ToList();

            // Should group by cert type name for active/public certs
            stats.Should().NotBeEmpty();
            stats.Should().AllSatisfy(s =>
            {
                s.Name.Should().NotBeNullOrEmpty();
                s.Count.Should().BeGreaterThan(0);
            });
        }

        #endregion
    }
}
