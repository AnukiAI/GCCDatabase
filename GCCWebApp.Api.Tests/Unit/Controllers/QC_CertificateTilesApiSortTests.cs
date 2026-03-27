using Xunit;
using FluentAssertions;
using GCCWebApp.Controllers.Api;
using GCCWebApp.Repositories;
using GCCWebApp.Data;
using GCCWebApp.Models;
using GCCWebApp.Services;
using GCCWebApp.Api.Tests.TestUtilities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Moq;

namespace GCCWebApp.Api.Tests.Unit.Controllers
{
    /// <summary>
    /// Tests for CertificateTilesApiController sort logic — QC #26 (sortBy ?? sort priority)
    /// Verifies that the sortBy parameter takes precedence over sort, that MapSortBy maps
    /// all frontend sort names correctly, and that SortDescending is set correctly for Z-A sorts.
    /// </summary>
    public class QC_CertificateTilesApiSortTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly Mock<ICertificateRepository> _repositoryMock;
        private readonly CertificateTilesApiController _controller;
        private CertificateSearchFilter? _capturedFilter;

        public QC_CertificateTilesApiSortTests()
        {
            _context = TestDbContextFactory.CreateInMemoryContext();

            _repositoryMock = new Mock<ICertificateRepository>();

            // Capture the filter passed to the repository so we can assert on SortBy/SortDescending
            _repositoryMock
                .Setup(r => r.GetAdvancedFilteredCertificatesAsync(It.IsAny<CertificateSearchFilter>()))
                .Callback<CertificateSearchFilter>(f => _capturedFilter = f)
                .ReturnsAsync((new List<Certificate>(), 0));

            var loggerMock = new Mock<ILogger<CertificateTilesApiController>>();
            var unitOfWorkMock = new Mock<IUnitOfWork>();
            var pdfServiceMock = new Mock<ICertificatePdfService>();
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>())
                .Build();

            _controller = new CertificateTilesApiController(
                loggerMock.Object,
                _repositoryMock.Object,
                unitOfWorkMock.Object,
                pdfServiceMock.Object,
                _context,
                config);
        }

        public void Dispose()
        {
            _context.Dispose();
        }

        #region Sort Priority (QC #26)

        [Fact]
        public async Task GetCertificates_SortByProvided_UsesSortByOverSort()
        {
            // sortBy = "company-az", sort = "cert-number-newest" — sortBy should win
            await _controller.GetCertificates(sortBy: "company-az", sort: "cert-number-newest");

            _capturedFilter.Should().NotBeNull();
            _capturedFilter!.SortBy.Should().Be("Company");
        }

        [Fact]
        public async Task GetCertificates_OnlySortProvided_UsesSort()
        {
            await _controller.GetCertificates(sortBy: null, sort: "product-az");

            _capturedFilter.Should().NotBeNull();
            _capturedFilter!.SortBy.Should().Be("Product");
        }

        [Fact]
        public async Task GetCertificates_NoSortParams_DefaultsToCertNumberNewest()
        {
            await _controller.GetCertificates(sortBy: null, sort: null);

            _capturedFilter.Should().NotBeNull();
            _capturedFilter!.SortBy.Should().Be("Number");
            _capturedFilter.SortDescending.Should().BeTrue();
        }

        #endregion

        #region MapSortBy Mappings

        [Theory]
        [InlineData("company-az", "Company", false)]
        [InlineData("company-za", "Company", true)]
        [InlineData("product-az", "Product", false)]
        [InlineData("product-za", "Product", true)]
        [InlineData("cert-type", "CertType", false)]
        [InlineData("cert-type-za", "CertType", true)]
        [InlineData("location-az", "Location", false)]
        [InlineData("location-za", "Location", true)]
        [InlineData("country", "Country", false)]
        [InlineData("country-za", "Country", true)]
        [InlineData("cert-number-newest", "Number", true)]
        [InlineData("cert-number-oldest", "Number", false)]
        public async Task GetCertificates_SortMapping_MapsCorrectly(string sortValue, string expectedSortBy, bool expectedDescending)
        {
            await _controller.GetCertificates(sortBy: sortValue);

            _capturedFilter.Should().NotBeNull();
            _capturedFilter!.SortBy.Should().Be(expectedSortBy);
            _capturedFilter.SortDescending.Should().Be(expectedDescending);
        }

        #endregion

        #region SortDescending Logic

        [Fact]
        public async Task GetCertificates_CertNumberNewest_IsSortDescending()
        {
            await _controller.GetCertificates(sortBy: "cert-number-newest");
            _capturedFilter!.SortDescending.Should().BeTrue();
        }

        [Fact]
        public async Task GetCertificates_CompanyAZ_IsNotSortDescending()
        {
            await _controller.GetCertificates(sortBy: "company-az");
            _capturedFilter!.SortDescending.Should().BeFalse();
        }

        #endregion
    }
}
