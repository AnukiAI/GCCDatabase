using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using FluentAssertions;
using GCCWebApp.Data;
using GCCWebApp.Models;
using GCCWebApp.Services;
using GCCWebApp.Repositories;
using GCCWebApp.Api.Tests.TestUtilities;
using Microsoft.EntityFrameworkCore;

namespace GCCWebApp.Api.Tests.Unit.Services
{
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
            _context = TestDbContextFactory.CreateInMemoryContext();
            _certificateRepoMock = new Mock<ICertificateRepository>();
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _loggerMock = new Mock<ILogger<CertificateWorkflowService>>();
            _httpContextAccessorMock = new Mock<IHttpContextAccessor>();

            _service = new CertificateWorkflowService(
                _certificateRepoMock.Object,
                _unitOfWorkMock.Object,
                _loggerMock.Object,
                _context,
                _httpContextAccessorMock.Object);

            SeedTestData();
        }

        public void Dispose()
        {
            _context.Dispose();
        }

        private void SeedTestData()
        {
            var user = new User
            {
                Id = 1,
                Username = "testadmin",
                Email = "admin@test.com",
                PasswordHash = "hashed",
                Status = "active",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                TwoFactorMethod = "Email"
            };
            _context.Users.Add(user);
            _context.SaveChanges();
        }

        private Certificate CreateTestCertificate(int id, string status, DateTime? expirationDate = null)
        {
            return new Certificate
            {
                Id = id,
                Number = $"CERT-{id:D4}",
                Status = status,
                CompanyId = 1,
                ProductId = 1,
                CertificationTypeId = 1,
                EffectiveDate = DateTime.UtcNow.AddDays(-60),
                ExpirationDate = expirationDate ?? DateTime.UtcNow.AddDays(300),
                Partner = "None",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
        }

        #region CanTransitionToStatusAsync Tests

        [Fact]
        public async Task CanTransitionToStatusAsync_CertificateNotFound_ReturnsFalse()
        {
            // Arrange
            _certificateRepoMock.Setup(r => r.GetByIdAsync(999))
                .ReturnsAsync((Certificate?)null);

            // Act
            var result = await _service.CanTransitionToStatusAsync(999, "PendingReview", "Admin");

            // Assert
            result.Should().BeFalse();
        }

        [Theory]
        [InlineData("Draft", "PendingReview", "Admin", true)]
        [InlineData("Draft", "Cancelled", "Admin", true)]
        [InlineData("Draft", "Active", "Admin", false)] // Invalid transition
        [InlineData("PendingReview", "UnderReview", "Admin", true)]
        [InlineData("PendingReview", "Draft", "Admin", true)]
        [InlineData("PendingReview", "Cancelled", "Admin", true)]
        [InlineData("PendingReview", "OnHold", "Admin", true)]
        [InlineData("UnderReview", "Approved", "Admin", true)]
        [InlineData("UnderReview", "PendingReview", "Admin", true)]
        [InlineData("UnderReview", "Rejected", "Admin", true)]
        [InlineData("UnderReview", "OnHold", "Admin", true)]
        [InlineData("Approved", "Active", "Admin", true)]
        [InlineData("Approved", "UnderReview", "Admin", true)]
        [InlineData("Approved", "OnHold", "Admin", true)]
        [InlineData("Active", "Suspended", "Admin", true)]
        [InlineData("Active", "Expired", "Admin", true)]
        [InlineData("Active", "PendingRenewal", "Admin", true)]
        [InlineData("Active", "Revoked", "Admin", true)]
        [InlineData("Active", "Archived", "Admin", true)]
        [InlineData("Suspended", "Active", "Admin", true)]
        [InlineData("Suspended", "Revoked", "Admin", true)]
        [InlineData("Suspended", "Cancelled", "Admin", true)]
        [InlineData("Suspended", "Archived", "Admin", true)]
        [InlineData("Revoked", "Archived", "Admin", true)]
        [InlineData("Revoked", "Active", "Admin", false)] // Invalid transition
        [InlineData("Expired", "PendingRenewal", "Admin", true)]
        [InlineData("Expired", "Archived", "Admin", true)]
        [InlineData("Expired", "Active", "Admin", true)]
        [InlineData("PendingRenewal", "UnderReview", "Admin", true)]
        [InlineData("PendingRenewal", "Renewed", "Admin", true)]
        [InlineData("PendingRenewal", "Expired", "Admin", true)]
        [InlineData("PendingRenewal", "Cancelled", "Admin", true)]
        [InlineData("Renewed", "Active", "Admin", true)]
        [InlineData("Renewed", "Archived", "Admin", false)] // Invalid transition
        [InlineData("Cancelled", "Draft", "Admin", false)] // Final state
        [InlineData("Cancelled", "Active", "Admin", false)] // Final state
        [InlineData("Archived", "Draft", "Admin", false)] // Final state
        [InlineData("Archived", "Active", "Admin", false)] // Final state
        [InlineData("OnHold", "PendingReview", "Admin", true)]
        [InlineData("OnHold", "UnderReview", "Admin", true)]
        [InlineData("OnHold", "Approved", "Admin", true)]
        [InlineData("OnHold", "Cancelled", "Admin", true)]
        [InlineData("Rejected", "Draft", "Admin", true)]
        [InlineData("Rejected", "Archived", "Admin", true)]
        [InlineData("Rejected", "Active", "Admin", false)] // Invalid transition
        public async Task CanTransitionToStatusAsync_AdminRole_ValidatesTransition(
            string fromStatus, string toStatus, string role, bool expected)
        {
            // Arrange
            var cert = CreateTestCertificate(1, fromStatus);
            _certificateRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(cert);

            // Act
            var result = await _service.CanTransitionToStatusAsync(1, toStatus, role);

            // Assert
            result.Should().Be(expected);
        }

        [Theory]
        [InlineData("UnderReview", "Rejected", "Analyst", false)] // Analyst cannot set Rejected
        [InlineData("Active", "Revoked", "Analyst", false)] // Analyst cannot set Revoked
        [InlineData("UnderReview", "Approved", "Analyst", true)] // Analyst can approve
        [InlineData("Draft", "PendingReview", "Analyst", true)] // Analyst can submit for review
        public async Task CanTransitionToStatusAsync_AnalystRole_EnforcesRoleRestrictions(
            string fromStatus, string toStatus, string role, bool expected)
        {
            // Arrange
            var cert = CreateTestCertificate(1, fromStatus);
            _certificateRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(cert);

            // Act
            var result = await _service.CanTransitionToStatusAsync(1, toStatus, role);

            // Assert
            result.Should().Be(expected);
        }

        [Theory]
        [InlineData("Draft", "PendingReview", "External", true)] // External can submit for review
        [InlineData("Draft", "Cancelled", "External", false)] // External cannot cancel (not in creation statuses)
        [InlineData("Active", "Suspended", "External", false)] // External has no access to Active transitions
        [InlineData("UnderReview", "Approved", "External", false)] // External cannot approve
        public async Task CanTransitionToStatusAsync_ExternalRole_LimitedToCreationStatuses(
            string fromStatus, string toStatus, string role, bool expected)
        {
            // Arrange
            var cert = CreateTestCertificate(1, fromStatus);
            _certificateRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(cert);

            // Act
            var result = await _service.CanTransitionToStatusAsync(1, toStatus, role);

            // Assert
            result.Should().Be(expected);
        }

        #endregion

        #region TransitionCertificateStatusAsync Tests

        [Fact]
        public async Task TransitionCertificateStatusAsync_CertificateNotFound_ReturnsFailure()
        {
            // Arrange
            _certificateRepoMock.Setup(r => r.GetByIdAsync(999))
                .ReturnsAsync((Certificate?)null);

            // Act
            var (success, errorMessage) = await _service.TransitionCertificateStatusAsync(999, "PendingReview", 1);

            // Assert
            success.Should().BeFalse();
            errorMessage.Should().Be("Certificate not found");
        }

        [Fact]
        public async Task TransitionCertificateStatusAsync_InvalidTransition_ReturnsFailure()
        {
            // Arrange - Draft cannot go directly to Active
            var cert = CreateTestCertificate(1, "Draft");
            _certificateRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(cert);

            // Act
            var (success, errorMessage) = await _service.TransitionCertificateStatusAsync(1, "Active", 1);

            // Assert
            success.Should().BeFalse();
            errorMessage.Should().Contain("Invalid status transition");
            errorMessage.Should().Contain("Draft");
            errorMessage.Should().Contain("Active");
        }

        [Fact]
        public async Task TransitionCertificateStatusAsync_ValidTransition_UpdatesCertificate()
        {
            // Arrange
            var cert = CreateTestCertificate(1, "Draft");
            _certificateRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(cert);
            _certificateRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Certificate>()))
                .ReturnsAsync((Certificate c) => c);
            _unitOfWorkMock.Setup(u => u.CompleteAsync()).ReturnsAsync(1);

            // Act
            var (success, errorMessage) = await _service.TransitionCertificateStatusAsync(1, "PendingReview", 1);

            // Assert
            success.Should().BeTrue();
            errorMessage.Should().Be("Status updated successfully");
            cert.Status.Should().Be("PendingReview");
            cert.UpdatedBy.Should().Be(1);
        }

        [Fact]
        public async Task TransitionCertificateStatusAsync_ValidTransition_CallsRepositoryUpdate()
        {
            // Arrange
            var cert = CreateTestCertificate(1, "Draft");
            _certificateRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(cert);
            _certificateRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Certificate>()))
                .ReturnsAsync((Certificate c) => c);
            _unitOfWorkMock.Setup(u => u.CompleteAsync()).ReturnsAsync(1);

            // Act
            await _service.TransitionCertificateStatusAsync(1, "PendingReview", 1);

            // Assert
            _certificateRepoMock.Verify(r => r.UpdateAsync(It.Is<Certificate>(
                c => c.Status == "PendingReview")), Times.Once);
            _unitOfWorkMock.Verify(u => u.CompleteAsync(), Times.Once);
        }

        [Fact]
        public async Task TransitionCertificateStatusAsync_ValidTransition_LogsAuditHistory()
        {
            // Arrange
            var cert = CreateTestCertificate(1, "Draft");
            _certificateRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(cert);
            _certificateRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Certificate>()))
                .ReturnsAsync((Certificate c) => c);
            _unitOfWorkMock.Setup(u => u.CompleteAsync()).ReturnsAsync(1);

            // Act
            await _service.TransitionCertificateStatusAsync(1, "PendingReview", 1);

            // Assert - Verify audit history was logged to the database
            var historyEntry = await _context.CertificateStatusHistory
                .FirstOrDefaultAsync(h => h.CertificateId == 1);
            historyEntry.Should().NotBeNull();
            historyEntry!.PreviousStatus.Should().Be("Draft");
            historyEntry.NewStatus.Should().Be("PendingReview");
            historyEntry.ChangedBy.Should().Be(1);
            historyEntry.IsAutomatedTransition.Should().BeFalse();
            historyEntry.ChangeSource.Should().Be("Manual");
        }

        [Fact]
        public async Task TransitionCertificateStatusAsync_WithKnownUser_IncludesUsername()
        {
            // Arrange
            var cert = CreateTestCertificate(1, "Draft");
            _certificateRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(cert);
            _certificateRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Certificate>()))
                .ReturnsAsync((Certificate c) => c);
            _unitOfWorkMock.Setup(u => u.CompleteAsync()).ReturnsAsync(1);

            // Act - userId 1 matches the seeded "testadmin" user
            await _service.TransitionCertificateStatusAsync(1, "PendingReview", 1);

            // Assert
            var historyEntry = await _context.CertificateStatusHistory
                .FirstOrDefaultAsync(h => h.CertificateId == 1);
            historyEntry.Should().NotBeNull();
            historyEntry!.ChangedByUsername.Should().Be("testadmin");
        }

        [Fact]
        public async Task TransitionCertificateStatusAsync_RepositoryThrows_ReturnsFailure()
        {
            // Arrange
            var cert = CreateTestCertificate(1, "Draft");
            _certificateRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(cert);
            _certificateRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Certificate>()))
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var (success, errorMessage) = await _service.TransitionCertificateStatusAsync(1, "PendingReview", 1);

            // Assert
            success.Should().BeFalse();
            errorMessage.Should().Be("An error occurred while updating the status");
        }

        [Fact]
        public async Task TransitionCertificateStatusAsync_FromCancelledState_AlwaysFails()
        {
            // Arrange - Cancelled is a final state with no valid transitions
            var cert = CreateTestCertificate(1, "Cancelled");
            _certificateRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(cert);

            // Act & Assert - Try every possible target status
            var allStatuses = CertificateStatusHelper.GetAllStatuses().Keys;
            foreach (var targetStatus in allStatuses)
            {
                var (success, _) = await _service.TransitionCertificateStatusAsync(1, targetStatus, 1);
                success.Should().BeFalse($"Cancelled should not transition to {targetStatus}");
            }
        }

        [Fact]
        public async Task TransitionCertificateStatusAsync_FromArchivedState_AlwaysFails()
        {
            // Arrange - Archived is a final state with no valid transitions
            var cert = CreateTestCertificate(1, "Archived");
            _certificateRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(cert);

            // Act & Assert - Try every possible target status
            var allStatuses = CertificateStatusHelper.GetAllStatuses().Keys;
            foreach (var targetStatus in allStatuses)
            {
                var (success, _) = await _service.TransitionCertificateStatusAsync(1, targetStatus, 1);
                success.Should().BeFalse($"Archived should not transition to {targetStatus}");
            }
        }

        [Fact]
        public async Task TransitionCertificateStatusAsync_FullHappyPath_DraftToActive()
        {
            // Arrange - Walk the full happy path: Draft → PendingReview → UnderReview → Approved → Active
            var cert = CreateTestCertificate(1, "Draft");
            _certificateRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(cert);
            _certificateRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Certificate>()))
                .ReturnsAsync((Certificate c) => c);
            _unitOfWorkMock.Setup(u => u.CompleteAsync()).ReturnsAsync(1);

            var transitions = new[] { "PendingReview", "UnderReview", "Approved", "Active" };

            foreach (var targetStatus in transitions)
            {
                // Act
                var (success, message) = await _service.TransitionCertificateStatusAsync(1, targetStatus, 1);

                // Assert
                success.Should().BeTrue($"Transition to {targetStatus} should succeed");
                cert.Status.Should().Be(targetStatus);
            }

            // Verify all 4 audit entries were created
            var historyCount = await _context.CertificateStatusHistory
                .CountAsync(h => h.CertificateId == 1);
            historyCount.Should().Be(4);
        }

        #endregion

        #region GetAvailableStatusesForCertificateAsync Tests

        [Fact]
        public async Task GetAvailableStatusesForCertificateAsync_CertificateNotFound_ReturnsEmptyList()
        {
            // Arrange
            _certificateRepoMock.Setup(r => r.GetByIdAsync(999))
                .ReturnsAsync((Certificate?)null);

            // Act
            var result = await _service.GetAvailableStatusesForCertificateAsync(999, "Admin");

            // Assert
            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetAvailableStatusesForCertificateAsync_DraftCert_AdminRole_ReturnsPendingReviewAndCancelled()
        {
            // Arrange
            var cert = CreateTestCertificate(1, "Draft");
            _certificateRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(cert);

            // Act
            var result = await _service.GetAvailableStatusesForCertificateAsync(1, "Admin");

            // Assert
            result.Should().Contain("PendingReview");
            result.Should().Contain("Cancelled");
            result.Should().HaveCount(2);
        }

        [Fact]
        public async Task GetAvailableStatusesForCertificateAsync_ActiveCert_AdminRole_ReturnsAllValidTransitions()
        {
            // Arrange
            var cert = CreateTestCertificate(1, "Active");
            _certificateRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(cert);

            // Act
            var result = await _service.GetAvailableStatusesForCertificateAsync(1, "Admin");

            // Assert
            result.Should().Contain("Suspended");
            result.Should().Contain("Expired");
            result.Should().Contain("PendingRenewal");
            result.Should().Contain("Revoked");
            result.Should().Contain("Archived");
            result.Should().HaveCount(5);
        }

        [Fact]
        public async Task GetAvailableStatusesForCertificateAsync_ActiveCert_AnalystRole_ExcludesRevoked()
        {
            // Arrange - Analyst cannot set Revoked status
            var cert = CreateTestCertificate(1, "Active");
            _certificateRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(cert);

            // Act
            var result = await _service.GetAvailableStatusesForCertificateAsync(1, "Analyst");

            // Assert
            result.Should().Contain("Suspended");
            result.Should().Contain("Expired");
            result.Should().Contain("PendingRenewal");
            result.Should().Contain("Archived");
            result.Should().NotContain("Revoked"); // Analyst restriction
        }

        [Fact]
        public async Task GetAvailableStatusesForCertificateAsync_DraftCert_ExternalRole_OnlyPendingReview()
        {
            // Arrange - External users can only access Draft and PendingReview statuses
            var cert = CreateTestCertificate(1, "Draft");
            _certificateRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(cert);

            // Act
            var result = await _service.GetAvailableStatusesForCertificateAsync(1, "External");

            // Assert
            result.Should().Contain("PendingReview");
            result.Should().NotContain("Cancelled"); // External can't cancel
            result.Should().HaveCount(1);
        }

        [Fact]
        public async Task GetAvailableStatusesForCertificateAsync_CancelledCert_AnyRole_ReturnsEmpty()
        {
            // Arrange - Cancelled is a final state
            var cert = CreateTestCertificate(1, "Cancelled");
            _certificateRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(cert);

            // Act
            var result = await _service.GetAvailableStatusesForCertificateAsync(1, "Admin");

            // Assert
            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetAvailableStatusesForCertificateAsync_ArchivedCert_AnyRole_ReturnsEmpty()
        {
            // Arrange - Archived is a final state
            var cert = CreateTestCertificate(1, "Archived");
            _certificateRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(cert);

            // Act
            var result = await _service.GetAvailableStatusesForCertificateAsync(1, "Admin");

            // Assert
            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetAvailableStatusesForCertificateAsync_UnderReviewCert_AnalystRole_ExcludesRejected()
        {
            // Arrange - Analyst cannot reject
            var cert = CreateTestCertificate(1, "UnderReview");
            _certificateRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(cert);

            // Act
            var result = await _service.GetAvailableStatusesForCertificateAsync(1, "Analyst");

            // Assert
            result.Should().Contain("Approved");
            result.Should().Contain("PendingReview");
            result.Should().Contain("OnHold");
            result.Should().NotContain("Rejected"); // Analyst restriction
        }

        #endregion

        #region CheckAndUpdateExpiringCertificatesAsync Tests

        [Fact]
        public async Task CheckAndUpdateExpiringCertificatesAsync_ExpiredCertificate_TransitionsToExpired()
        {
            // Arrange - Certificate with expiration in the past
            var expiredCert = CreateTestCertificate(1, "Active", DateTime.UtcNow.AddDays(-5));
            _certificateRepoMock.Setup(r => r.GetExpiringCertificatesAsync(It.IsAny<DateTime>()))
                .ReturnsAsync(new List<Certificate> { expiredCert });
            _certificateRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Certificate>()))
                .ReturnsAsync((Certificate c) => c);
            _unitOfWorkMock.Setup(u => u.CompleteAsync()).ReturnsAsync(1);

            // Act
            await _service.CheckAndUpdateExpiringCertificatesAsync();

            // Assert
            expiredCert.Status.Should().Be("Expired");
            _certificateRepoMock.Verify(r => r.UpdateAsync(It.Is<Certificate>(
                c => c.Status == "Expired")), Times.Once);
        }

        [Fact]
        public async Task CheckAndUpdateExpiringCertificatesAsync_ExpiringWithin30Days_TransitionsToPendingRenewal()
        {
            // Arrange - Active certificate expiring within 30 days (but not yet expired)
            var expiringCert = CreateTestCertificate(1, "Active", DateTime.UtcNow.AddDays(15));
            _certificateRepoMock.Setup(r => r.GetExpiringCertificatesAsync(It.IsAny<DateTime>()))
                .ReturnsAsync(new List<Certificate> { expiringCert });
            _certificateRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Certificate>()))
                .ReturnsAsync((Certificate c) => c);
            _unitOfWorkMock.Setup(u => u.CompleteAsync()).ReturnsAsync(1);

            // Act
            await _service.CheckAndUpdateExpiringCertificatesAsync();

            // Assert
            expiringCert.Status.Should().Be("PendingRenewal");
            _certificateRepoMock.Verify(r => r.UpdateAsync(It.Is<Certificate>(
                c => c.Status == "PendingRenewal")), Times.Once);
        }

        [Fact]
        public async Task CheckAndUpdateExpiringCertificatesAsync_PendingRenewalExpired_TransitionsToExpired()
        {
            // Arrange - PendingRenewal cert that has now actually expired
            var cert = CreateTestCertificate(1, "PendingRenewal", DateTime.UtcNow.AddDays(-1));
            _certificateRepoMock.Setup(r => r.GetExpiringCertificatesAsync(It.IsAny<DateTime>()))
                .ReturnsAsync(new List<Certificate> { cert });
            _certificateRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Certificate>()))
                .ReturnsAsync((Certificate c) => c);
            _unitOfWorkMock.Setup(u => u.CompleteAsync()).ReturnsAsync(1);

            // Act
            await _service.CheckAndUpdateExpiringCertificatesAsync();

            // Assert
            cert.Status.Should().Be("Expired");
        }

        [Fact]
        public async Task CheckAndUpdateExpiringCertificatesAsync_LogsAutomatedTransition()
        {
            // Arrange
            var expiredCert = CreateTestCertificate(1, "Active", DateTime.UtcNow.AddDays(-5));
            _certificateRepoMock.Setup(r => r.GetExpiringCertificatesAsync(It.IsAny<DateTime>()))
                .ReturnsAsync(new List<Certificate> { expiredCert });
            _certificateRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Certificate>()))
                .ReturnsAsync((Certificate c) => c);
            _unitOfWorkMock.Setup(u => u.CompleteAsync()).ReturnsAsync(1);

            // Act
            await _service.CheckAndUpdateExpiringCertificatesAsync();

            // Assert - Verify automated audit entry
            var historyEntry = await _context.CertificateStatusHistory
                .FirstOrDefaultAsync(h => h.CertificateId == 1);
            historyEntry.Should().NotBeNull();
            historyEntry!.IsAutomatedTransition.Should().BeTrue();
            historyEntry.ChangeSource.Should().Be("Automated");
            historyEntry.ChangedBy.Should().Be(0); // System user
        }

        [Fact]
        public async Task CheckAndUpdateExpiringCertificatesAsync_MultipleCertificates_ProcessesAll()
        {
            // Arrange - Mix of expired and expiring
            var expiredCert = CreateTestCertificate(1, "Active", DateTime.UtcNow.AddDays(-5));
            var expiringCert = CreateTestCertificate(2, "Active", DateTime.UtcNow.AddDays(15));
            _certificateRepoMock.Setup(r => r.GetExpiringCertificatesAsync(It.IsAny<DateTime>()))
                .ReturnsAsync(new List<Certificate> { expiredCert, expiringCert });
            _certificateRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Certificate>()))
                .ReturnsAsync((Certificate c) => c);
            _unitOfWorkMock.Setup(u => u.CompleteAsync()).ReturnsAsync(1);

            // Act
            await _service.CheckAndUpdateExpiringCertificatesAsync();

            // Assert
            expiredCert.Status.Should().Be("Expired");
            expiringCert.Status.Should().Be("PendingRenewal");
            _certificateRepoMock.Verify(r => r.UpdateAsync(It.IsAny<Certificate>()), Times.Exactly(2));
        }

        [Fact]
        public async Task CheckAndUpdateExpiringCertificatesAsync_NoCertificates_CompletesWithoutError()
        {
            // Arrange
            _certificateRepoMock.Setup(r => r.GetExpiringCertificatesAsync(It.IsAny<DateTime>()))
                .ReturnsAsync(new List<Certificate>());
            _unitOfWorkMock.Setup(u => u.CompleteAsync()).ReturnsAsync(1);

            // Act & Assert - Should not throw
            await _service.CheckAndUpdateExpiringCertificatesAsync();

            _unitOfWorkMock.Verify(u => u.CompleteAsync(), Times.Once);
        }

        [Fact]
        public async Task CheckAndUpdateExpiringCertificatesAsync_DraftCertExpired_DoesNotTransition()
        {
            // Arrange - Only Active and PendingRenewal certs should auto-expire
            var draftCert = CreateTestCertificate(1, "Draft", DateTime.UtcNow.AddDays(-5));
            _certificateRepoMock.Setup(r => r.GetExpiringCertificatesAsync(It.IsAny<DateTime>()))
                .ReturnsAsync(new List<Certificate> { draftCert });
            _unitOfWorkMock.Setup(u => u.CompleteAsync()).ReturnsAsync(1);

            // Act
            await _service.CheckAndUpdateExpiringCertificatesAsync();

            // Assert - Status should NOT change
            draftCert.Status.Should().Be("Draft");
            _certificateRepoMock.Verify(r => r.UpdateAsync(It.IsAny<Certificate>()), Times.Never);
        }

        [Fact]
        public async Task CheckAndUpdateExpiringCertificatesAsync_RepositoryThrows_DoesNotCrash()
        {
            // Arrange
            _certificateRepoMock.Setup(r => r.GetExpiringCertificatesAsync(It.IsAny<DateTime>()))
                .ThrowsAsync(new Exception("Database error"));

            // Act & Assert - Should swallow exception
            await _service.CheckAndUpdateExpiringCertificatesAsync();
        }

        #endregion

        #region GetStatusStatisticsAsync Tests

        [Fact]
        public async Task GetStatusStatisticsAsync_ReturnsStatisticsFromRepository()
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
        public async Task GetStatusStatisticsAsync_RepositoryThrows_ReturnsEmptyDictionary()
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
    }

    /// <summary>
    /// Tests for CertificateStatusHelper static methods — the 14-state workflow rules
    /// </summary>
    public class CertificateStatusHelperTests
    {
        #region GetAllStatuses Tests

        [Fact]
        public void GetAllStatuses_Returns14Statuses()
        {
            var statuses = CertificateStatusHelper.GetAllStatuses();
            statuses.Should().HaveCount(14);
        }

        [Theory]
        [InlineData("Draft", "Draft")]
        [InlineData("PendingReview", "Pending Review")]
        [InlineData("UnderReview", "Under Review")]
        [InlineData("Approved", "Approved")]
        [InlineData("Active", "Active")]
        [InlineData("Suspended", "Suspended")]
        [InlineData("Revoked", "Revoked")]
        [InlineData("Expired", "Expired")]
        [InlineData("PendingRenewal", "Pending Renewal")]
        [InlineData("Renewed", "Renewed")]
        [InlineData("Cancelled", "Cancelled")]
        [InlineData("Archived", "Archived")]
        [InlineData("OnHold", "On Hold")]
        [InlineData("Rejected", "Rejected")]
        public void GetAllStatuses_ContainsStatusWithCorrectDisplayName(string key, string expectedDisplayName)
        {
            var statuses = CertificateStatusHelper.GetAllStatuses();
            statuses.Should().ContainKey(key);
            statuses[key].Should().Be(expectedDisplayName);
        }

        #endregion

        #region GetCreationStatuses Tests

        [Fact]
        public void GetCreationStatuses_ReturnsOnlyDraftAndPendingReview()
        {
            var statuses = CertificateStatusHelper.GetCreationStatuses();
            statuses.Should().HaveCount(2);
            statuses.Should().ContainKey("Draft");
            statuses.Should().ContainKey("PendingReview");
        }

        #endregion

        #region GetStatusesForRole Tests

        [Fact]
        public void GetStatusesForRole_Admin_ReturnsAll14Statuses()
        {
            var statuses = CertificateStatusHelper.GetStatusesForRole("Admin");
            statuses.Should().HaveCount(14);
        }

        [Fact]
        public void GetStatusesForRole_Analyst_Excludes_RejectedAndRevoked()
        {
            var statuses = CertificateStatusHelper.GetStatusesForRole("Analyst");
            statuses.Should().HaveCount(12);
            statuses.Should().NotContainKey("Rejected");
            statuses.Should().NotContainKey("Revoked");
        }

        [Fact]
        public void GetStatusesForRole_External_OnlyCreationStatuses()
        {
            var statuses = CertificateStatusHelper.GetStatusesForRole("External");
            statuses.Should().HaveCount(2);
            statuses.Should().ContainKey("Draft");
            statuses.Should().ContainKey("PendingReview");
        }

        [Fact]
        public void GetStatusesForRole_UnknownRole_ReturnsCreationStatuses()
        {
            var statuses = CertificateStatusHelper.GetStatusesForRole("SomeRandomRole");
            statuses.Should().HaveCount(2);
            statuses.Should().ContainKey("Draft");
            statuses.Should().ContainKey("PendingReview");
        }

        [Fact]
        public void GetStatusesForRole_NullRole_ReturnsCreationStatuses()
        {
            var statuses = CertificateStatusHelper.GetStatusesForRole(null!);
            statuses.Should().HaveCount(2);
        }

        [Fact]
        public void GetStatusesForRole_CaseInsensitive()
        {
            var adminStatuses = CertificateStatusHelper.GetStatusesForRole("admin");
            var AdminStatuses = CertificateStatusHelper.GetStatusesForRole("Admin");
            var ADMINStatuses = CertificateStatusHelper.GetStatusesForRole("ADMIN");

            adminStatuses.Should().HaveCount(14);
            AdminStatuses.Should().HaveCount(14);
            ADMINStatuses.Should().HaveCount(14);
        }

        #endregion

        #region IsValidTransition Tests - Complete Transition Matrix

        [Theory]
        // Draft transitions
        [InlineData("Draft", "PendingReview", true)]
        [InlineData("Draft", "Cancelled", true)]
        [InlineData("Draft", "Active", false)]
        [InlineData("Draft", "Approved", false)]
        [InlineData("Draft", "UnderReview", false)]
        // PendingReview transitions
        [InlineData("PendingReview", "UnderReview", true)]
        [InlineData("PendingReview", "Draft", true)]
        [InlineData("PendingReview", "Cancelled", true)]
        [InlineData("PendingReview", "OnHold", true)]
        [InlineData("PendingReview", "Active", false)]
        [InlineData("PendingReview", "Approved", false)]
        // UnderReview transitions
        [InlineData("UnderReview", "Approved", true)]
        [InlineData("UnderReview", "PendingReview", true)]
        [InlineData("UnderReview", "Rejected", true)]
        [InlineData("UnderReview", "OnHold", true)]
        [InlineData("UnderReview", "Active", false)]
        [InlineData("UnderReview", "Draft", false)]
        // Approved transitions
        [InlineData("Approved", "Active", true)]
        [InlineData("Approved", "UnderReview", true)]
        [InlineData("Approved", "OnHold", true)]
        [InlineData("Approved", "Draft", false)]
        [InlineData("Approved", "Cancelled", false)]
        // Active transitions
        [InlineData("Active", "Suspended", true)]
        [InlineData("Active", "Expired", true)]
        [InlineData("Active", "PendingRenewal", true)]
        [InlineData("Active", "Revoked", true)]
        [InlineData("Active", "Archived", true)]
        [InlineData("Active", "Draft", false)]
        [InlineData("Active", "Approved", false)]
        // Suspended transitions
        [InlineData("Suspended", "Active", true)]
        [InlineData("Suspended", "Revoked", true)]
        [InlineData("Suspended", "Cancelled", true)]
        [InlineData("Suspended", "Archived", true)]
        [InlineData("Suspended", "Draft", false)]
        [InlineData("Suspended", "Approved", false)]
        // Revoked transitions
        [InlineData("Revoked", "Archived", true)]
        [InlineData("Revoked", "Active", false)]
        [InlineData("Revoked", "Draft", false)]
        [InlineData("Revoked", "Cancelled", false)]
        // Expired transitions
        [InlineData("Expired", "PendingRenewal", true)]
        [InlineData("Expired", "Archived", true)]
        [InlineData("Expired", "Active", true)]
        [InlineData("Expired", "Draft", false)]
        [InlineData("Expired", "Approved", false)]
        // PendingRenewal transitions
        [InlineData("PendingRenewal", "UnderReview", true)]
        [InlineData("PendingRenewal", "Renewed", true)]
        [InlineData("PendingRenewal", "Expired", true)]
        [InlineData("PendingRenewal", "Cancelled", true)]
        [InlineData("PendingRenewal", "Active", false)]
        [InlineData("PendingRenewal", "Draft", false)]
        // Renewed transitions
        [InlineData("Renewed", "Active", true)]
        [InlineData("Renewed", "Draft", false)]
        [InlineData("Renewed", "Archived", false)]
        [InlineData("Renewed", "Cancelled", false)]
        // Cancelled transitions (final state)
        [InlineData("Cancelled", "Draft", false)]
        [InlineData("Cancelled", "Active", false)]
        [InlineData("Cancelled", "Archived", false)]
        // Archived transitions (final state)
        [InlineData("Archived", "Draft", false)]
        [InlineData("Archived", "Active", false)]
        [InlineData("Archived", "Cancelled", false)]
        // OnHold transitions
        [InlineData("OnHold", "PendingReview", true)]
        [InlineData("OnHold", "UnderReview", true)]
        [InlineData("OnHold", "Approved", true)]
        [InlineData("OnHold", "Cancelled", true)]
        [InlineData("OnHold", "Active", false)]
        [InlineData("OnHold", "Draft", false)]
        // Rejected transitions
        [InlineData("Rejected", "Draft", true)]
        [InlineData("Rejected", "Archived", true)]
        [InlineData("Rejected", "Active", false)]
        [InlineData("Rejected", "Approved", false)]
        public void IsValidTransition_ReturnsExpectedResult(string from, string to, bool expected)
        {
            CertificateStatusHelper.IsValidTransition(from, to).Should().Be(expected,
                $"transition from '{from}' to '{to}' should be {expected}");
        }

        [Fact]
        public void IsValidTransition_UnknownFromStatus_ReturnsFalse()
        {
            CertificateStatusHelper.IsValidTransition("InvalidStatus", "Active").Should().BeFalse();
        }

        [Fact]
        public void IsValidTransition_SameStatus_ReturnsFalse()
        {
            // No self-transitions defined in the workflow
            var allStatuses = CertificateStatusHelper.GetAllStatuses().Keys;
            foreach (var status in allStatuses)
            {
                CertificateStatusHelper.IsValidTransition(status, status).Should().BeFalse(
                    $"self-transition from '{status}' to '{status}' should not be allowed");
            }
        }

        #endregion

        #region GetDisplayName Tests

        [Theory]
        [InlineData("Draft", "Draft")]
        [InlineData("PendingReview", "Pending Review")]
        [InlineData("UnderReview", "Under Review")]
        [InlineData("PendingRenewal", "Pending Renewal")]
        [InlineData("OnHold", "On Hold")]
        public void GetDisplayName_ReturnsHumanReadableName(string statusValue, string expectedDisplay)
        {
            CertificateStatusHelper.GetDisplayName(statusValue).Should().Be(expectedDisplay);
        }

        [Fact]
        public void GetDisplayName_UnknownStatus_ReturnsInputValue()
        {
            CertificateStatusHelper.GetDisplayName("UnknownStatus").Should().Be("UnknownStatus");
        }

        #endregion

        #region IsActiveStatus Tests

        [Theory]
        [InlineData("Active", true)]
        [InlineData("PendingRenewal", true)]
        [InlineData("Renewed", true)]
        [InlineData("Draft", false)]
        [InlineData("Expired", false)]
        [InlineData("Suspended", false)]
        [InlineData("Archived", false)]
        [InlineData("Cancelled", false)]
        public void IsActiveStatus_ReturnsExpectedResult(string status, bool expected)
        {
            CertificateStatusHelper.IsActiveStatus(status).Should().Be(expected);
        }

        #endregion

        #region IsFinalStatus Tests

        [Theory]
        [InlineData("Archived", true)]
        [InlineData("Cancelled", true)]
        [InlineData("Active", false)]
        [InlineData("Revoked", false)]
        [InlineData("Rejected", false)]
        [InlineData("Draft", false)]
        public void IsFinalStatus_ReturnsExpectedResult(string status, bool expected)
        {
            CertificateStatusHelper.IsFinalStatus(status).Should().Be(expected);
        }

        #endregion

        #region IsNegativeStatus Tests

        [Theory]
        [InlineData("Rejected", true)]
        [InlineData("Revoked", true)]
        [InlineData("Cancelled", true)]
        [InlineData("Active", false)]
        [InlineData("Draft", false)]
        [InlineData("Expired", false)]
        public void IsNegativeStatus_ReturnsExpectedResult(string status, bool expected)
        {
            CertificateStatusHelper.IsNegativeStatus(status).Should().Be(expected);
        }

        #endregion

        #region NeedsAttention Tests

        [Theory]
        [InlineData("PendingReview", true)]
        [InlineData("UnderReview", true)]
        [InlineData("OnHold", true)]
        [InlineData("PendingRenewal", true)]
        [InlineData("Expired", true)]
        [InlineData("Active", false)]
        [InlineData("Draft", false)]
        [InlineData("Archived", false)]
        [InlineData("Cancelled", false)]
        public void NeedsAttention_ReturnsExpectedResult(string status, bool expected)
        {
            CertificateStatusHelper.NeedsAttention(status).Should().Be(expected);
        }

        #endregion

        #region Workflow Consistency Tests

        [Fact]
        public void FinalStatuses_HaveNoOutboundTransitions()
        {
            var finalStatuses = CertificateStatusHelper.GetAllStatuses().Keys
                .Where(CertificateStatusHelper.IsFinalStatus);

            foreach (var finalStatus in finalStatuses)
            {
                var allTargets = CertificateStatusHelper.GetAllStatuses().Keys;
                foreach (var target in allTargets)
                {
                    CertificateStatusHelper.IsValidTransition(finalStatus, target).Should().BeFalse(
                        $"Final status '{finalStatus}' should not allow transition to '{target}'");
                }
            }
        }

        [Fact]
        public void AllStatuses_HaveTransitionRules()
        {
            // Every status should be a valid 'from' status in the transition map
            // (even if it has no valid transitions, like Cancelled/Archived)
            var allStatuses = CertificateStatusHelper.GetAllStatuses().Keys;

            foreach (var status in allStatuses)
            {
                // This tests that IsValidTransition doesn't crash for any status
                // and returns false for at least one non-matching transition
                var anyValid = allStatuses.Any(target =>
                    CertificateStatusHelper.IsValidTransition(status, target));

                // Only final statuses should have zero valid transitions
                if (!anyValid)
                {
                    CertificateStatusHelper.IsFinalStatus(status).Should().BeTrue(
                        $"Status '{status}' has no outbound transitions but is not marked as a final status");
                }
            }
        }

        [Fact]
        public void HappyPath_DraftToActive_IsReachable()
        {
            // Verify the primary workflow path works end-to-end
            CertificateStatusHelper.IsValidTransition("Draft", "PendingReview").Should().BeTrue();
            CertificateStatusHelper.IsValidTransition("PendingReview", "UnderReview").Should().BeTrue();
            CertificateStatusHelper.IsValidTransition("UnderReview", "Approved").Should().BeTrue();
            CertificateStatusHelper.IsValidTransition("Approved", "Active").Should().BeTrue();
        }

        [Fact]
        public void RenewalPath_ActiveToRenewedToActive_IsReachable()
        {
            // Verify the renewal workflow works
            CertificateStatusHelper.IsValidTransition("Active", "PendingRenewal").Should().BeTrue();
            CertificateStatusHelper.IsValidTransition("PendingRenewal", "Renewed").Should().BeTrue();
            CertificateStatusHelper.IsValidTransition("Renewed", "Active").Should().BeTrue();
        }

        [Fact]
        public void RejectionPath_CanReturnToDraft()
        {
            // Verify rejected certificates can restart the process
            CertificateStatusHelper.IsValidTransition("UnderReview", "Rejected").Should().BeTrue();
            CertificateStatusHelper.IsValidTransition("Rejected", "Draft").Should().BeTrue();
            CertificateStatusHelper.IsValidTransition("Draft", "PendingReview").Should().BeTrue();
        }

        #endregion
    }
}
