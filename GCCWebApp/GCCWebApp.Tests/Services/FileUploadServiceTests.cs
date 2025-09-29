using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using FluentAssertions;
using GCCWebApp.Services;
using GCCWebApp.Models;
using GCCWebApp.Data;
using System.Text;

namespace GCCWebApp.Tests.Services
{
    public class FileUploadServiceTests : IDisposable
    {
        private readonly Mock<ILogger<FileUploadService>> _loggerMock;
        private readonly Mock<IWebHostEnvironment> _webHostEnvironmentMock;
        private readonly ApplicationDbContext _context;
        private readonly FileUploadService _service;
        private readonly string _tempDirectory;

        public FileUploadServiceTests()
        {
            _loggerMock = new Mock<ILogger<FileUploadService>>();
            _webHostEnvironmentMock = new Mock<IWebHostEnvironment>();
            
            // Create temp directory for testing
            _tempDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(_tempDirectory);
            
            _webHostEnvironmentMock.Setup(x => x.WebRootPath).Returns(_tempDirectory);

            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new ApplicationDbContext(options);

            _service = new FileUploadService(_loggerMock.Object, _webHostEnvironmentMock.Object, _context);
        }

        #region Helper Methods

        private Mock<IFormFile> CreateMockFile(string fileName, string contentType, byte[] content)
        {
            var fileMock = new Mock<IFormFile>();
            var stream = new MemoryStream(content);
            
            fileMock.Setup(x => x.FileName).Returns(fileName);
            fileMock.Setup(x => x.ContentType).Returns(contentType);
            fileMock.Setup(x => x.Length).Returns(content.Length);
            fileMock.Setup(x => x.OpenReadStream()).Returns(stream);
            fileMock.Setup(x => x.CopyToAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
                .Returns((Stream target, CancellationToken token) =>
                {
                    stream.Position = 0;
                    return stream.CopyToAsync(target, token);
                });
            
            return fileMock;
        }

        private byte[] CreatePdfContent()
        {
            // Simple PDF header for testing
            var pdfHeader = "%PDF-1.4\n1 0 obj\n<<\n/Type /Catalog\n/Pages 2 0 R\n>>\nendobj";
            return Encoding.UTF8.GetBytes(pdfHeader);
        }

        private byte[] CreateImageContent()
        {
            // Simple JPEG header for testing
            var jpegHeader = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, 0x49, 0x46 };
            return jpegHeader;
        }

        #endregion

        #region UploadCertificateAsync Tests

        [Fact]
        public async Task UploadCertificateAsync_WithValidPdfFile_ShouldSucceed()
        {
            // Arrange
            var pdfContent = CreatePdfContent();
            var fileMock = CreateMockFile("test-certificate.pdf", "application/pdf", pdfContent);
            var certificateNumber = "CERT-001";

            // Act
            var result = await _service.UploadCertificateAsync(fileMock.Object, certificateNumber);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.FileName.Should().StartWith(certificateNumber);
            result.FileName.Should().EndWith(".pdf");
            result.FileSize.Should().Be(pdfContent.Length);
            result.FilePath.Should().StartWith("uploads/certificates/");
            result.Errors.Should().BeEmpty();
        }

        [Fact]
        public async Task UploadCertificateAsync_WithInvalidFileType_ShouldFail()
        {
            // Arrange
            var txtContent = Encoding.UTF8.GetBytes("This is a text file");
            var fileMock = CreateMockFile("test.txt", "text/plain", txtContent);
            var certificateNumber = "CERT-001";

            // Act
            var result = await _service.UploadCertificateAsync(fileMock.Object, certificateNumber);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.Errors.Should().Contain("Invalid file type. Only PDF files are allowed for certificates.");
        }

        [Fact]
        public async Task UploadCertificateAsync_WithOversizedFile_ShouldFail()
        {
            // Arrange
            var largeContent = new byte[25 * 1024 * 1024]; // 25MB (exceeds 20MB limit)
            var fileMock = CreateMockFile("large-certificate.pdf", "application/pdf", largeContent);
            var certificateNumber = "CERT-001";

            // Act
            var result = await _service.UploadCertificateAsync(fileMock.Object, certificateNumber);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.Errors.Should().Contain("File size exceeds the maximum limit of 20MB.");
        }

        #endregion

        #region UploadImageAsync Tests

        [Fact]
        public async Task UploadImageAsync_WithValidJpegFile_ShouldSucceed()
        {
            // Arrange
            var imageContent = CreateImageContent();
            var fileMock = CreateMockFile("test-image.jpg", "image/jpeg", imageContent);
            var identifier = "IMG-001";

            // Act
            var result = await _service.UploadImageAsync(fileMock.Object, identifier);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.FileName.Should().StartWith(identifier);
            result.FileName.Should().EndWith(".jpg");
            result.FileSize.Should().Be(imageContent.Length);
            result.FilePath.Should().StartWith("uploads/images/");
            result.Errors.Should().BeEmpty();
        }

        [Fact]
        public async Task UploadImageAsync_WithInvalidFileType_ShouldFail()
        {
            // Arrange
            var txtContent = Encoding.UTF8.GetBytes("This is not an image");
            var fileMock = CreateMockFile("not-image.txt", "text/plain", txtContent);
            var identifier = "IMG-001";

            // Act
            var result = await _service.UploadImageAsync(fileMock.Object, identifier);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.Errors.Should().Contain("Invalid file type. Only JPG, PNG, and GIF images are allowed.");
        }

        [Fact]
        public async Task UploadImageAsync_WithOversizedFile_ShouldFail()
        {
            // Arrange
            var largeContent = new byte[6 * 1024 * 1024]; // 6MB (exceeds 5MB limit)
            var fileMock = CreateMockFile("large-image.jpg", "image/jpeg", largeContent);
            var identifier = "IMG-001";

            // Act
            var result = await _service.UploadImageAsync(fileMock.Object, identifier);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.Errors.Should().Contain("File size exceeds the maximum limit of 5MB.");
        }

        #endregion

        #region File Validation Tests

        [Theory]
        [InlineData("certificate.pdf", "application/pdf", FileType.Certificate, true)]
        [InlineData("document.doc", "application/msword", FileType.Certificate, false)]
        [InlineData("image.jpg", "image/jpeg", FileType.Image, true)]
        [InlineData("image.png", "image/png", FileType.Image, true)]
        [InlineData("image.gif", "image/gif", FileType.Image, true)]
        [InlineData("document.pdf", "application/pdf", FileType.Image, false)]
        public void IsValidFileType_ShouldValidateCorrectly(string fileName, string contentType, FileType fileType, bool expectedResult)
        {
            // Arrange
            var content = Encoding.UTF8.GetBytes("test content");
            var fileMock = CreateMockFile(fileName, contentType, content);

            // Act
            var result = _service.IsValidFileType(fileMock.Object, fileType);

            // Assert
            result.Should().Be(expectedResult);
        }

        [Theory]
        [InlineData(1024, 2048, true)]      // 1KB file, 2KB limit
        [InlineData(2048, 1024, false)]     // 2KB file, 1KB limit
        [InlineData(0, 1024, false)]        // Empty file
        [InlineData(1024, 1024, true)]      // Exact size match
        public void IsValidFileSize_ShouldValidateCorrectly(long fileSize, long maxSize, bool expectedResult)
        {
            // Arrange
            var content = new byte[fileSize];
            var fileMock = CreateMockFile("test.txt", "text/plain", content);

            // Act
            var result = _service.IsValidFileSize(fileMock.Object, maxSize);

            // Assert
            result.Should().Be(expectedResult);
        }

        [Fact]
        public void ValidateFile_WithValidFile_ShouldPass()
        {
            // Arrange
            var content = CreatePdfContent();
            var fileMock = CreateMockFile("test.pdf", "application/pdf", content);
            var config = new FileUploadConfiguration
            {
                MaxFileSize = 10 * 1024 * 1024, // 10MB
                AllowedExtensions = new[] { ".pdf" },
                AllowedContentTypes = new[] { "application/pdf" },
                DangerousExtensions = new[] { ".exe", ".bat" }
            };

            // Act
            var result = _service.ValidateFile(fileMock.Object, config);

            // Assert
            result.Should().NotBeNull();
            result.IsValid.Should().BeTrue();
            result.Errors.Should().BeEmpty();
        }

        [Fact]
        public void ValidateFile_WithOversizedFile_ShouldFail()
        {
            // Arrange
            var largeContent = new byte[2048];
            var fileMock = CreateMockFile("test.pdf", "application/pdf", largeContent);
            var config = new FileUploadConfiguration
            {
                MaxFileSize = 1024, // 1KB limit
                AllowedExtensions = new[] { ".pdf" },
                AllowedContentTypes = new[] { "application/pdf" }
            };

            // Act
            var result = _service.ValidateFile(fileMock.Object, config);

            // Assert
            result.Should().NotBeNull();
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.Contains("File size") && e.Contains("exceeds maximum"));
        }

        [Fact]
        public void ValidateFile_WithDisallowedExtension_ShouldFail()
        {
            // Arrange
            var content = Encoding.UTF8.GetBytes("test");
            var fileMock = CreateMockFile("test.txt", "text/plain", content);
            var config = new FileUploadConfiguration
            {
                MaxFileSize = 10 * 1024 * 1024,
                AllowedExtensions = new[] { ".pdf" },
                AllowedContentTypes = new[] { "application/pdf" }
            };

            // Act
            var result = _service.ValidateFile(fileMock.Object, config);

            // Assert
            result.Should().NotBeNull();
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.Contains("File extension '.txt' is not allowed"));
        }

        [Fact]
        public void ValidateFile_WithDangerousExtension_ShouldFail()
        {
            // Arrange
            var content = Encoding.UTF8.GetBytes("malicious content");
            var fileMock = CreateMockFile("malware.exe", "application/x-msdownload", content);
            var config = new FileUploadConfiguration
            {
                MaxFileSize = 10 * 1024 * 1024,
                AllowedExtensions = new[] { ".exe" }, // Allowed in config but dangerous
                AllowedContentTypes = new[] { "application/x-msdownload" },
                DangerousExtensions = new[] { ".exe" }
            };

            // Act
            var result = _service.ValidateFile(fileMock.Object, config);

            // Assert
            result.Should().NotBeNull();
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.Contains("potentially dangerous"));
        }

        [Fact]
        public void ValidateFile_WithMaliciousFileName_ShouldFail()
        {
            // Arrange
            var content = CreatePdfContent();
            var fileMock = CreateMockFile("../../../etc/passwd.pdf", "application/pdf", content);
            var config = new FileUploadConfiguration
            {
                MaxFileSize = 10 * 1024 * 1024,
                AllowedExtensions = new[] { ".pdf" },
                AllowedContentTypes = new[] { "application/pdf" }
            };

            // Act
            var result = _service.ValidateFile(fileMock.Object, config);

            // Assert
            result.Should().NotBeNull();
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain("Invalid file name.");
        }

        #endregion

        #region File Operations Tests

        [Fact]
        public async Task CalculateFileHashAsync_ShouldReturnConsistentHash()
        {
            // Arrange
            var content = Encoding.UTF8.GetBytes("test content for hashing");
            using var stream1 = new MemoryStream(content);
            using var stream2 = new MemoryStream(content);

            // Act
            var hash1 = await _service.CalculateFileHashAsync(stream1);
            var hash2 = await _service.CalculateFileHashAsync(stream2);

            // Assert
            hash1.Should().NotBeNullOrEmpty();
            hash2.Should().NotBeNullOrEmpty();
            hash1.Should().Be(hash2, "Same content should produce same hash");
            hash1.Should().MatchRegex(@"^[a-f0-9]+$", "Hash should be hexadecimal");
        }

        [Fact]
        public async Task ScanFileForVirusesAsync_ShouldAlwaysReturnTrue()
        {
            // Arrange
            var filePath = "test-file.pdf";

            // Act
            var result = await _service.ScanFileForVirusesAsync(filePath);

            // Assert
            result.Should().BeTrue("Placeholder implementation should always return true");
        }

        [Fact]
        public void GetFileUrl_WithValidPath_ShouldReturnCorrectUrl()
        {
            // Arrange
            var filePath = @"uploads\certificates\test.pdf";

            // Act
            var result = _service.GetFileUrl(filePath);

            // Assert
            result.Should().Be("/uploads/certificates/test.pdf");
        }

        [Fact]
        public void GetFileUrl_WithEmptyPath_ShouldReturnEmpty()
        {
            // Act
            var result = _service.GetFileUrl("");

            // Assert
            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetFileAsync_WithNonExistentFile_ShouldReturnNull()
        {
            // Arrange
            var nonExistentPath = "uploads/certificates/non-existent.pdf";

            // Act
            var result = await _service.GetFileAsync(nonExistentPath);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task DeleteFileAsync_WithNonExistentFile_ShouldReturnFalse()
        {
            // Arrange
            var nonExistentPath = "uploads/certificates/non-existent.pdf";

            // Act
            var result = await _service.DeleteFileAsync(nonExistentPath);

            // Assert
            result.Should().BeFalse();
        }

        #endregion

        #region UploadCertificateDocumentAsync Tests

        [Fact]
        public async Task UploadCertificateDocumentAsync_WithValidFile_ShouldCreateDatabaseRecord()
        {
            // Arrange
            var pdfContent = CreatePdfContent();
            var fileMock = CreateMockFile("test-document.pdf", "application/pdf", pdfContent);
            var certificateId = 1;
            var documentType = "Certificate";
            var description = "Test document";
            var uploadedBy = 1;

            // Act
            var result = await _service.UploadCertificateDocumentAsync(
                fileMock.Object, certificateId, documentType, description, uploadedBy);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.DocumentId.Should().HaveValue();
            result.FileHash.Should().NotBeNullOrEmpty();

            // Verify database record was created
            var document = await _context.Set<CertificateDocument>().FindAsync(result.DocumentId!.Value);
            document.Should().NotBeNull();
            document.CertificateId.Should().Be(certificateId);
            document.DocumentType.Should().Be(documentType);
            document.Description.Should().Be(description);
            document.UploadedBy.Should().Be(uploadedBy);
            document.FileHash.Should().Be(result.FileHash);
        }

        #endregion

        #region Security Tests

        [Theory]
        [InlineData("malware.exe")]
        [InlineData("script.bat")]
        [InlineData("virus.scr")]
        [InlineData("trojan.com")]
        public async Task UploadCertificateAsync_WithMaliciousFiles_ShouldReject(string fileName)
        {
            // Arrange
            var content = Encoding.UTF8.GetBytes("malicious content");
            var fileMock = CreateMockFile(fileName, "application/octet-stream", content);
            var certificateNumber = "CERT-001";

            // Act
            var result = await _service.UploadCertificateAsync(fileMock.Object, certificateNumber);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.Errors.Should().NotBeEmpty();
        }

        [Fact]
        public void ValidateFile_WithNullFile_ShouldFail()
        {
            // Arrange
            var config = new FileUploadConfiguration();

            // Act
            var result = _service.ValidateFile(null!, config);

            // Assert
            result.Should().NotBeNull();
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain("No file was uploaded.");
        }

        [Fact]
        public void ValidateFile_WithEmptyFile_ShouldFail()
        {
            // Arrange
            var fileMock = CreateMockFile("empty.pdf", "application/pdf", Array.Empty<byte>());
            var config = new FileUploadConfiguration();

            // Act
            var result = _service.ValidateFile(fileMock.Object, config);

            // Assert
            result.Should().NotBeNull();
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain("No file was uploaded.");
        }

        #endregion

        public void Dispose()
        {
            _context.Dispose();
            
            // Clean up temp directory
            if (Directory.Exists(_tempDirectory))
            {
                try
                {
                    Directory.Delete(_tempDirectory, true);
                }
                catch
                {
                    // Ignore cleanup errors in tests
                }
            }
        }
    }
}