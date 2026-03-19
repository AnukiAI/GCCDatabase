using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using GCCWebApp.Controllers.Api;
using GCCWebApp.Data;
using GCCWebApp.Models;

namespace GCCWebApp.Api.Tests.Integration
{
    /// <summary>
    /// Tests that external users only see certificates from their assigned companies.
    /// Covers: Amazon partner, Novi partner, no-partner, and no-company-association scenarios.
    /// </summary>
    public class ExternalCertificateVisibilityTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly Mock<ILogger<CertificateTileExternalApiController>> _logger;

        // Test company IDs (set during seed)
        private int _amazonCoId;
        private int _noviCoId;
        private int _noPartnerCoId;

        public ExternalCertificateVisibilityTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            _context = new ApplicationDbContext(options);
            _logger = new Mock<ILogger<CertificateTileExternalApiController>>();

            SeedTestData();
        }

        public void Dispose()
        {
            _context.Dispose();
        }

        private void SeedTestData()
        {
            // Roles
            var externalRole = new Role { Id = 1, Name = "External", Description = "External user" };
            var adminRole = new Role { Id = 2, Name = "Admin", Description = "Admin user" };
            _context.Roles.AddRange(externalRole, adminRole);
            _context.SaveChanges();

            // Certification type
            var certType = new CertificationType { Id = 1, Name = "Test Cert Type", IsActive = true };
            _context.CertificationTypes.Add(certType);
            _context.SaveChanges();

            // Companies
            var amazonCo = new Company { Name = "ExtTest-AmazonCo", Email = "a@test.com" };
            var noviCo = new Company { Name = "ExtTest-NoviCo", Email = "n@test.com" };
            var noPartnerCo = new Company { Name = "ExtTest-NoPartnerCo", Email = "np@test.com" };
            _context.Companies.AddRange(amazonCo, noviCo, noPartnerCo);
            _context.SaveChanges();

            _amazonCoId = amazonCo.Id;
            _noviCoId = noviCo.Id;
            _noPartnerCoId = noPartnerCo.Id;

            // Products
            var amazonProd = new Product { Name = "Amazon Product", CompanyId = _amazonCoId, CreatedAt = DateTime.UtcNow };
            var noviProd = new Product { Name = "Novi Product", CompanyId = _noviCoId, CreatedAt = DateTime.UtcNow };
            var noPartnerProd = new Product { Name = "NoPartner Product", CompanyId = _noPartnerCoId, CreatedAt = DateTime.UtcNow };
            _context.Products.AddRange(amazonProd, noviProd, noPartnerProd);
            _context.SaveChanges();

            // Certificates - Amazon company (3 active, 1 draft)
            _context.Certificates.AddRange(
                new Certificate { Number = "EXT-AMZ-001", Status = "active", Partner = "Amazon", CompanyId = _amazonCoId, ProductId = amazonProd.Id, CertificationTypeId = 1, EffectiveDate = DateTime.UtcNow.AddDays(-60), ExpirationDate = DateTime.UtcNow.AddDays(300), Country = "United States", CreatedAt = DateTime.UtcNow },
                new Certificate { Number = "EXT-AMZ-002", Status = "active", Partner = "Amazon", CompanyId = _amazonCoId, ProductId = amazonProd.Id, CertificationTypeId = 1, EffectiveDate = DateTime.UtcNow.AddDays(-30), ExpirationDate = DateTime.UtcNow.AddDays(330), Country = "United States", CreatedAt = DateTime.UtcNow },
                new Certificate { Number = "EXT-AMZ-003", Status = "active", Partner = "Amazon", CompanyId = _amazonCoId, ProductId = amazonProd.Id, CertificationTypeId = 1, EffectiveDate = DateTime.UtcNow.AddDays(-10), ExpirationDate = DateTime.UtcNow.AddDays(350), Country = "United States", CreatedAt = DateTime.UtcNow },
                new Certificate { Number = "EXT-AMZ-DRAFT", Status = "draft", Partner = "Amazon", CompanyId = _amazonCoId, ProductId = amazonProd.Id, CertificationTypeId = 1, EffectiveDate = DateTime.UtcNow.AddDays(-10), ExpirationDate = DateTime.UtcNow.AddDays(350), Country = "United States", CreatedAt = DateTime.UtcNow }
            );

            // Certificates - Novi company (2 active, 1 expired)
            _context.Certificates.AddRange(
                new Certificate { Number = "EXT-NOVI-001", Status = "active", Partner = "Novi", CompanyId = _noviCoId, ProductId = noviProd.Id, CertificationTypeId = 1, EffectiveDate = DateTime.UtcNow.AddDays(-45), ExpirationDate = DateTime.UtcNow.AddDays(315), Country = "United States", CreatedAt = DateTime.UtcNow },
                new Certificate { Number = "EXT-NOVI-002", Status = "active", Partner = "Novi", CompanyId = _noviCoId, ProductId = noviProd.Id, CertificationTypeId = 1, EffectiveDate = DateTime.UtcNow.AddDays(-20), ExpirationDate = DateTime.UtcNow.AddDays(340), Country = "United States", CreatedAt = DateTime.UtcNow },
                new Certificate { Number = "EXT-NOVI-EXPIRED", Status = "expired", Partner = "Novi", CompanyId = _noviCoId, ProductId = noviProd.Id, CertificationTypeId = 1, EffectiveDate = DateTime.UtcNow.AddDays(-365), ExpirationDate = DateTime.UtcNow.AddDays(-1), Country = "United States", CreatedAt = DateTime.UtcNow }
            );

            // Certificates - No partner company (2 active)
            _context.Certificates.AddRange(
                new Certificate { Number = "EXT-NP-001", Status = "active", Partner = "None", CompanyId = _noPartnerCoId, ProductId = noPartnerProd.Id, CertificationTypeId = 1, EffectiveDate = DateTime.UtcNow.AddDays(-50), ExpirationDate = DateTime.UtcNow.AddDays(310), Country = "United States", CreatedAt = DateTime.UtcNow },
                new Certificate { Number = "EXT-NP-002", Status = "active", Partner = null, CompanyId = _noPartnerCoId, ProductId = noPartnerProd.Id, CertificationTypeId = 1, EffectiveDate = DateTime.UtcNow.AddDays(-30), ExpirationDate = DateTime.UtcNow.AddDays(330), Country = "United States", CreatedAt = DateTime.UtcNow }
            );
            _context.SaveChanges();

            // Users with company associations
            var extAmazon = new User
            {
                Username = "ext-amazon", Email = "ext-amazon@test.com",
                PasswordHash = "hash", Status = "Active", IsActive = true,
                CreatedAt = DateTime.UtcNow, TwoFactorMethod = "Email",
                Roles = new List<Role> { externalRole },
                Companies = new List<Company> { amazonCo }
            };
            var extNovi = new User
            {
                Username = "ext-novi", Email = "ext-novi@test.com",
                PasswordHash = "hash", Status = "Active", IsActive = true,
                CreatedAt = DateTime.UtcNow, TwoFactorMethod = "Email",
                Roles = new List<Role> { externalRole },
                Companies = new List<Company> { noviCo }
            };
            var extNoPartner = new User
            {
                Username = "ext-nopartner", Email = "ext-nopartner@test.com",
                PasswordHash = "hash", Status = "Active", IsActive = true,
                CreatedAt = DateTime.UtcNow, TwoFactorMethod = "Email",
                Roles = new List<Role> { externalRole },
                Companies = new List<Company> { noPartnerCo }
            };
            var extNone = new User
            {
                Username = "ext-none", Email = "ext-none@test.com",
                PasswordHash = "hash", Status = "Active", IsActive = true,
                CreatedAt = DateTime.UtcNow, TwoFactorMethod = "Email",
                Roles = new List<Role> { externalRole },
                Companies = new List<Company>() // NO company association
            };

            _context.Users.AddRange(extAmazon, extNovi, extNoPartner, extNone);
            _context.SaveChanges();
        }

        private CertificateTileExternalApiController CreateControllerForUser(string email, string role = "External")
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Email, email),
                new Claim("Role", role),
                new Claim(ClaimTypes.Role, role)
            };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var principal = new ClaimsPrincipal(identity);

            var controller = new CertificateTileExternalApiController(_context, _logger.Object);
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal }
            };
            return controller;
        }

        [Fact]
        public async Task ExternalUser_WithAmazonCompany_SeesOnlyAmazonCompanyCerts()
        {
            var controller = CreateControllerForUser("ext-amazon@test.com");

            var result = await controller.GetCertificateTiles();
            var okResult = result as OkObjectResult;

            okResult.Should().NotBeNull("expected 200 OK response");

            dynamic data = okResult!.Value!;
            var totalCount = (int)data.GetType().GetProperty("totalCount")!.GetValue(data);
            var certificates = (IEnumerable<dynamic>)data.GetType().GetProperty("certificates")!.GetValue(data);
            var certList = certificates.ToList();

            // Should see 3 active certs from AmazonCo (draft excluded by status filter)
            totalCount.Should().Be(3, "ext-amazon should see 3 active certificates from ExtTest-AmazonCo");

            foreach (var cert in certList)
            {
                int companyId = (int)cert.GetType().GetProperty("companyId")!.GetValue(cert);
                companyId.Should().Be(_amazonCoId, "all returned certs should belong to ExtTest-AmazonCo");
            }
        }

        [Fact]
        public async Task ExternalUser_WithNoviCompany_SeesOnlyNoviCompanyCerts()
        {
            var controller = CreateControllerForUser("ext-novi@test.com");

            var result = await controller.GetCertificateTiles();
            var okResult = result as OkObjectResult;

            okResult.Should().NotBeNull();

            dynamic data = okResult!.Value!;
            var totalCount = (int)data.GetType().GetProperty("totalCount")!.GetValue(data);
            var certificates = (IEnumerable<dynamic>)data.GetType().GetProperty("certificates")!.GetValue(data);
            var certList = certificates.ToList();

            // Should see 2 active certs from NoviCo (expired excluded by status filter)
            totalCount.Should().Be(2, "ext-novi should see 2 active certificates from ExtTest-NoviCo");

            foreach (var cert in certList)
            {
                int companyId = (int)cert.GetType().GetProperty("companyId")!.GetValue(cert);
                companyId.Should().Be(_noviCoId, "all returned certs should belong to ExtTest-NoviCo");
            }
        }

        [Fact]
        public async Task ExternalUser_WithNoPartnerCompany_SeesOnlyNoPartnerCerts()
        {
            var controller = CreateControllerForUser("ext-nopartner@test.com");

            var result = await controller.GetCertificateTiles();
            var okResult = result as OkObjectResult;

            okResult.Should().NotBeNull();

            dynamic data = okResult!.Value!;
            var totalCount = (int)data.GetType().GetProperty("totalCount")!.GetValue(data);
            var certificates = (IEnumerable<dynamic>)data.GetType().GetProperty("certificates")!.GetValue(data);
            var certList = certificates.ToList();

            // Should see 2 active certs from NoPartnerCo
            totalCount.Should().Be(2, "ext-nopartner should see 2 active certificates from ExtTest-NoPartnerCo");

            foreach (var cert in certList)
            {
                int companyId = (int)cert.GetType().GetProperty("companyId")!.GetValue(cert);
                companyId.Should().Be(_noPartnerCoId, "all returned certs should belong to ExtTest-NoPartnerCo");
            }
        }

        [Fact]
        public async Task ExternalUser_WithNoCompanyAssociation_SeesZeroCerts()
        {
            var controller = CreateControllerForUser("ext-none@test.com");

            var result = await controller.GetCertificateTiles();
            var okResult = result as OkObjectResult;

            okResult.Should().NotBeNull();

            dynamic data = okResult!.Value!;
            var totalCount = (int)data.GetType().GetProperty("totalCount")!.GetValue(data);

            totalCount.Should().Be(0, "ext-none has no company association and should see 0 certificates");
        }

        [Fact]
        public async Task ExternalUser_DoesNotSeeDraftOrExpiredCerts()
        {
            // ext-amazon has 4 total certs: 3 active + 1 draft
            var controller = CreateControllerForUser("ext-amazon@test.com");

            var result = await controller.GetCertificateTiles();
            var okResult = result as OkObjectResult;

            okResult.Should().NotBeNull();

            dynamic data = okResult!.Value!;
            var certificates = (IEnumerable<dynamic>)data.GetType().GetProperty("certificates")!.GetValue(data);
            var certList = certificates.ToList();

            // Verify no draft/expired certs returned
            foreach (var cert in certList)
            {
                string number = (string)cert.GetType().GetProperty("certificateNumber")!.GetValue(cert);
                number.Should().NotContain("DRAFT", "draft certificates should not be visible to external users");
                number.Should().NotContain("EXPIRED", "expired certificates should not be visible to external users");
            }
        }

        [Fact]
        public async Task ExternalUser_CannotSeeCertsFromOtherCompanies()
        {
            // ext-amazon should NOT see any NoviCo or NoPartnerCo certs
            var controller = CreateControllerForUser("ext-amazon@test.com");

            var result = await controller.GetCertificateTiles();
            var okResult = result as OkObjectResult;

            okResult.Should().NotBeNull();

            dynamic data = okResult!.Value!;
            var certificates = (IEnumerable<dynamic>)data.GetType().GetProperty("certificates")!.GetValue(data);
            var certList = certificates.ToList();

            foreach (var cert in certList)
            {
                int companyId = (int)cert.GetType().GetProperty("companyId")!.GetValue(cert);
                companyId.Should().NotBe(_noviCoId, "ext-amazon should not see NoviCo certs");
                companyId.Should().NotBe(_noPartnerCoId, "ext-amazon should not see NoPartnerCo certs");
            }
        }
    }
}
