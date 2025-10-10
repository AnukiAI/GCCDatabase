using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using FluentAssertions;
using GCCWebApp.Controllers;
using GCCWebApp.Data;
using GCCWebApp.Models;
using GCCWebApp.Services;
using System.Security.Claims;
using System.Text;

namespace GCCWebApp.Tests.Controllers
{
    public class DocumentControllerTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly Mock<IFileUploadService> _fileUploadServiceMock;
        private readonly Mock<ILogger<DocumentController>> _loggerMock;
        private readonly DocumentController _controller;

        public DocumentControllerTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new ApplicationDbContext(options);
            _fileUploadServiceMock = new Mock<IFileUploadService>();
            _loggerMock = new Mock<ILogger<DocumentController>>();
            
            _controller = new DocumentController(_context, _fileUploadServiceMock.Object, _loggerMock.Object);
            
            SeedTestData();
            SetupMockSession();
        }

        private void SeedTestData()
        {
            var companies = new List<Company>
            {
                new Company { Id = 1, Name = "Test Company", IsActive = true }
            };

            var categories = new List<ProductCategory>
            {
                new ProductCategory { Id = 1, Name = "Test Category", IsActive = true }
            };

            var products = new List<Product>
            {
                new Product { Id = 1, Name = "Test Product", CategoryId = 1, IsActive = true }
            };

            var certificates = new List<Certificate>
            {
                new Certificate
                {
                    Id = 1,
                    Number = "CERT-001",
                    CompanyId = 1,
                    ProductId = 1,
                    Status = "active",
                    EffectiveDate = DateTime.Now.AddDays(-30),
                    ExpirationDate = DateTime.Now.AddDays(60)
                }
            };

            var documents = new List<CertificateDocument>
            {
                new CertificateDocument
                {
                    Id = 1,
                    FileName = "test-document.pdf",
                    OriginalFileName = "Test Document.pdf",
                    FilePath = "uploads/certificates/test-document.pdf",
                    ContentType = "application/pdf",
                    FileSize = 1024,
                    DocumentType = "Certificate",
                    Description = "Test document",
                    CertificateId = 1,
                    IsActive = true,
                    IsPublic = false,
                    UploadedAt = DateTime.UtcNow
                },
                new CertificateDocument
                {
                    Id = 2,
                    FileName = "public-document.pdf",
                    OriginalFileName = "Public Document.pdf",
                    FilePath = "uploads/certificates/public-document.pdf",
                    ContentType = "application/pdf",
                    FileSize = 2048,
                    DocumentType = "Certificate",
                    Description = "Public document",
                    CertificateId = 1,
                    IsActive = true,
                    IsPublic = true,
                    UploadedAt = DateTime.UtcNow
                }
            };

            _context.Companies.AddRange(companies);
            _context.ProductCategories.AddRange(categories);
            _context.Products.AddRange(products);
            _context.Certificates.AddRange(certificates);
            _context.Set<CertificateDocument>().AddRange(documents);
            _context.SaveChanges();
        }

        private void SetupMockSession()
        {
            var mockSession = new Mock<ISession>();
            mockSession.Setup(x => x.GetString("UserRole")).Returns("Admin");
            
            var mockHttpContext = new Mock<HttpContext>();
            mockHttpContext.Setup(x => x.Session).Returns(mockSession.Object);
            
            _controller.ControllerContext = new ControllerContext()
            {
                HttpContext = mockHttpContext.Object
            };
        }

        private Mock<IFormFile> CreateMockFile(string fileName, string contentType, byte[] content)
        {
            var fileMock = new Mock<IFormFile>();
            var stream = new MemoryStream(content);
            
            fileMock.Setup(x => x.FileName).Returns(fileName);
            fileMock.Setup(x => x.ContentType).Returns(contentType);
            fileMock.Setup(x => x.Length).Returns(content.Length);
            fileMock.Setup(x => x.OpenReadStream()).Returns(stream);
            
            return fileMock;
        }

        #region Upload GET Tests

        [Fact]
        public async Task Upload_WithValidCertificateId_ShouldReturnViewWithModel()
        {
            // Act
            var result = await _controller.Upload(1);

            // Assert
            var viewResult = result.Should().BeOfType<ViewResult>().Subject;
            var model = viewResult.Model.Should().BeOfType<CertificateDocumentUploadViewModel>().Subject;
            model.CertificateId.Should().Be(1);
            viewResult.ViewData["Certificate"].Should().NotBeNull();
        }

        [Fact]
        public async Task Upload_WithInvalidCertificateId_ShouldReturnNotFound()
        {
            // Act
            var result = await _controller.Upload(999);

            // Assert
            result.Should().BeOfType<NotFoundResult>();
        }

        #endregion

        #region Upload POST Tests

        [Fact]
        public async Task Upload_WithValidModel_ShouldCallFileUploadServiceAndRedirect()
        {
            // Arrange
            var pdfContent = Encoding.UTF8.GetBytes("%PDF-1.4 test content");
            var fileMock = CreateMockFile("test.pdf", "application/pdf", pdfContent);
            var model = new CertificateDocumentUploadViewModel
            {
                CertificateId = 1,
                File = fileMock.Object,
                DocumentType = "Certificate",
                Description = "Test upload"
            };

            var uploadResult = new GCCWebApp.Services.FileUploadResult
            {
                IsSuccess = true,
                FilePath = "uploads/certificates/test.pdf",
                FileName = "test.pdf",
                FileSize = pdfContent.Length
            };

            _fileUploadServiceMock
                .Setup(x => x.UploadCertificateDocumentAsync(
                    It.IsAny<IFormFile>(),
                    It.IsAny<int>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<int?>()))
                .ReturnsAsync(uploadResult);

            // Act
            var result = await _controller.Upload(model);

            // Assert
            var redirectResult = result.Should().BeOfType<RedirectToActionResult>().Subject;
            redirectResult.ActionName.Should().Be("Details");
            redirectResult.ControllerName.Should().Be("Certificate");
            redirectResult.RouteValues!["id"].Should().Be(1);

            _fileUploadServiceMock.Verify(x => x.UploadCertificateDocumentAsync(
                model.File,
                model.CertificateId,
                model.DocumentType,
                model.Description,
                It.IsAny<int?>()), Times.Once);
        }

        [Fact]
        public async Task Upload_WithInvalidModel_ShouldReturnViewWithErrors()
        {
            // Arrange
            var model = new CertificateDocumentUploadViewModel
            {
                CertificateId = 1,
                File = null!, // Invalid - no file
                DocumentType = "Certificate"
            };

            _controller.ModelState.AddModelError("File", "Please select a file to upload");

            // Act
            var result = await _controller.Upload(model);

            // Assert
            var viewResult = result.Should().BeOfType<ViewResult>().Subject;
            viewResult.Model.Should().Be(model);
            viewResult.ViewData["Certificate"].Should().NotBeNull();
        }

        [Fact]
        public async Task Upload_WithUploadServiceFailure_ShouldReturnViewWithErrors()
        {
            // Arrange
            var pdfContent = Encoding.UTF8.GetBytes("%PDF-1.4 test content");
            var fileMock = CreateMockFile("test.pdf", "application/pdf", pdfContent);
            var model = new CertificateDocumentUploadViewModel
            {
                CertificateId = 1,
                File = fileMock.Object,
                DocumentType = "Certificate"
            };

            var uploadResult = new GCCWebApp.Services.FileUploadResult
            {
                IsSuccess = false
            };
            uploadResult.AddError("File upload failed");

            _fileUploadServiceMock
                .Setup(x => x.UploadCertificateDocumentAsync(
                    It.IsAny<IFormFile>(),
                    It.IsAny<int>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<int?>()))
                .ReturnsAsync(uploadResult);

            // Act
            var result = await _controller.Upload(model);

            // Assert
            var viewResult = result.Should().BeOfType<ViewResult>().Subject;
            _controller.ModelState.Should().ContainKey("");
            _controller.ModelState[""]!.Errors.Should().Contain(e => e.ErrorMessage.Contains("File upload failed"));
        }

        #endregion

        #region Download Tests

        [Fact]
        public async Task Download_WithValidPublicDocument_ShouldReturnFile()
        {
            // Arrange
            var fileContent = Encoding.UTF8.GetBytes("PDF content");
            _fileUploadServiceMock.Setup(x => x.GetFileAsync(It.IsAny<string>()))
                .ReturnsAsync(fileContent);

            // Act
            var result = await _controller.Download(2); // Public document

            // Assert
            var fileResult = result.Should().BeOfType<FileContentResult>().Subject;
            fileResult.FileContents.Should().Equal(fileContent);
            fileResult.ContentType.Should().Be("application/pdf");
            fileResult.FileDownloadName.Should().Be("Public Document.pdf");
        }

        [Fact]
        public async Task Download_WithValidPrivateDocumentAsAdmin_ShouldReturnFile()
        {
            // Arrange
            var fileContent = Encoding.UTF8.GetBytes("PDF content");
            _fileUploadServiceMock.Setup(x => x.GetFileAsync(It.IsAny<string>()))
                .ReturnsAsync(fileContent);

            // Act
            var result = await _controller.Download(1); // Private document, but user is Admin

            // Assert
            var fileResult = result.Should().BeOfType<FileContentResult>().Subject;
            fileResult.FileContents.Should().Equal(fileContent);
            fileResult.ContentType.Should().Be("application/pdf");
            fileResult.FileDownloadName.Should().Be("Test Document.pdf");
        }

        [Fact]
        public async Task Download_WithPrivateDocumentAsNonAdmin_ShouldReturnForbid()
        {
            // Arrange
            var mockSession = new Mock<ISession>();
            mockSession.Setup(x => x.GetString("UserRole")).Returns("User"); // Non-admin role
            
            var mockHttpContext = new Mock<HttpContext>();
            mockHttpContext.Setup(x => x.Session).Returns(mockSession.Object);
            
            _controller.ControllerContext.HttpContext = mockHttpContext.Object;

            // Act
            var result = await _controller.Download(1); // Private document

            // Assert
            result.Should().BeOfType<ForbidResult>();
        }

        [Fact]
        public async Task Download_WithNonExistentDocument_ShouldReturnNotFound()
        {
            // Act
            var result = await _controller.Download(999);

            // Assert
            result.Should().BeOfType<NotFoundResult>();
        }

        [Fact]
        public async Task Download_WithFileNotFoundOnDisk_ShouldReturnNotFound()
        {
            // Arrange
            _fileUploadServiceMock.Setup(x => x.GetFileAsync(It.IsAny<string>()))
                .ReturnsAsync((byte[]?)null);

            // Act
            var result = await _controller.Download(1);

            // Assert
            var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
            notFoundResult.Value.Should().Be("File not found.");
        }

        #endregion

        #region Preview Tests

        [Fact]
        public async Task Preview_WithValidDocument_ShouldReturnFile()
        {
            // Arrange
            var fileContent = Encoding.UTF8.GetBytes("PDF content");
            _fileUploadServiceMock.Setup(x => x.GetFileAsync(It.IsAny<string>()))
                .ReturnsAsync(fileContent);

            // Act
            var result = await _controller.Preview(2); // Public document

            // Assert
            var fileResult = result.Should().BeOfType<FileContentResult>().Subject;
            fileResult.FileContents.Should().Equal(fileContent);
            fileResult.ContentType.Should().Be("application/pdf");
        }

        [Fact]
        public async Task Preview_WithNonExistentDocument_ShouldReturnNotFound()
        {
            // Act
            var result = await _controller.Preview(999);

            // Assert
            result.Should().BeOfType<NotFoundResult>();
        }

        #endregion

        #region GetDocuments Tests

        [Fact]
        public async Task GetDocuments_WithValidCertificateId_ShouldReturnJsonWithDocuments()
        {
            // Act
            var result = await _controller.GetDocuments(1);

            // Assert
            var jsonResult = result.Should().BeOfType<JsonResult>().Subject;
            jsonResult.Value.Should().NotBeNull();
            
            // The result should be a list of documents
            var documents = jsonResult.Value as IEnumerable<object>;
            documents.Should().NotBeNull();
            documents!.Count().Should().Be(2); // Two documents seeded
        }

        [Fact]
        public async Task GetDocuments_WithInvalidCertificateId_ShouldReturnEmptyJson()
        {
            // Act
            var result = await _controller.GetDocuments(999);

            // Assert
            var jsonResult = result.Should().BeOfType<JsonResult>().Subject;
            var documents = jsonResult.Value as IEnumerable<object>;
            documents.Should().NotBeNull();
            documents!.Count().Should().Be(0);
        }

        #endregion

        #region Delete Tests

        [Fact]
        public async Task Delete_WithValidDocument_ShouldMarkAsInactiveAndReturnSuccess()
        {
            // Act
            var result = await _controller.Delete(1);

            // Assert
            var jsonResult = result.Should().BeOfType<JsonResult>().Subject;
            var response = jsonResult.Value as dynamic;
            response.Should().NotBeNull();

            // Verify document is marked as inactive
            var document = await _context.Set<CertificateDocument>().FindAsync(1);
            document.Should().NotBeNull();
            document!.IsActive.Should().BeFalse();

            _fileUploadServiceMock.Verify(x => x.DeleteFileAsync(It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task Delete_WithNonExistentDocument_ShouldReturnNotFound()
        {
            // Act
            var result = await _controller.Delete(999);

            // Assert
            result.Should().BeOfType<NotFoundResult>();
        }

        #endregion

        #region Authorization Tests

        [Fact]
        public async Task Upload_WithNonAnalystRole_ShouldDenyAccess()
        {
            // Arrange
            var mockSession = new Mock<ISession>();
            mockSession.Setup(x => x.GetString("UserRole")).Returns("User"); // Not Analyst or Admin
            
            var mockHttpContext = new Mock<HttpContext>();
            mockHttpContext.Setup(x => x.Session).Returns(mockSession.Object);
            
            _controller.ControllerContext.HttpContext = mockHttpContext.Object;

            // This test would need proper authorization setup to work fully
            // For now, we're testing the helper method logic
            var document = new CertificateDocument { IsPublic = false };
            
            // Use reflection to test private method
            var method = typeof(DocumentController).GetMethod("CanUserAccessDocument", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var canAccess = (bool)method!.Invoke(_controller, new object[] { document })!;
            
            canAccess.Should().BeFalse();
        }

        [Fact]
        public void IsPreviewableFile_WithSupportedContentTypes_ShouldReturnTrue()
        {
            // Use reflection to test private method
            var method = typeof(DocumentController).GetMethod("IsPreviewableFile", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            var testCases = new[]
            {
                "application/pdf",
                "image/jpeg",
                "image/png",
                "image/gif",
                "text/plain"
            };

            foreach (var contentType in testCases)
            {
                var result = (bool)method!.Invoke(_controller, new object[] { contentType })!;
                result.Should().BeTrue($"{contentType} should be previewable");
            }
        }

        [Fact]
        public void IsPreviewableFile_WithUnsupportedContentTypes_ShouldReturnFalse()
        {
            // Use reflection to test private method
            var method = typeof(DocumentController).GetMethod("IsPreviewableFile", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            var testCases = new[]
            {
                "application/msword",
                "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                "video/mp4",
                "audio/mpeg"
            };

            foreach (var contentType in testCases)
            {
                var result = (bool)method!.Invoke(_controller, new object[] { contentType })!;
                result.Should().BeFalse($"{contentType} should not be previewable");
            }
        }

        #endregion

        #region Error Handling Tests

        [Fact]
        public async Task Upload_WithException_ShouldLogErrorAndReturnView()
        {
            // Arrange
            var pdfContent = Encoding.UTF8.GetBytes("%PDF-1.4 test content");
            var fileMock = CreateMockFile("test.pdf", "application/pdf", pdfContent);
            var model = new CertificateDocumentUploadViewModel
            {
                CertificateId = 1,
                File = fileMock.Object,
                DocumentType = "Certificate"
            };

            _fileUploadServiceMock
                .Setup(x => x.UploadCertificateDocumentAsync(
                    It.IsAny<IFormFile>(),
                    It.IsAny<int>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<int?>()))
                .ThrowsAsync(new Exception("Upload service error"));

            // Act
            var result = await _controller.Upload(model);

            // Assert
            var viewResult = result.Should().BeOfType<ViewResult>().Subject;
            _controller.ModelState.Should().ContainKey("");
            _controller.ModelState[""]!.Errors.Should().Contain(e => 
                e.ErrorMessage.Contains("An error occurred while uploading the document"));

            _loggerMock.Verify(x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Error uploading document")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()
            ), Times.Once);
        }

        [Fact]
        public async Task Download_WithException_ShouldReturnInternalServerError()
        {
            // Arrange
            _fileUploadServiceMock.Setup(x => x.GetFileAsync(It.IsAny<string>()))
                .ThrowsAsync(new Exception("File service error"));

            // Act
            var result = await _controller.Download(1);

            // Assert
            var statusResult = result.Should().BeOfType<ObjectResult>().Subject;
            statusResult.StatusCode.Should().Be(500);
            statusResult.Value.Should().Be("An error occurred while downloading the file.");
        }

        #endregion

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}