using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;
using FluentAssertions;
using GCCWebApp.Controllers.Api;
using GCCWebApp.Data;
using GCCWebApp.Models;
using System.Security.Claims;
using System.Text.Json;

namespace GCCWebApp.Api.Tests.Unit.Controllers
{
    public class CertificateTileExternalApiControllerTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly Mock<ILogger<CertificateTileExternalApiController>> _loggerMock;
        private readonly CertificateTileExternalApiController _controller;

        // Test entity IDs (assigned by InMemory after SaveChanges)
        private Company _company1 = null!;
        private Company _company2 = null!;
        private Partner _amazonPartner = null!;
        private Partner _noviPartner = null!;

        public CertificateTileExternalApiControllerTests()
        {
            _loggerMock = new Mock<ILogger<CertificateTileExternalApiController>>();

            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            _context = new ApplicationDbContext(options);

            SeedTestData();

            _controller = new CertificateTileExternalApiController(_context, _loggerMock.Object);

            // Default: Admin role (full access)
            SetUserClaims("Admin", "admin@test.com");
        }

        public void Dispose()
        {
            _context?.Dispose();
        }

        #region Helpers

        private void SetUserClaims(string role, string email, string? partner = null)
        {
            var claims = new List<Claim>
            {
                new Claim("Role", role),
                new Claim(ClaimTypes.Email, email)
            };
            if (partner != null)
                claims.Add(new Claim("partner", partner));

            var identity = new ClaimsIdentity(claims, "TestAuth");
            var principal = new ClaimsPrincipal(identity);

            var httpContext = new DefaultHttpContext { User = principal };
            httpContext.Request.Scheme = "https";
            httpContext.Request.Host = new HostString("localhost:5101");

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };
        }

        private void SeedTestData()
        {
            // Companies
            _company1 = new Company { Name = "GreenCorp", Email = "info@green.com", IsActive = true };
            _company2 = new Company { Name = "EcoProducts", Email = "info@eco.com", IsActive = true };
            var company3 = new Company { Name = "OtherCo", Email = "info@other.com", IsActive = true };
            _context.Companies.AddRange(_company1, _company2, company3);
            _context.SaveChanges();

            // Partners
            _amazonPartner = new Partner { Name = "Amazon", Description = "Amazon CPF", IsActive = true };
            _noviPartner = new Partner { Name = "Novi", Description = "Novi Connect", IsActive = true };
            _context.Partners.AddRange(_amazonPartner, _noviPartner);
            _context.SaveChanges();

            // Product Categories
            var category = new ProductCategory { Name = "Paper Products", IsActive = true };
            _context.ProductCategories.Add(category);
            _context.SaveChanges();

            // Certification Types
            var certType1 = new CertificationType { Name = "FSC", Description = "Forest Stewardship", IsActive = true, Level = 1 };
            var certType2 = new CertificationType { Name = "SFI", Description = "Sustainable Forestry", IsActive = true, Level = 1 };
            _context.CertificationTypes.AddRange(certType1, certType2);
            _context.SaveChanges();

            // Products
            var product1 = new Product { Name = "Recycled Paper", CompanyId = _company1.Id, CategoryId = category.Id, CreatedAt = DateTime.UtcNow };
            var product2 = new Product { Name = "Eco-Towels", CompanyId = _company2.Id, CategoryId = category.Id, CreatedAt = DateTime.UtcNow };
            var product3 = new Product { Name = "Other Product", CompanyId = company3.Id, CategoryId = category.Id, CreatedAt = DateTime.UtcNow };
            _context.Products.AddRange(product1, product2, product3);
            _context.SaveChanges();

            // Tags
            var extTag = new Tag { Name = "eco-friendly", TagType = "external", IsActive = true };
            var intTag = new Tag { Name = "internal-review", TagType = "internal", IsActive = true };
            _context.Tags.AddRange(extTag, intTag);
            _context.SaveChanges();

            var now = DateTime.UtcNow;

            // Certificates - various statuses, companies, partners
            var cert1 = new Certificate
            {
                Number = "25-0001", Status = "active", CompanyId = _company1.Id, ProductId = product1.Id,
                CertificationTypeId = certType1.Id, EffectiveDate = now.AddDays(-60),
                ExpirationDate = now.AddDays(300), Location = "Philadelphia", Country = "USA",
                Partner = "Amazon", CreatedAt = now.AddDays(-60),
                Tags = new List<Tag> { extTag }
            };
            var cert2 = new Certificate
            {
                Number = "25-0002", Status = "active", CompanyId = _company2.Id, ProductId = product2.Id,
                CertificationTypeId = certType2.Id, EffectiveDate = now.AddDays(-30),
                ExpirationDate = now.AddDays(200), Location = "Toronto", Country = "Canada",
                Partner = "Novi", CreatedAt = now.AddDays(-30),
                Tags = new List<Tag> { extTag, intTag }
            };
            var cert3 = new Certificate
            {
                Number = "25-0003", Status = "active", CompanyId = _company1.Id, ProductId = product1.Id,
                CertificationTypeId = certType1.Id, EffectiveDate = now.AddDays(-90),
                ExpirationDate = now.AddDays(15), Location = "Denver", Country = "USA",
                Partner = "Amazon", CreatedAt = now.AddDays(-90)
            };
            // Expired cert (should never appear for external users)
            var cert4 = new Certificate
            {
                Number = "25-0004", Status = "expired", CompanyId = _company1.Id, ProductId = product1.Id,
                CertificationTypeId = certType1.Id, EffectiveDate = now.AddDays(-400),
                ExpirationDate = now.AddDays(-10), Location = "Austin", Country = "USA",
                Partner = "Amazon", CreatedAt = now.AddDays(-400)
            };
            // Draft cert (should not appear for external users)
            var cert5 = new Certificate
            {
                Number = "25-0005", Status = "draft", CompanyId = _company1.Id, ProductId = product1.Id,
                CertificationTypeId = certType1.Id, EffectiveDate = now.AddDays(-10),
                ExpirationDate = now.AddDays(350), Location = "Chicago", Country = "USA",
                Partner = "Amazon", CreatedAt = now.AddDays(-10)
            };
            // Cert with "expiring_soon" status (should appear)
            var cert6 = new Certificate
            {
                Number = "25-0006", Status = "expiring_soon", CompanyId = _company2.Id, ProductId = product2.Id,
                CertificationTypeId = certType2.Id, EffectiveDate = now.AddDays(-300),
                ExpirationDate = now.AddDays(10), Location = "Portland", Country = "USA",
                Partner = "Novi", CreatedAt = now.AddDays(-300)
            };
            // Cert with no partner
            var cert7 = new Certificate
            {
                Number = "25-0007", Status = "active", CompanyId = company3.Id, ProductId = product3.Id,
                CertificationTypeId = certType1.Id, EffectiveDate = now.AddDays(-20),
                ExpirationDate = now.AddDays(340), Location = "Austin", Country = "USA",
                Partner = "None", CreatedAt = now.AddDays(-20)
            };

            _context.Certificates.AddRange(cert1, cert2, cert3, cert4, cert5, cert6, cert7);
            _context.SaveChanges();

            // Users with company/partner associations
            var externalUser = new User
            {
                Username = "external1", Email = "external@green.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Test123!"),
                Status = "active", IsActive = true, CreatedAt = now,
                TwoFactorMethod = "Email",
                Companies = new List<Company> { _company1 }
            };
            var partnerUser = new User
            {
                Username = "partner1", Email = "partner@amazon.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Test123!"),
                Status = "active", IsActive = true, CreatedAt = now,
                TwoFactorMethod = "Email",
                Partners = new List<Partner> { _amazonPartner }
            };
            var noCompanyUser = new User
            {
                Username = "nocompany", Email = "nocompany@test.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Test123!"),
                Status = "active", IsActive = true, CreatedAt = now,
                TwoFactorMethod = "Email"
            };

            _context.Users.AddRange(externalUser, partnerUser, noCompanyUser);
            _context.SaveChanges();
        }

        /// <summary>
        /// Extract the certificates array from OkObjectResult response.
        /// </summary>
        private static JsonElement GetResponseData(IActionResult result)
        {
            var okResult = (OkObjectResult)result;
            var json = JsonSerializer.Serialize(okResult.Value);
            return JsonDocument.Parse(json).RootElement;
        }

        private static List<string> GetCertNumbers(JsonElement data)
        {
            return data.GetProperty("certificates")
                .EnumerateArray()
                .Select(c => c.GetProperty("certificateNumber").GetString()!)
                .ToList();
        }

        #endregion

        // ========================================================
        //  ROLE-BASED DATA ISOLATION - GetCertificateTiles
        // ========================================================

        [Fact]
        public async Task GetCertificateTiles_AdminRole_SeesAllActiveCerts()
        {
            SetUserClaims("Admin", "admin@test.com");

            var result = await _controller.GetCertificateTiles();

            var data = GetResponseData(result);
            // Admin sees all active/expiring_soon/renewal_in_progress certs (not expired/draft)
            var totalCount = data.GetProperty("totalCount").GetInt32();
            totalCount.Should().Be(5); // cert1,2,3,6,7 (not cert4=expired, cert5=draft)
        }

        [Fact]
        public async Task GetCertificateTiles_ExternalRole_SeesOnlyAssignedCompanyCerts()
        {
            SetUserClaims("External", "external@green.com");

            var result = await _controller.GetCertificateTiles();

            var data = GetResponseData(result);
            var certNumbers = GetCertNumbers(data);

            // External user (external@green.com) is assigned to company1 (GreenCorp)
            // Should see only active certs from company1: cert1 (25-0001), cert3 (25-0003)
            // Should NOT see cert4 (expired) or cert5 (draft) even though they're from company1
            certNumbers.Should().Contain("25-0001");
            certNumbers.Should().Contain("25-0003");
            certNumbers.Should().NotContain("25-0002"); // company2
            certNumbers.Should().NotContain("25-0004"); // expired
            certNumbers.Should().NotContain("25-0005"); // draft
            certNumbers.Should().NotContain("25-0007"); // company3
        }

        [Fact]
        public async Task GetCertificateTiles_PartnerRole_SeesOnlyPartnerCerts()
        {
            SetUserClaims("Partner", "partner@amazon.com");

            var result = await _controller.GetCertificateTiles();

            var data = GetResponseData(result);
            var certNumbers = GetCertNumbers(data);

            // Partner user with Amazon partner sees only Amazon-tagged active certs
            certNumbers.Should().Contain("25-0001");
            certNumbers.Should().Contain("25-0003");
            certNumbers.Should().NotContain("25-0002"); // Novi partner
            certNumbers.Should().NotContain("25-0004"); // expired
            certNumbers.Should().NotContain("25-0005"); // draft
            certNumbers.Should().NotContain("25-0006"); // Novi partner
            certNumbers.Should().NotContain("25-0007"); // None partner
        }

        [Fact]
        public async Task GetCertificateTiles_ExternalRoleNoCompany_SeesNoCerts()
        {
            SetUserClaims("External", "nocompany@test.com");

            var result = await _controller.GetCertificateTiles();

            var data = GetResponseData(result);
            data.GetProperty("totalCount").GetInt32().Should().Be(0);
        }

        [Fact]
        public async Task GetCertificateTiles_PartnerRoleNoPartner_SeesNoCerts()
        {
            // User with Partner role but no partner associations in DB
            SetUserClaims("Partner", "nocompany@test.com");

            var result = await _controller.GetCertificateTiles();

            var data = GetResponseData(result);
            data.GetProperty("totalCount").GetInt32().Should().Be(0);
        }

        // ========================================================
        //  STATUS FILTERING
        // ========================================================

        [Fact]
        public async Task GetCertificateTiles_ExcludesExpiredCerts()
        {
            SetUserClaims("Admin", "admin@test.com");

            var result = await _controller.GetCertificateTiles();

            var certNumbers = GetCertNumbers(GetResponseData(result));
            certNumbers.Should().NotContain("25-0004"); // expired
        }

        [Fact]
        public async Task GetCertificateTiles_ExcludesDraftCerts()
        {
            SetUserClaims("Admin", "admin@test.com");

            var result = await _controller.GetCertificateTiles();

            var certNumbers = GetCertNumbers(GetResponseData(result));
            certNumbers.Should().NotContain("25-0005"); // draft
        }

        [Fact]
        public async Task GetCertificateTiles_IncludesExpiringSoonCerts()
        {
            SetUserClaims("Admin", "admin@test.com");

            var result = await _controller.GetCertificateTiles();

            var certNumbers = GetCertNumbers(GetResponseData(result));
            certNumbers.Should().Contain("25-0006"); // expiring_soon
        }

        // ========================================================
        //  SEARCH FILTERING
        // ========================================================

        [Fact]
        public async Task GetCertificateTiles_SearchByCertNumber_FiltersCorrectly()
        {
            SetUserClaims("Admin", "admin@test.com");

            var result = await _controller.GetCertificateTiles(search: "25-0001");

            var data = GetResponseData(result);
            var certNumbers = GetCertNumbers(data);
            certNumbers.Should().Contain("25-0001");
            data.GetProperty("totalCount").GetInt32().Should().Be(1);
        }

        [Fact]
        public async Task GetCertificateTiles_SearchByCompanyName_FiltersCorrectly()
        {
            SetUserClaims("Admin", "admin@test.com");

            var result = await _controller.GetCertificateTiles(search: "GreenCorp");

            var data = GetResponseData(result);
            var certNumbers = GetCertNumbers(data);
            // GreenCorp is company1, has cert1 (active), cert3 (active)
            // cert4 (expired) and cert5 (draft) are excluded by status filter
            certNumbers.Should().Contain("25-0001");
            certNumbers.Should().Contain("25-0003");
        }

        [Fact]
        public async Task GetCertificateTiles_SearchByCountry_FiltersCorrectly()
        {
            SetUserClaims("Admin", "admin@test.com");

            var result = await _controller.GetCertificateTiles(search: "Canada");

            var data = GetResponseData(result);
            var certNumbers = GetCertNumbers(data);
            certNumbers.Should().Contain("25-0002"); // Toronto, Canada
        }

        // ========================================================
        //  QUERY PARAMETER FILTERS
        // ========================================================

        [Fact]
        public async Task GetCertificateTiles_FilterByCertType_FiltersCorrectly()
        {
            SetUserClaims("Admin", "admin@test.com");

            var result = await _controller.GetCertificateTiles(certType: "SFI");

            var data = GetResponseData(result);
            var certNumbers = GetCertNumbers(data);
            // SFI certs: cert2 (active), cert6 (expiring_soon)
            certNumbers.Should().Contain("25-0002");
            certNumbers.Should().Contain("25-0006");
            certNumbers.Should().NotContain("25-0001"); // FSC
        }

        [Fact]
        public async Task GetCertificateTiles_FilterByCountry_FiltersCorrectly()
        {
            SetUserClaims("Admin", "admin@test.com");

            var result = await _controller.GetCertificateTiles(country: "Canada");

            var data = GetResponseData(result);
            data.GetProperty("totalCount").GetInt32().Should().Be(1);
            var certNumbers = GetCertNumbers(data);
            certNumbers.Should().Contain("25-0002");
        }

        [Fact]
        public async Task GetCertificateTiles_FilterByExternalTag_FiltersCorrectly()
        {
            SetUserClaims("Admin", "admin@test.com");

            var result = await _controller.GetCertificateTiles(tags: "eco-friendly");

            var data = GetResponseData(result);
            var certNumbers = GetCertNumbers(data);
            // eco-friendly tag is on cert1 and cert2
            certNumbers.Should().Contain("25-0001");
            certNumbers.Should().Contain("25-0002");
        }

        [Fact]
        public async Task GetCertificateTiles_FilterByInternalTag_ReturnsNone()
        {
            // Internal tags should NOT be filterable from external endpoint
            SetUserClaims("Admin", "admin@test.com");

            var result = await _controller.GetCertificateTiles(tags: "internal-review");

            var data = GetResponseData(result);
            // Tag filter requires TagType == "external", so internal tag match returns nothing
            data.GetProperty("totalCount").GetInt32().Should().Be(0);
        }

        // ========================================================
        //  PAGINATION
        // ========================================================

        [Fact]
        public async Task GetCertificateTiles_Pagination_ReturnsCorrectPage()
        {
            SetUserClaims("Admin", "admin@test.com");

            var result = await _controller.GetCertificateTiles(page: 1, pageSize: 2);

            var data = GetResponseData(result);
            data.GetProperty("page").GetInt32().Should().Be(1);
            data.GetProperty("pageSize").GetInt32().Should().Be(2);
            data.GetProperty("totalCount").GetInt32().Should().Be(5);
            data.GetProperty("hasMore").GetBoolean().Should().BeTrue();
            data.GetProperty("certificates").GetArrayLength().Should().Be(2);
        }

        [Fact]
        public async Task GetCertificateTiles_Pagination_TotalPagesCalculatedCorrectly()
        {
            SetUserClaims("Admin", "admin@test.com");

            var result = await _controller.GetCertificateTiles(page: 1, pageSize: 3);

            var data = GetResponseData(result);
            data.GetProperty("totalPages").GetInt32().Should().Be(2); // 5 items / 3 per page = 2 pages
        }

        [Fact]
        public async Task GetCertificateTiles_LastPage_HasMoreIsFalse()
        {
            SetUserClaims("Admin", "admin@test.com");

            var result = await _controller.GetCertificateTiles(page: 3, pageSize: 2);

            var data = GetResponseData(result);
            data.GetProperty("hasMore").GetBoolean().Should().BeFalse();
        }

        // ========================================================
        //  CERTIFICATE NUMBER FORMATTING
        // ========================================================

        [Fact]
        public async Task GetCertificateTiles_CertNumberAlreadyFormatted_ReturnedAsIs()
        {
            SetUserClaims("Admin", "admin@test.com");

            var result = await _controller.GetCertificateTiles(search: "25-0001");

            var data = GetResponseData(result);
            var certNumber = data.GetProperty("certificates")[0]
                .GetProperty("certificateNumber").GetString();
            certNumber.Should().Be("25-0001");
        }

        // ========================================================
        //  RESPONSE STRUCTURE
        // ========================================================

        [Fact]
        public async Task GetCertificateTiles_ResponseHasExpectedShape()
        {
            SetUserClaims("Admin", "admin@test.com");

            var result = await _controller.GetCertificateTiles(page: 1, pageSize: 1);

            var data = GetResponseData(result);
            data.TryGetProperty("totalCount", out _).Should().BeTrue();
            data.TryGetProperty("page", out _).Should().BeTrue();
            data.TryGetProperty("pageSize", out _).Should().BeTrue();
            data.TryGetProperty("totalPages", out _).Should().BeTrue();
            data.TryGetProperty("hasMore", out _).Should().BeTrue();
            data.TryGetProperty("certificates", out _).Should().BeTrue();

            var cert = data.GetProperty("certificates")[0];
            cert.TryGetProperty("certificateNumber", out _).Should().BeTrue();
            cert.TryGetProperty("certificationType", out _).Should().BeTrue();
            cert.TryGetProperty("companyName", out _).Should().BeTrue();
            cert.TryGetProperty("productName", out _).Should().BeTrue();
            cert.TryGetProperty("location", out _).Should().BeTrue();
            cert.TryGetProperty("country", out _).Should().BeTrue();
            cert.TryGetProperty("partner", out _).Should().BeTrue();
        }

        [Fact]
        public async Task GetCertificateTiles_ResponseShowsExternalTagsOnly()
        {
            SetUserClaims("Admin", "admin@test.com");

            // cert2 has both external and internal tags
            var result = await _controller.GetCertificateTiles(search: "25-0002");

            var data = GetResponseData(result);
            var externalTags = data.GetProperty("certificates")[0]
                .GetProperty("externalTags").GetString();
            // Should contain eco-friendly (external) but NOT internal-review
            externalTags.Should().Contain("eco-friendly");
            externalTags.Should().NotContain("internal-review");
        }

        // ========================================================
        //  AUTOCOMPLETE ENDPOINT
        // ========================================================

        [Fact]
        public async Task GetAutocompleteSuggestions_ShortTerm_ReturnsEmpty()
        {
            var result = await _controller.GetAutocompleteSuggestions("a");

            var data = GetResponseData(result);
            data.GetProperty("suggestions").GetArrayLength().Should().Be(0);
        }

        [Fact]
        public async Task GetAutocompleteSuggestions_EmptyTerm_ReturnsEmpty()
        {
            var result = await _controller.GetAutocompleteSuggestions("");

            var data = GetResponseData(result);
            data.GetProperty("suggestions").GetArrayLength().Should().Be(0);
        }

        [Fact]
        public async Task GetAutocompleteSuggestions_AdminRole_ReturnsSuggestions()
        {
            SetUserClaims("Admin", "admin@test.com");

            var result = await _controller.GetAutocompleteSuggestions("green");

            var data = GetResponseData(result);
            data.GetProperty("suggestions").GetArrayLength().Should().BeGreaterThan(0);
        }

        [Fact]
        public async Task GetAutocompleteSuggestions_ExternalRole_ScopedToCompany()
        {
            SetUserClaims("External", "external@green.com");

            // Search for "eco" which matches EcoProducts company (company2)
            // External user is only associated with company1 (GreenCorp)
            var result = await _controller.GetAutocompleteSuggestions("25-0002");

            var data = GetResponseData(result);
            var suggestions = data.GetProperty("suggestions");
            // cert 25-0002 belongs to company2 - external user at company1 should NOT see it
            // in certificate number suggestions
            var certSuggestions = suggestions.EnumerateArray()
                .Where(s => s.GetProperty("type").GetString() == "Certificate Number")
                .ToList();
            certSuggestions.Should().BeEmpty();
        }

        [Fact]
        public async Task GetAutocompleteSuggestions_PartnerRole_ScopedToPartner()
        {
            SetUserClaims("Partner", "partner@amazon.com");

            // Search for "25-0002" which is a Novi cert - Amazon partner shouldn't see it
            var result = await _controller.GetAutocompleteSuggestions("25-0002");

            var data = GetResponseData(result);
            var certSuggestions = data.GetProperty("suggestions").EnumerateArray()
                .Where(s => s.GetProperty("type").GetString() == "Certificate Number")
                .ToList();
            certSuggestions.Should().BeEmpty();
        }

        [Fact]
        public async Task GetAutocompleteSuggestions_PartnerRole_FindsOwnCerts()
        {
            SetUserClaims("Partner", "partner@amazon.com");

            var result = await _controller.GetAutocompleteSuggestions("25-0001");

            var data = GetResponseData(result);
            var certSuggestions = data.GetProperty("suggestions").EnumerateArray()
                .Where(s => s.GetProperty("type").GetString() == "Certificate Number")
                .ToList();
            certSuggestions.Should().NotBeEmpty();
        }

        // ========================================================
        //  FILTER OPTIONS ENDPOINT
        // ========================================================

        [Fact]
        public async Task GetFilterOptions_AdminRole_ReturnsAllFilters()
        {
            SetUserClaims("Admin", "admin@test.com");

            var result = await _controller.GetFilterOptions();

            var data = GetResponseData(result);
            data.TryGetProperty("certificationTypes", out _).Should().BeTrue();
            data.TryGetProperty("productCategories", out _).Should().BeTrue();
            data.TryGetProperty("companies", out _).Should().BeTrue();
            data.TryGetProperty("countries", out _).Should().BeTrue();
            data.TryGetProperty("tags", out _).Should().BeTrue();
            data.TryGetProperty("partners", out _).Should().BeTrue();
        }

        [Fact]
        public async Task GetFilterOptions_AdminRole_ReturnsMultipleCompanies()
        {
            SetUserClaims("Admin", "admin@test.com");

            var result = await _controller.GetFilterOptions();

            var data = GetResponseData(result);
            var companies = data.GetProperty("companies");
            companies.GetArrayLength().Should().BeGreaterThanOrEqualTo(2);
        }

        [Fact]
        public async Task GetFilterOptions_ExternalRole_ScopedToCompany()
        {
            SetUserClaims("External", "external@green.com");

            var result = await _controller.GetFilterOptions();

            var data = GetResponseData(result);
            var companies = data.GetProperty("companies");
            // External user at company1 should only see company1 in filters
            var companyNames = companies.EnumerateArray()
                .Select(c => c.GetProperty("name").GetString())
                .ToList();
            companyNames.Should().Contain("GreenCorp");
            companyNames.Should().NotContain("EcoProducts");
        }

        [Fact]
        public async Task GetFilterOptions_ExternalRole_OnlyShowsExternalTags()
        {
            SetUserClaims("External", "external@green.com");

            var result = await _controller.GetFilterOptions();

            var data = GetResponseData(result);
            var additionalTags = data.GetProperty("additionalTags");
            var tagNames = additionalTags.EnumerateArray()
                .Select(t => t.GetString())
                .ToList();
            // Should show external tags, not internal
            if (tagNames.Any())
            {
                tagNames.Should().NotContain("internal-review");
            }
        }

        [Fact]
        public async Task GetFilterOptions_PartnerRole_ScopedToPartner()
        {
            SetUserClaims("Partner", "partner@amazon.com");

            var result = await _controller.GetFilterOptions();

            var data = GetResponseData(result);
            var partners = data.GetProperty("partners");
            var partnerNames = partners.EnumerateArray()
                .Select(p => p.GetString())
                .ToList();
            // Amazon partner user should see only Amazon in partners filter
            partnerNames.Should().Contain("Amazon");
            partnerNames.Should().NotContain("Novi");
        }

        [Fact]
        public async Task GetFilterOptions_ExternalNoCompany_ReturnsEmptyFilters()
        {
            SetUserClaims("External", "nocompany@test.com");

            var result = await _controller.GetFilterOptions();

            var data = GetResponseData(result);
            data.GetProperty("companies").GetArrayLength().Should().Be(0);
        }

        // ========================================================
        //  DOWNLOAD CERTIFICATE
        // ========================================================

        [Fact]
        public async Task DownloadCertificate_NonexistentCert_ReturnsNotFound()
        {
            var result = await _controller.DownloadCertificate(9999);

            result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public async Task DownloadCertificate_ExpiredCert_ReturnsBadRequest()
        {
            SetUserClaims("Admin", "admin@test.com");
            var expiredCert = await _context.Certificates.FirstAsync(c => c.Status == "expired");

            var result = await _controller.DownloadCertificate(expiredCert.Id);

            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task DownloadCertificate_DraftCert_ReturnsBadRequest()
        {
            SetUserClaims("Admin", "admin@test.com");
            var draftCert = await _context.Certificates.FirstAsync(c => c.Status == "draft");

            var result = await _controller.DownloadCertificate(draftCert.Id);

            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task DownloadCertificate_ExternalUserWrongCompany_ReturnsForbid()
        {
            SetUserClaims("External", "external@green.com");
            // cert from company2, but external user is at company1
            var cert2 = await _context.Certificates.FirstAsync(c => c.Number == "25-0002");

            var result = await _controller.DownloadCertificate(cert2.Id);

            result.Should().BeOfType<ForbidResult>();
        }

        [Fact]
        public async Task DownloadCertificate_PartnerUserWrongPartner_ReturnsForbid()
        {
            SetUserClaims("Partner", "partner@amazon.com");
            // cert2 has Partner = "Novi", but user is Amazon partner
            var cert2 = await _context.Certificates.FirstAsync(c => c.Number == "25-0002");

            var result = await _controller.DownloadCertificate(cert2.Id);

            result.Should().BeOfType<ForbidResult>();
        }

        [Fact]
        public async Task DownloadCertificate_ActiveCertNoPdf_ReturnsNotFound()
        {
            SetUserClaims("Admin", "admin@test.com");
            var cert = await _context.Certificates.FirstAsync(c => c.Number == "25-0001");
            // CertificateFile is null by default

            var result = await _controller.DownloadCertificate(cert.Id);

            result.Should().BeOfType<NotFoundObjectResult>();
        }

        // ========================================================
        //  SHARE LINK
        // ========================================================

        [Fact]
        public void GetShareableLink_WithFilters_ReturnsUrl()
        {
            var request = new ExternalShareLinkRequest
            {
                Search = "paper",
                CertType = "FSC",
                Country = "USA",
                SortBy = "newest"
            };

            var result = _controller.GetShareableLink(request);

            var data = GetResponseData(result);
            var url = data.GetProperty("url").GetString();
            url.Should().Contain("/Certificate/CertificateTileViewExternal");
            url.Should().Contain("search=paper");
            url.Should().Contain("certType=FSC");
            url.Should().Contain("country=USA");
            url.Should().Contain("sort=newest");
        }

        [Fact]
        public void GetShareableLink_EmptyFilters_ReturnsBaseUrl()
        {
            var request = new ExternalShareLinkRequest();

            var result = _controller.GetShareableLink(request);

            var data = GetResponseData(result);
            var url = data.GetProperty("url").GetString();
            url.Should().Be("https://localhost:5101/Certificate/CertificateTileViewExternal");
        }

        [Fact]
        public void GetShareableLink_SpecialCharacters_AreUrlEncoded()
        {
            var request = new ExternalShareLinkRequest { Search = "recycled paper & towels" };

            var result = _controller.GetShareableLink(request);

            var data = GetResponseData(result);
            var url = data.GetProperty("url").GetString();
            url.Should().Contain("search=recycled%20paper%20%26%20towels");
        }

        // ========================================================
        //  SORTING
        // ========================================================

        [Fact]
        public async Task GetCertificateTiles_SortByNewest_DefaultSort()
        {
            SetUserClaims("Admin", "admin@test.com");

            var result = await _controller.GetCertificateTiles(sortBy: "newest");

            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task GetCertificateTiles_SortByCompanyAZ_SortsCorrectly()
        {
            SetUserClaims("Admin", "admin@test.com");

            var result = await _controller.GetCertificateTiles(sortBy: "company-az", pageSize: 50);

            var data = GetResponseData(result);
            var companies = data.GetProperty("certificates").EnumerateArray()
                .Select(c => c.GetProperty("companyName").GetString()!)
                .ToList();

            // Verify A-Z ordering
            for (int i = 1; i < companies.Count; i++)
            {
                string.Compare(companies[i - 1], companies[i], StringComparison.Ordinal)
                    .Should().BeLessThanOrEqualTo(0);
            }
        }

        [Fact]
        public async Task GetCertificateTiles_SortByExpiring_SortsCorrectly()
        {
            SetUserClaims("Admin", "admin@test.com");

            var result = await _controller.GetCertificateTiles(sortBy: "expiring", pageSize: 50);

            var data = GetResponseData(result);
            var dates = data.GetProperty("certificates").EnumerateArray()
                .Select(c => c.GetProperty("expirationDate").GetString()!)
                .ToList();

            // Verify ascending order by expiration date
            for (int i = 1; i < dates.Count; i++)
            {
                string.Compare(dates[i - 1], dates[i], StringComparison.Ordinal)
                    .Should().BeLessThanOrEqualTo(0);
            }
        }

        [Fact]
        public async Task GetCertificateTiles_InvalidSortBy_DefaultsToNewest()
        {
            SetUserClaims("Admin", "admin@test.com");

            var result = await _controller.GetCertificateTiles(sortBy: "invalid-sort");

            // Should not throw, defaults to newest
            result.Should().BeOfType<OkObjectResult>();
            var data = GetResponseData(result);
            data.GetProperty("totalCount").GetInt32().Should().Be(5);
        }

        // ========================================================
        //  COMBINED ROLE + FILTER TESTS
        // ========================================================

        [Fact]
        public async Task GetCertificateTiles_ExternalWithSearch_SearchWithinCompanyScope()
        {
            SetUserClaims("External", "external@green.com");

            // Search for "FSC" - cert1 and cert3 are FSC and belong to company1
            var result = await _controller.GetCertificateTiles(search: "FSC");

            var data = GetResponseData(result);
            var certNumbers = GetCertNumbers(data);
            // Should find FSC certs only from company1
            certNumbers.Should().Contain("25-0001");
            certNumbers.Should().Contain("25-0003");
            // cert7 is FSC but belongs to company3 - should NOT appear
            certNumbers.Should().NotContain("25-0007");
        }

        [Fact]
        public async Task GetCertificateTiles_PartnerWithCertTypeFilter_FilterWithinPartnerScope()
        {
            SetUserClaims("Partner", "partner@amazon.com");

            // Filter by SFI cert type - but Amazon partner only has FSC certs
            var result = await _controller.GetCertificateTiles(certType: "SFI");

            var data = GetResponseData(result);
            data.GetProperty("totalCount").GetInt32().Should().Be(0);
        }

        [Fact]
        public async Task GetCertificateTiles_AnalystRole_SeesAllActiveCerts()
        {
            SetUserClaims("Analyst", "analyst@test.com");

            var result = await _controller.GetCertificateTiles();

            var data = GetResponseData(result);
            // Analyst (like Admin) sees all active/expiring_soon/renewal_in_progress certs
            data.GetProperty("totalCount").GetInt32().Should().Be(5);
        }
    }
}
