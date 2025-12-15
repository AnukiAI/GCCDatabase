using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using Moq;
using FluentAssertions;
using GCCWebApp.Services;
using GCCWebApp.Models;
using GCCWebApp.Data;
using GCCWebApp.Repositories;

namespace GCCWebApp.Tests.Services
{
    /// <summary>
    /// Comprehensive tests for CertificateWorkflowService
    /// Tests the 14-state workflow machine and all transitions
    /// </summary>
    public class CertificateWorkflowServiceTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly Mock<ICertificateRepository> _certificateRepoMock;
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<ILogger<CertificateWorkflowService>> _loggerMock;
        private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock;
        private readonly CertificateWorkflowService _service;

        public CertificateWorkflowServiceTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new ApplicationDbContext(options);
            _certificateRepoMock = new Mock<ICertificateRepository>();
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _loggerMock = new Mock<ILogger<CertificateWorkflowService>>();
            _httpContextAccessorMock = new Mock<IHttpContextAccessor>();

            // Setup default HttpContext
            var httpContext = new DefaultHttpContext();
            _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);

            _service = new CertificateWorkflowService(
                _certificateRepoMock.Object,
                _unitOfWorkMock.Object,
                _loggerMock.Object,
                _context,
                _httpContextAccessorMock.Object
            );

            SeedTestData();
        }

        private void SeedTestData()
        {
            var users = new List<User>
            {
                new User { Id = 1, Username = "admin", Email = "admin@test.com", PasswordHash = "hash", IsActive = true, Status = "active", TwoFactorMethod = "Email" },
                new User { Id = 2, Username = "analyst", Email = "analyst@test.com", PasswordHash = "hash", IsActive = true, Status = "active", TwoFactorMethod = "Email" },
                new User { Id = 3, Username = "external", Email = "external@test.com", PasswordHash = "hash", IsActive = true, Status = "active", TwoFactorMethod = "Email" }
            };

            _context.Users.AddRange(users);
            _context.SaveChanges();
        }

        #region CanTransitionToStatusAsync Tests

        [Theory]
        [InlineData("Draft", "PendingReview", "Admin", true)]
        [InlineData("Draft", "Cancelled", "Admin", true)]
        [InlineData("Draft", "Active", "Admin", false)] // Invalid transition
        [InlineData("PendingReview", "UnderReview", "Admin", true)]
        [InlineData("PendingReview", "Draft", "Admin", true)]
        [InlineData("PendingReview", "OnHold", "Admin", true)]
        [InlineData("UnderReview", "Approved", "Admin", true)]
        [InlineData("UnderReview", "Rejected", "Admin", true)]
        [InlineData("Approved", "Active", "Admin", true)]
        [InlineData("Active", "Suspended", "Admin", true)]
        [InlineData("Active", "Expired", "Admin", true)]
        [InlineData("Active", "PendingRenewal", "Admin", true)]
        [InlineData("Active", "Revoked", "Admin", true)]
        [InlineData("Suspended", "Active", "Admin", true)]
        [InlineData("Revoked", "Archived", "Admin", true)]
        [InlineData("Revoked", "Active", "Admin", false)] // Invalid - Revoked can only go to Archived
        [InlineData("Expired", "PendingRenewal", "Admin", true)]
        [InlineData("Expired", "Active", "Admin", true)] // Re-activation allowed
        [InlineData("PendingRenewal", "Renewed", "Admin", true)]
        [InlineData("Renewed", "Active", "Admin", true)]
        [InlineData("Cancelled", "Draft", "Admin", false)] // Final state - no transitions
        [InlineData("Archived", "Active", "Admin", false)] // Final state - no transitions
        [InlineData("OnHold", "PendingReview", "Admin", true)]
        [InlineData("Rejected", "Draft", "Admin", true)] // Can restart from rejection
        public async Task CanTransitionToStatusAsync_ShouldValidateTransitions(
            string fromStatus, string toStatus, string role, bool expectedResult)
        {
            // Arrange
            var certificate = new Certificate
            {
                Id = 1,
                Number = "TEST-001",
                Status = fromStatus
            };

            _certificateRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(certificate);

            // Act
            var result = await _service.CanTransitionToStatusAsync(1, toStatus, role);

            // Assert
            result.Should().Be(expectedResult,
                $"Transition from '{fromStatus}' to '{toStatus}' for role '{role}' should be {expectedResult}");
        }

        [Fact]
        public async Task CanTransitionToStatusAsync_WithNonExistentCertificate_ShouldReturnFalse()
        {
            // Arrange
            _certificateRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Certificate?)null);

            // Act
            var result = await _service.CanTransitionToStatusAsync(999, "Active", "Admin");

            // Assert
            result.Should().BeFalse();
        }

        [Theory]
        // External users only have Draft and PendingReview in their allowed statuses
        [InlineData("Draft", "External", "PendingReview", true)] // External can submit for review
        [InlineData("PendingReview", "External", "Draft", true)] // External can return to draft
        [InlineData("UnderReview", "External", "Approved", false)] // External cannot approve
        [InlineData("UnderReview", "External", "Rejected", false)] // External cannot reject
        // Analyst cannot set Rejected or Revoked
        [InlineData("UnderReview", "Analyst", "Approved", true)] // Analyst can approve
        [InlineData("UnderReview", "Analyst", "Rejected", false)] // Analyst cannot reject
        [InlineData("Active", "Analyst", "Revoked", false)] // Analyst cannot revoke
        // Admin can do anything (that's a valid transition)
        [InlineData("UnderReview", "Admin", "Rejected", true)] // Admin can reject
        [InlineData("Active", "Admin", "Revoked", true)] // Admin can revoke
        public async Task CanTransitionToStatusAsync_ShouldRespectRolePermissions(
            string fromStatus, string role, string toStatus, bool expectedResult)
        {
            // Arrange
            var certificate = new Certificate
            {
                Id = 1,
                Number = "TEST-001",
                Status = fromStatus
            };

            _certificateRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(certificate);

            // Act
            var result = await _service.CanTransitionToStatusAsync(1, toStatus, role);

            // Assert
            result.Should().Be(expectedResult,
                $"Role '{role}' transitioning from '{fromStatus}' to '{toStatus}' should be {expectedResult}");
        }

        #endregion

        #region TransitionCertificateStatusAsync Tests

        [Fact]
        public async Task TransitionCertificateStatusAsync_WithValidTransition_ShouldSucceed()
        {
            // Arrange
            var certificate = new Certificate
            {
                Id = 1,
                Number = "TEST-001",
                Status = "Draft"
            };

            _certificateRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(certificate);
            _certificateRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Certificate>())).ReturnsAsync((Certificate c) => c);
            _unitOfWorkMock.Setup(u => u.CompleteAsync()).ReturnsAsync(1);

            // Act
            var (success, errorMessage) = await _service.TransitionCertificateStatusAsync(1, "PendingReview", 1);

            // Assert
            success.Should().BeTrue();
            errorMessage.Should().Be("Status updated successfully");
            certificate.Status.Should().Be("PendingReview");
            certificate.UpdatedBy.Should().Be(1);
        }

        [Fact]
        public async Task TransitionCertificateStatusAsync_WithInvalidTransition_ShouldFail()
        {
            // Arrange
            var certificate = new Certificate
            {
                Id = 1,
                Number = "TEST-001",
                Status = "Draft"
            };

            _certificateRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(certificate);

            // Act - Try to go directly from Draft to Active (invalid)
            var (success, errorMessage) = await _service.TransitionCertificateStatusAsync(1, "Active", 1);

            // Assert
            success.Should().BeFalse();
            errorMessage.Should().Contain("Invalid status transition");
        }

        [Fact]
        public async Task TransitionCertificateStatusAsync_WithNonExistentCertificate_ShouldFail()
        {
            // Arrange
            _certificateRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Certificate?)null);

            // Act
            var (success, errorMessage) = await _service.TransitionCertificateStatusAsync(999, "Active", 1);

            // Assert
            success.Should().BeFalse();
            errorMessage.Should().Be("Certificate not found");
        }

        [Fact]
        public async Task TransitionCertificateStatusAsync_ShouldLogStatusChange()
        {
            // Arrange
            var certificate = new Certificate
            {
                Id = 1,
                Number = "TEST-001",
                Status = "Approved"
            };

            _certificateRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(certificate);
            _certificateRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Certificate>())).ReturnsAsync((Certificate c) => c);
            _unitOfWorkMock.Setup(u => u.CompleteAsync()).ReturnsAsync(1);

            // Act
            await _service.TransitionCertificateStatusAsync(1, "Active", 1);

            // Assert - Check audit history was created
            var historyEntry = await _context.CertificateStatusHistory
                .FirstOrDefaultAsync(h => h.CertificateId == 1);

            historyEntry.Should().NotBeNull();
            historyEntry!.PreviousStatus.Should().Be("Approved");
            historyEntry.NewStatus.Should().Be("Active");
            historyEntry.ChangedBy.Should().Be(1);
            historyEntry.IsAutomatedTransition.Should().BeFalse();
        }

        [Theory]
        [InlineData("Draft", "PendingReview")]
        [InlineData("PendingReview", "UnderReview")]
        [InlineData("UnderReview", "Approved")]
        [InlineData("Approved", "Active")]
        public async Task TransitionCertificateStatusAsync_HappyPath_ShouldCompleteFullWorkflow(
            string fromStatus, string toStatus)
        {
            // Arrange
            var certificate = new Certificate
            {
                Id = 1,
                Number = "TEST-001",
                Status = fromStatus
            };

            _certificateRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(certificate);
            _certificateRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Certificate>())).ReturnsAsync((Certificate c) => c);
            _unitOfWorkMock.Setup(u => u.CompleteAsync()).ReturnsAsync(1);

            // Act
            var (success, _) = await _service.TransitionCertificateStatusAsync(1, toStatus, 1);

            // Assert
            success.Should().BeTrue($"Transition from '{fromStatus}' to '{toStatus}' should succeed");
            certificate.Status.Should().Be(toStatus);
        }

        #endregion

        #region GetAvailableStatusesForCertificateAsync Tests

        [Theory]
        [InlineData("Draft", "Admin", new[] { "PendingReview", "Cancelled" })]
        [InlineData("PendingReview", "Admin", new[] { "UnderReview", "Draft", "Cancelled", "OnHold" })]
        [InlineData("Active", "Admin", new[] { "Suspended", "Expired", "PendingRenewal", "Revoked", "Archived" })]
        [InlineData("Cancelled", "Admin", new string[] { })] // Final state
        [InlineData("Archived", "Admin", new string[] { })] // Final state
        public async Task GetAvailableStatusesForCertificateAsync_ShouldReturnValidTransitions(
            string currentStatus, string role, string[] expectedStatuses)
        {
            // Arrange
            var certificate = new Certificate
            {
                Id = 1,
                Number = "TEST-001",
                Status = currentStatus
            };

            _certificateRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(certificate);

            // Act
            var result = await _service.GetAvailableStatusesForCertificateAsync(1, role);

            // Assert
            result.Should().BeEquivalentTo(expectedStatuses,
                $"Available statuses from '{currentStatus}' for '{role}' should match expected");
        }

        [Fact]
        public async Task GetAvailableStatusesForCertificateAsync_WithNonExistentCertificate_ShouldReturnEmpty()
        {
            // Arrange
            _certificateRepoMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Certificate?)null);

            // Act
            var result = await _service.GetAvailableStatusesForCertificateAsync(999, "Admin");

            // Assert
            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetAvailableStatusesForCertificateAsync_ExternalRole_ShouldHaveLimitedOptions()
        {
            // Arrange
            var certificate = new Certificate
            {
                Id = 1,
                Number = "TEST-001",
                Status = "Draft"
            };

            _certificateRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(certificate);

            // Act
            var result = await _service.GetAvailableStatusesForCertificateAsync(1, "External");

            // Assert
            // External can only use Draft and PendingReview statuses
            result.Should().OnlyContain(s => s == "PendingReview" || s == "Cancelled");
            result.Should().NotContain("Active");
            result.Should().NotContain("Rejected");
        }

        #endregion

        #region CheckAndUpdateExpiringCertificatesAsync Tests

        [Fact]
        public async Task CheckAndUpdateExpiringCertificatesAsync_ShouldExpirePastDueCertificates()
        {
            // Arrange
            var expiredCertificate = new Certificate
            {
                Id = 1,
                Number = "EXPIRED-001",
                Status = "Active",
                ExpirationDate = DateTime.UtcNow.AddDays(-5) // Already expired
            };

            _certificateRepoMock.Setup(r => r.GetExpiringCertificatesAsync(It.IsAny<DateTime>()))
                .ReturnsAsync(new List<Certificate> { expiredCertificate });
            _certificateRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Certificate>())).ReturnsAsync((Certificate c) => c);
            _unitOfWorkMock.Setup(u => u.CompleteAsync()).ReturnsAsync(1);

            // Act
            await _service.CheckAndUpdateExpiringCertificatesAsync();

            // Assert
            expiredCertificate.Status.Should().Be("Expired");
            _certificateRepoMock.Verify(r => r.UpdateAsync(expiredCertificate), Times.Once);
        }

        [Fact]
        public async Task CheckAndUpdateExpiringCertificatesAsync_ShouldSetPendingRenewalFor30DayWarning()
        {
            // Arrange
            var expiringSoonCertificate = new Certificate
            {
                Id = 2,
                Number = "EXPIRING-001",
                Status = "Active",
                ExpirationDate = DateTime.UtcNow.AddDays(15) // Expiring in 15 days (within 30-day window)
            };

            _certificateRepoMock.Setup(r => r.GetExpiringCertificatesAsync(It.IsAny<DateTime>()))
                .ReturnsAsync(new List<Certificate> { expiringSoonCertificate });
            _certificateRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Certificate>())).ReturnsAsync((Certificate c) => c);
            _unitOfWorkMock.Setup(u => u.CompleteAsync()).ReturnsAsync(1);

            // Act
            await _service.CheckAndUpdateExpiringCertificatesAsync();

            // Assert
            expiringSoonCertificate.Status.Should().Be("PendingRenewal");
        }

        [Fact]
        public async Task CheckAndUpdateExpiringCertificatesAsync_ShouldNotChangeCertificatesAlreadyExpired()
        {
            // Arrange - Certificate already in Expired status
            var alreadyExpiredCertificate = new Certificate
            {
                Id = 3,
                Number = "ALREADY-EXPIRED",
                Status = "Expired",
                ExpirationDate = DateTime.UtcNow.AddDays(-30)
            };

            _certificateRepoMock.Setup(r => r.GetExpiringCertificatesAsync(It.IsAny<DateTime>()))
                .ReturnsAsync(new List<Certificate> { alreadyExpiredCertificate });
            _unitOfWorkMock.Setup(u => u.CompleteAsync()).ReturnsAsync(1);

            // Act
            await _service.CheckAndUpdateExpiringCertificatesAsync();

            // Assert - Should not update since it's already Expired
            _certificateRepoMock.Verify(r => r.UpdateAsync(alreadyExpiredCertificate), Times.Never);
        }

        [Fact]
        public async Task CheckAndUpdateExpiringCertificatesAsync_ShouldLogAutomatedTransitions()
        {
            // Arrange
            var expiredCertificate = new Certificate
            {
                Id = 1,
                Number = "AUTO-EXPIRE-001",
                Status = "Active",
                ExpirationDate = DateTime.UtcNow.AddDays(-1)
            };

            _certificateRepoMock.Setup(r => r.GetExpiringCertificatesAsync(It.IsAny<DateTime>()))
                .ReturnsAsync(new List<Certificate> { expiredCertificate });
            _certificateRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Certificate>())).ReturnsAsync((Certificate c) => c);
            _unitOfWorkMock.Setup(u => u.CompleteAsync()).ReturnsAsync(1);

            // Act
            await _service.CheckAndUpdateExpiringCertificatesAsync();

            // Assert - Check audit history was created with automated flag
            var historyEntry = await _context.CertificateStatusHistory
                .FirstOrDefaultAsync(h => h.CertificateId == 1);

            historyEntry.Should().NotBeNull();
            historyEntry!.IsAutomatedTransition.Should().BeTrue();
            historyEntry.ChangeSource.Should().Be("Automated");
            historyEntry.ChangedBy.Should().Be(0); // System user
        }

        #endregion

        #region GetStatusStatisticsAsync Tests

        [Fact]
        public async Task GetStatusStatisticsAsync_ShouldReturnStatistics()
        {
            // Arrange
            var expectedStats = new Dictionary<string, int>
            {
                { "Active", 10 },
                { "Draft", 5 },
                { "Expired", 3 }
            };

            _certificateRepoMock.Setup(r => r.GetCertificateStatisticsAsync())
                .ReturnsAsync(expectedStats);

            // Act
            var result = await _service.GetStatusStatisticsAsync();

            // Assert
            result.Should().BeEquivalentTo(expectedStats);
        }

        [Fact]
        public async Task GetStatusStatisticsAsync_WhenRepositoryThrows_ShouldReturnEmptyDictionary()
        {
            // Arrange
            _certificateRepoMock.Setup(r => r.GetCertificateStatisticsAsync())
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _service.GetStatusStatisticsAsync();

            // Assert
            result.Should().BeEmpty();
        }

        #endregion

        #region Full Workflow Integration Tests

        [Fact]
        public async Task FullWorkflow_DraftToActive_ShouldCompleteSuccessfully()
        {
            // This test validates the complete happy path workflow
            // Draft -> PendingReview -> UnderReview -> Approved -> Active

            var certificate = new Certificate
            {
                Id = 1,
                Number = "WORKFLOW-001",
                Status = "Draft"
            };

            _certificateRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(certificate);
            _certificateRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Certificate>())).ReturnsAsync((Certificate c) => c);
            _unitOfWorkMock.Setup(u => u.CompleteAsync()).ReturnsAsync(1);

            // Step 1: Draft -> PendingReview
            var (success1, _) = await _service.TransitionCertificateStatusAsync(1, "PendingReview", 1);
            success1.Should().BeTrue("Step 1: Draft -> PendingReview");
            certificate.Status.Should().Be("PendingReview");

            // Step 2: PendingReview -> UnderReview
            var (success2, _) = await _service.TransitionCertificateStatusAsync(1, "UnderReview", 1);
            success2.Should().BeTrue("Step 2: PendingReview -> UnderReview");
            certificate.Status.Should().Be("UnderReview");

            // Step 3: UnderReview -> Approved
            var (success3, _) = await _service.TransitionCertificateStatusAsync(1, "Approved", 1);
            success3.Should().BeTrue("Step 3: UnderReview -> Approved");
            certificate.Status.Should().Be("Approved");

            // Step 4: Approved -> Active
            var (success4, _) = await _service.TransitionCertificateStatusAsync(1, "Active", 1);
            success4.Should().BeTrue("Step 4: Approved -> Active");
            certificate.Status.Should().Be("Active");
        }

        [Fact]
        public async Task RenewalWorkflow_ActiveToRenewedToActive_ShouldComplete()
        {
            // Active -> PendingRenewal -> Renewed -> Active

            var certificate = new Certificate
            {
                Id = 1,
                Number = "RENEWAL-001",
                Status = "Active"
            };

            _certificateRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(certificate);
            _certificateRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Certificate>())).ReturnsAsync((Certificate c) => c);
            _unitOfWorkMock.Setup(u => u.CompleteAsync()).ReturnsAsync(1);

            // Active -> PendingRenewal
            var (success1, _) = await _service.TransitionCertificateStatusAsync(1, "PendingRenewal", 1);
            success1.Should().BeTrue();

            // PendingRenewal -> Renewed
            var (success2, _) = await _service.TransitionCertificateStatusAsync(1, "Renewed", 1);
            success2.Should().BeTrue();

            // Renewed -> Active
            var (success3, _) = await _service.TransitionCertificateStatusAsync(1, "Active", 1);
            success3.Should().BeTrue();
            certificate.Status.Should().Be("Active");
        }

        [Fact]
        public async Task RejectionWorkflow_UnderReviewToRejectedToDraft_ShouldAllow()
        {
            // Certificate can be rejected and then restarted

            var certificate = new Certificate
            {
                Id = 1,
                Number = "REJECT-001",
                Status = "UnderReview"
            };

            _certificateRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(certificate);
            _certificateRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Certificate>())).ReturnsAsync((Certificate c) => c);
            _unitOfWorkMock.Setup(u => u.CompleteAsync()).ReturnsAsync(1);

            // UnderReview -> Rejected
            var (success1, _) = await _service.TransitionCertificateStatusAsync(1, "Rejected", 1);
            success1.Should().BeTrue();
            certificate.Status.Should().Be("Rejected");

            // Rejected -> Draft (restart workflow)
            var (success2, _) = await _service.TransitionCertificateStatusAsync(1, "Draft", 1);
            success2.Should().BeTrue();
            certificate.Status.Should().Be("Draft");
        }

        [Fact]
        public async Task SuspensionWorkflow_ActiveToSuspendedToActive_ShouldAllow()
        {
            // Certificate can be suspended and reinstated

            var certificate = new Certificate
            {
                Id = 1,
                Number = "SUSPEND-001",
                Status = "Active"
            };

            _certificateRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(certificate);
            _certificateRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Certificate>())).ReturnsAsync((Certificate c) => c);
            _unitOfWorkMock.Setup(u => u.CompleteAsync()).ReturnsAsync(1);

            // Active -> Suspended
            var (success1, _) = await _service.TransitionCertificateStatusAsync(1, "Suspended", 1);
            success1.Should().BeTrue();

            // Suspended -> Active (reinstate)
            var (success2, _) = await _service.TransitionCertificateStatusAsync(1, "Active", 1);
            success2.Should().BeTrue();
            certificate.Status.Should().Be("Active");
        }

        #endregion

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}
