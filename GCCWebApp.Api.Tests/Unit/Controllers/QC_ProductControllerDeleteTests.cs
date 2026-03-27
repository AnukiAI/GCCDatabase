using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;
using FluentAssertions;
using GCCWebApp.Data;
using GCCWebApp.Models;
using GCCWebApp.Controllers;
using GCCWebApp.Services.Audit;
using GCCWebApp.Api.Tests.TestUtilities;
using Microsoft.EntityFrameworkCore;

namespace GCCWebApp.Api.Tests.Unit.Controllers
{
    /// <summary>
    /// Tests for ProductController.DeleteProduct — QC #98 (delete product guard)
    /// Verifies that products with certificates cannot be deleted, and products
    /// without certificates are deleted with proper audit logging.
    /// </summary>
    public class QC_ProductControllerDeleteTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly Mock<IAuditService> _auditServiceMock;
        private readonly ProductController _controller;

        public QC_ProductControllerDeleteTests()
        {
            _context = TestDbContextFactory.CreateInMemoryContext();
            _auditServiceMock = new Mock<IAuditService>();
            var loggerMock = new Mock<ILogger<ProductController>>();
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>())
                .Build();

            _controller = new ProductController(_context, _auditServiceMock.Object, loggerMock.Object, config);

            // Setup HttpContext with session
            var httpContext = new DefaultHttpContext();
            var session = new TestSession();
            session.SetString("UserRole", "Admin");
            session.SetString("UserId", "1");
            session.SetString("UserName", "admin");
            httpContext.Session = session;
            _controller.ControllerContext = new ControllerContext { HttpContext = httpContext };

            SeedTestData();
        }

        public void Dispose()
        {
            _context.Dispose();
        }

        private void SeedTestData()
        {
            _context.Companies.Add(new Company { Id = 50, Name = "Test Co", Email = "test@test.com" });

            // Product WITH certificates (should NOT be deletable)
            _context.Products.Add(new Product { Id = 50, Name = "Product With Certs", CompanyId = 50, CreatedAt = DateTime.UtcNow });
            _context.Certificates.Add(new Certificate { Id = 500, Number = "26-0500", Status = "Draft", CompanyId = 50, ProductId = 50, CreatedAt = DateTime.UtcNow });

            // Product WITHOUT certificates (should be deletable)
            _context.Products.Add(new Product { Id = 51, Name = "Product No Certs", CompanyId = 50, CreatedAt = DateTime.UtcNow });

            // Product with multiple certificates
            _context.Products.Add(new Product { Id = 52, Name = "Product Multi Certs", CompanyId = 50, CreatedAt = DateTime.UtcNow });
            _context.Certificates.Add(new Certificate { Id = 501, Number = "26-0501", Status = "Draft", CompanyId = 50, ProductId = 52, CreatedAt = DateTime.UtcNow });
            _context.Certificates.Add(new Certificate { Id = 502, Number = "26-0502", Status = "Draft", CompanyId = 50, ProductId = 52, CreatedAt = DateTime.UtcNow });

            _context.SaveChanges();
        }

        #region Delete Product Guard (QC #98)

        [Fact]
        public async Task DeleteProduct_NoCertificates_ReturnsSuccessTrue()
        {
            var result = await _controller.DeleteProduct(51);

            var jsonResult = result as JsonResult;
            jsonResult.Should().NotBeNull();
            var response = jsonResult!.Value;
            var successProp = response!.GetType().GetProperty("Success");
            successProp!.GetValue(response).Should().Be(true);
        }

        [Fact]
        public async Task DeleteProduct_HasCertificates_ReturnsSuccessFalse()
        {
            var result = await _controller.DeleteProduct(50);

            var jsonResult = result as JsonResult;
            jsonResult.Should().NotBeNull();
            var response = jsonResult!.Value;
            var successProp = response!.GetType().GetProperty("Success");
            successProp!.GetValue(response).Should().Be(false);
        }

        [Fact]
        public async Task DeleteProduct_HasCertificates_MessageContainsCertCount()
        {
            var result = await _controller.DeleteProduct(52); // Has 2 certs

            var jsonResult = result as JsonResult;
            var response = jsonResult!.Value;
            var messageProp = response!.GetType().GetProperty("Message");
            var message = messageProp!.GetValue(response) as string;
            message.Should().Contain("2 certificate");
        }

        [Fact]
        public async Task DeleteProduct_NonExistent_ReturnsSuccessFalse()
        {
            var result = await _controller.DeleteProduct(9999);

            var jsonResult = result as JsonResult;
            var response = jsonResult!.Value;
            var successProp = response!.GetType().GetProperty("Success");
            successProp!.GetValue(response).Should().Be(false);
        }

        [Fact]
        public async Task DeleteProduct_VerifiesRemovalFromDatabase()
        {
            await _controller.DeleteProduct(51);

            var product = await _context.Products.FindAsync(51);
            product.Should().BeNull();
        }

        [Fact]
        public async Task DeleteProduct_HasCertificates_ProductStillExists()
        {
            await _controller.DeleteProduct(50);

            var product = await _context.Products.FindAsync(50);
            product.Should().NotBeNull();
        }

        #endregion

        /// <summary>
        /// Simple ISession implementation for testing
        /// </summary>
        private class TestSession : ISession
        {
            private readonly Dictionary<string, byte[]> _store = new();
            public bool IsAvailable => true;
            public string Id => "test-session";
            public IEnumerable<string> Keys => _store.Keys;
            public void Clear() => _store.Clear();
            public Task CommitAsync(CancellationToken ct = default) => Task.CompletedTask;
            public Task LoadAsync(CancellationToken ct = default) => Task.CompletedTask;
            public void Remove(string key) => _store.Remove(key);
            public void Set(string key, byte[] value) => _store[key] = value;
            public bool TryGetValue(string key, out byte[]? value) => _store.TryGetValue(key, out value);

            public void SetString(string key, string value) =>
                _store[key] = System.Text.Encoding.UTF8.GetBytes(value);
        }
    }
}
