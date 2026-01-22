using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using GCCWebApp.Controllers;
using GCCWebApp.Data;
using GCCWebApp.Middleware;
using GCCWebApp.Models;
using GCCWebApp.Services;
using System.Security.Claims;

namespace GCCWebApp.Tests.Controllers
{
    /// <summary>
    /// Integration tests for AdminController Two-Factor Authentication management endpoints.
    /// Tests Reset2FA, Disable2FA, and Enable2FA controller actions.
    /// </summary>
    public class AdminController2FATests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly Mock<ILogger<AdminController>> _loggerMock;
        private readonly Mock<IAdminService> _adminServiceMock;
        private readonly AdminController _controller;

        public AdminController2FATests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: $"AdminController2FATests_{Guid.NewGuid()}")
                .Options;

            _context = new ApplicationDbContext(options);
            _loggerMock = new Mock<ILogger<AdminController>>();
            _adminServiceMock = new Mock<IAdminService>();
            _controller = new AdminController(_adminServiceMock.Object, _loggerMock.Object, _context, new RateLimitOptions());

            SetupAdminUser();
        }

        private void SetupAdminUser()
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, "admin"),
                new Claim(ClaimTypes.Email, "admin@test.com"),
                new Claim(ClaimTypes.Role, "Admin"),
                new Claim("UserId", "1")
            };

            var identity = new ClaimsIdentity(claims, "Test");
            var principal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal }
            };
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }

        private ApiResponse<object>? GetApiResponse(IActionResult result)
        {
            var jsonResult = result as JsonResult;
            return jsonResult?.Value as ApiResponse<object>;
        }

        #region Reset2FA Endpoint Tests

        [Fact]
        public async Task Reset2FA_ValidRequest_ReturnsSuccessJson()
        {
            // Arrange
            var request = new UserIdRequest { UserId = 1 };
            _adminServiceMock.Setup(s => s.Reset2FAAsync(1)).ReturnsAsync(true);

            // Act
            var result = await _controller.Reset2FA(request);

            // Assert
            var jsonResult = result.Should().BeOfType<JsonResult>().Subject;
            var response = GetApiResponse(result);
            response.Should().NotBeNull();
            response!.Success.Should().BeTrue();
            response.Message.Should().Contain("reset");
        }

        [Fact]
        public async Task Reset2FA_ServiceFails_ReturnsFailureJson()
        {
            // Arrange
            var request = new UserIdRequest { UserId = 999 };
            _adminServiceMock.Setup(s => s.Reset2FAAsync(999)).ReturnsAsync(false);

            // Act
            var result = await _controller.Reset2FA(request);

            // Assert
            var response = GetApiResponse(result);
            response.Should().NotBeNull();
            response!.Success.Should().BeFalse();
        }

        [Fact]
        public async Task Reset2FA_CallsServiceWithCorrectUserId()
        {
            // Arrange
            var request = new UserIdRequest { UserId = 42 };
            _adminServiceMock.Setup(s => s.Reset2FAAsync(It.IsAny<int>())).ReturnsAsync(true);

            // Act
            await _controller.Reset2FA(request);

            // Assert
            _adminServiceMock.Verify(s => s.Reset2FAAsync(42), Times.Once);
        }

        [Fact]
        public async Task Reset2FA_ServiceThrowsException_ReturnsFailure()
        {
            // Arrange
            var request = new UserIdRequest { UserId = 1 };
            _adminServiceMock.Setup(s => s.Reset2FAAsync(It.IsAny<int>()))
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.Reset2FA(request);

            // Assert
            var response = GetApiResponse(result);
            response.Should().NotBeNull();
            response!.Success.Should().BeFalse();
        }

        #endregion

        #region Disable2FA Endpoint Tests

        [Fact]
        public async Task Disable2FA_ValidRequest_ReturnsSuccessJson()
        {
            // Arrange
            var request = new UserIdRequest { UserId = 1 };
            _adminServiceMock.Setup(s => s.Disable2FAAsync(1)).ReturnsAsync(true);

            // Act
            var result = await _controller.Disable2FA(request);

            // Assert
            var response = GetApiResponse(result);
            response.Should().NotBeNull();
            response!.Success.Should().BeTrue();
            response.Message.Should().Contain("disabled");
        }

        [Fact]
        public async Task Disable2FA_ServiceFails_ReturnsFailureJson()
        {
            // Arrange
            var request = new UserIdRequest { UserId = 999 };
            _adminServiceMock.Setup(s => s.Disable2FAAsync(999)).ReturnsAsync(false);

            // Act
            var result = await _controller.Disable2FA(request);

            // Assert
            var response = GetApiResponse(result);
            response.Should().NotBeNull();
            response!.Success.Should().BeFalse();
        }

        [Fact]
        public async Task Disable2FA_CallsServiceWithCorrectUserId()
        {
            // Arrange
            var request = new UserIdRequest { UserId = 123 };
            _adminServiceMock.Setup(s => s.Disable2FAAsync(It.IsAny<int>())).ReturnsAsync(true);

            // Act
            await _controller.Disable2FA(request);

            // Assert
            _adminServiceMock.Verify(s => s.Disable2FAAsync(123), Times.Once);
        }

        [Fact]
        public async Task Disable2FA_ServiceThrowsException_ReturnsFailure()
        {
            // Arrange
            var request = new UserIdRequest { UserId = 1 };
            _adminServiceMock.Setup(s => s.Disable2FAAsync(It.IsAny<int>()))
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.Disable2FA(request);

            // Assert
            var response = GetApiResponse(result);
            response.Should().NotBeNull();
            response!.Success.Should().BeFalse();
        }

        #endregion

        #region Enable2FA Endpoint Tests

        [Fact]
        public async Task Enable2FA_ValidRequest_ReturnsSuccessJson()
        {
            // Arrange
            var request = new Enable2FARequest { UserId = 1, TwoFactorMethod = "TOTP" };
            _adminServiceMock.Setup(s => s.Enable2FAAsync(1, "TOTP")).ReturnsAsync(true);

            // Act
            var result = await _controller.Enable2FA(request);

            // Assert
            var response = GetApiResponse(result);
            response.Should().NotBeNull();
            response!.Success.Should().BeTrue();
            response.Message.Should().Contain("enabled");
        }

        [Fact]
        public async Task Enable2FA_WithEmailMethod_ReturnsSuccess()
        {
            // Arrange
            var request = new Enable2FARequest { UserId = 1, TwoFactorMethod = "Email" };
            _adminServiceMock.Setup(s => s.Enable2FAAsync(1, "Email")).ReturnsAsync(true);

            // Act
            var result = await _controller.Enable2FA(request);

            // Assert
            var response = GetApiResponse(result);
            response.Should().NotBeNull();
            response!.Success.Should().BeTrue();
        }

        [Fact]
        public async Task Enable2FA_NullMethod_UsesTOTPAsDefault()
        {
            // Arrange
            var request = new Enable2FARequest { UserId = 1, TwoFactorMethod = null };
            _adminServiceMock.Setup(s => s.Enable2FAAsync(1, "TOTP")).ReturnsAsync(true);

            // Act
            await _controller.Enable2FA(request);

            // Assert
            _adminServiceMock.Verify(s => s.Enable2FAAsync(1, "TOTP"), Times.Once);
        }

        [Fact]
        public async Task Enable2FA_ServiceFails_ReturnsFailureJson()
        {
            // Arrange
            var request = new Enable2FARequest { UserId = 999, TwoFactorMethod = "TOTP" };
            _adminServiceMock.Setup(s => s.Enable2FAAsync(999, "TOTP")).ReturnsAsync(false);

            // Act
            var result = await _controller.Enable2FA(request);

            // Assert
            var response = GetApiResponse(result);
            response.Should().NotBeNull();
            response!.Success.Should().BeFalse();
        }

        [Fact]
        public async Task Enable2FA_CallsServiceWithCorrectParameters()
        {
            // Arrange
            var request = new Enable2FARequest { UserId = 42, TwoFactorMethod = "SMS" };
            _adminServiceMock.Setup(s => s.Enable2FAAsync(It.IsAny<int>(), It.IsAny<string>()))
                .ReturnsAsync(true);

            // Act
            await _controller.Enable2FA(request);

            // Assert
            _adminServiceMock.Verify(s => s.Enable2FAAsync(42, "SMS"), Times.Once);
        }

        [Fact]
        public async Task Enable2FA_ServiceThrowsException_ReturnsFailure()
        {
            // Arrange
            var request = new Enable2FARequest { UserId = 1, TwoFactorMethod = "TOTP" };
            _adminServiceMock.Setup(s => s.Enable2FAAsync(It.IsAny<int>(), It.IsAny<string>()))
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.Enable2FA(request);

            // Assert
            var response = GetApiResponse(result);
            response.Should().NotBeNull();
            response!.Success.Should().BeFalse();
        }

        #endregion

        #region Authorization Tests

        [Fact]
        public void AdminController_ShouldHaveAuthorizeAttribute()
        {
            // Arrange & Act
            var controllerType = typeof(AdminController);
            var authorizeAttribute = controllerType.GetCustomAttributes(typeof(AuthorizeAttribute), true);

            // Assert
            authorizeAttribute.Should().NotBeEmpty();
        }

        [Fact]
        public void AdminController_ShouldRequireAdminOnlyPolicy()
        {
            // Arrange & Act
            var controllerType = typeof(AdminController);
            var authorizeAttributes = controllerType.GetCustomAttributes(typeof(AuthorizeAttribute), true)
                .Cast<AuthorizeAttribute>();

            // Assert
            authorizeAttributes.Should().Contain(a => a.Policy == "AdminOnly");
        }

        #endregion

        #region Request Validation Tests

        [Fact]
        public async Task Reset2FA_ZeroUserId_ServiceStillCalled()
        {
            // Arrange
            var request = new UserIdRequest { UserId = 0 };
            _adminServiceMock.Setup(s => s.Reset2FAAsync(0)).ReturnsAsync(false);

            // Act
            var result = await _controller.Reset2FA(request);

            // Assert
            _adminServiceMock.Verify(s => s.Reset2FAAsync(0), Times.Once);
            var response = GetApiResponse(result);
            response.Should().NotBeNull();
            response!.Success.Should().BeFalse();
        }

        [Fact]
        public async Task Disable2FA_NegativeUserId_ServiceStillCalled()
        {
            // Arrange
            var request = new UserIdRequest { UserId = -1 };
            _adminServiceMock.Setup(s => s.Disable2FAAsync(-1)).ReturnsAsync(false);

            // Act
            var result = await _controller.Disable2FA(request);

            // Assert
            _adminServiceMock.Verify(s => s.Disable2FAAsync(-1), Times.Once);
        }

        #endregion
    }
}
