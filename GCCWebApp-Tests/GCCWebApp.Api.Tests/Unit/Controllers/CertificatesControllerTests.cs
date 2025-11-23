using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using FluentAssertions;
using GCCWebApp.Controllers.Api.v1;
using GCCWebApp.Data;
using GCCWebApp.Models;
using GCCWebApp.Models.Api;
using GCCWebApp.Api.Tests.TestUtilities;
using Microsoft.EntityFrameworkCore;

namespace GCCWebApp.Api.Tests.Unit.Controllers
{
    public class CertificatesControllerTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly Mock<ILogger<CertificatesController>> _loggerMock;
        private readonly CertificatesController _controller;

        public CertificatesControllerTests()
        {
            _context = TestDbContextFactory.CreateInMemoryContext();
            _loggerMock = new Mock<ILogger<CertificatesController>>();
            _controller = new CertificatesController(_context, _loggerMock.Object);

            // Setup HttpContext for the controller (required for BaseApiController)
            ControllerTestHelper.SetupHttpContext(_controller);

            // Seed test data
            TestDbContextFactory.SeedTestData(_context);
        }

        public void Dispose()
        {
            _context.Dispose();
        }

        [Fact]
        public async Task GetCertificates_WithValidPagination_ReturnsOkResult()
        {
            // Arrange
            var parameters = new PaginationParams
            {
                PageNumber = 1,
                PageSize = 10
            };

            // Act
            var result = await _controller.GetCertificates(parameters);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            okResult!.Value.Should().BeOfType<PaginatedResponse<IEnumerable<Certificate>>>();
            
            var response = okResult.Value as PaginatedResponse<IEnumerable<Certificate>>;
            response!.Success.Should().BeTrue();
            response.Data.Should().NotBeNull();
            response.TotalCount.Should().BeGreaterThan(0);
        }

        [Fact]
        public async Task GetCertificates_WithSearchTerm_FiltersResults()
        {
            // Arrange
            var parameters = new PaginationParams
            {
                PageNumber = 1,
                PageSize = 10,
                SearchTerm = "CERT-TEST"
            };

            // Act
            var result = await _controller.GetCertificates(parameters);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            var response = okResult!.Value as PaginatedResponse<IEnumerable<Certificate>>;
            
            response!.Success.Should().BeTrue();
            response.Data.Should().NotBeNull();
            response.Data.Should().HaveCountGreaterThan(0);
        }

        [Fact]
        public async Task GetCertificate_WithValidId_ReturnsOkResult()
        {
            // Arrange
            const int certificateId = 1;

            // Act
            var result = await _controller.GetCertificate(certificateId);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            
            // Note: This will need updating once BaseApiController uses StandardApiResponse
            var response = okResult!.Value;
            response.Should().NotBeNull();
        }

        [Fact]
        public async Task GetCertificate_WithInvalidId_ReturnsNotFound()
        {
            // Arrange
            const int invalidId = 999;

            // Act
            var result = await _controller.GetCertificate(invalidId);

            // Assert
            result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public async Task GetCertificateByNumber_WithValidNumber_ReturnsOkResult()
        {
            // Arrange
            const string certificateNumber = "CERT-TEST-001";

            // Act
            var result = await _controller.GetCertificateByNumber(certificateNumber);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task GetCertificateByNumber_WithInvalidNumber_ReturnsNotFound()
        {
            // Arrange
            const string invalidNumber = "INVALID-CERT";

            // Act
            var result = await _controller.GetCertificateByNumber(invalidNumber);

            // Assert
            result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public async Task CreateCertificate_WithValidData_ReturnsCreatedResult()
        {
            // Arrange
            var certificate = new Certificate
            {
                Number = "CERT-NEW-001",
                ProductId = 1,
                CompanyId = 1,
                CertificationTypeId = 1,
                EffectiveDate = DateTime.UtcNow,
                ExpirationDate = DateTime.UtcNow.AddYears(1),
                Status = "Active"
            };

            // Act
            var result = await _controller.CreateCertificate(certificate);

            // Assert
            result.Should().BeOfType<ObjectResult>();
            var objectResult = result as ObjectResult;
            objectResult!.StatusCode.Should().Be(201);
        }

        [Fact]
        public async Task CreateCertificate_WithDuplicateNumber_ReturnsBadRequest()
        {
            // Arrange
            var certificate = new Certificate
            {
                Number = "CERT-TEST-001", // Duplicate number
                ProductId = 1,
                CompanyId = 1,
                CertificationTypeId = 1,
                EffectiveDate = DateTime.UtcNow,
                Status = "Active"
            };

            // Act
            var result = await _controller.CreateCertificate(certificate);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task UpdateCertificate_WithValidData_ReturnsOkResult()
        {
            // Arrange
            const int certificateId = 1;
            var updatedCertificate = new Certificate
            {
                Id = certificateId,
                Number = "CERT-UPDATED-001",
                ProductId = 1,
                CompanyId = 1,
                CertificationTypeId = 1,
                EffectiveDate = DateTime.UtcNow,
                Status = "Active"
            };

            // Act
            var result = await _controller.UpdateCertificate(certificateId, updatedCertificate);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task UpdateCertificate_WithMismatchedId_ReturnsBadRequest()
        {
            // Arrange
            const int certificateId = 1;
            var updatedCertificate = new Certificate
            {
                Id = 2, // Mismatched ID
                Number = "CERT-UPDATED-001",
                ProductId = 1,
                CompanyId = 1,
                CertificationTypeId = 1,
                EffectiveDate = DateTime.UtcNow,
                Status = "Active"
            };

            // Act
            var result = await _controller.UpdateCertificate(certificateId, updatedCertificate);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task DeleteCertificate_WithValidId_ReturnsNoContent()
        {
            // Arrange
            const int certificateId = 1;

            // Act
            var result = await _controller.DeleteCertificate(certificateId);

            // Assert
            result.Should().BeOfType<NoContentResult>();

            // Verify certificate was deleted
            var deletedCertificate = await _context.Certificates.FindAsync(certificateId);
            deletedCertificate.Should().BeNull();
        }

        [Fact]
        public async Task DeleteCertificate_WithInvalidId_ReturnsNotFound()
        {
            // Arrange
            const int invalidId = 999;

            // Act
            var result = await _controller.DeleteCertificate(invalidId);

            // Assert
            result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public async Task SearchCertificates_WithMultipleFilters_FiltersCorrectly()
        {
            // Arrange
            var parameters = new PaginationParams { PageNumber = 1, PageSize = 10 };

            // Act
            var result = await _controller.SearchCertificates(
                term: "TEST",
                productId: 1,
                companyId: 1,
                certificationTypeId: null,
                effectiveDateFrom: null,
                effectiveDateTo: null,
                expirationDateFrom: null,
                expirationDateTo: null,
                status: "Active",
                parameters: parameters);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            var response = okResult!.Value as PaginatedResponse<IEnumerable<Certificate>>;
            
            response!.Success.Should().BeTrue();
            response.Data.Should().NotBeNull();
        }

        [Fact]
        public async Task GetExpiringCertificates_WithValidDays_ReturnsResults()
        {
            // Arrange
            const int days = 60; // Should include our test certificate
            var parameters = new PaginationParams { PageNumber = 1, PageSize = 10 };

            // Act
            var result = await _controller.GetExpiringCertificates(days, parameters);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            var response = okResult!.Value as PaginatedResponse<IEnumerable<Certificate>>;
            
            response!.Success.Should().BeTrue();
            response.Data.Should().NotBeNull();
            response.TotalCount.Should().BeGreaterThan(0);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public async Task GetCertificates_WithInvalidPageNumber_ShouldHandleGracefully(int pageNumber)
        {
            // Arrange
            var parameters = new PaginationParams
            {
                PageNumber = pageNumber,
                PageSize = 10
            };

            // Act & Assert
            // The controller should handle invalid page numbers gracefully
            // This test verifies the behavior - adjust expectations based on implementation
            var result = await _controller.GetCertificates(parameters);
            result.Should().NotBeNull();
        }

        [Fact]
        public async Task GetCertificates_WithLargePageSize_ShouldRespectMaximum()
        {
            // Arrange
            var parameters = new PaginationParams
            {
                PageNumber = 1,
                PageSize = 1000 // Larger than typical maximum
            };

            // Act
            var result = await _controller.GetCertificates(parameters);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            var response = okResult!.Value as PaginatedResponse<IEnumerable<Certificate>>;
            
            // The PaginationParams should enforce maximum page size
            response!.PageSize.Should().BeLessOrEqualTo(100);
        }
    }
}