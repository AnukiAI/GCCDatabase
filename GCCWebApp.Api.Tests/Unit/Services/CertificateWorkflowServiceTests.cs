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

            var productIdServiceMock = new Mock<IProductIdService>();
            _service = new CertificateWorkflowService(
                _certificateRepoMock.Object,
                _unitOfWorkMock.Object,
                _loggerMock.Object,
                _context,
                _httpContextAccessorMock.Object,
                productIdServiceMock.Object);

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
            var result = await _service.CanTransitionToStatusAsync(999, "Client Approved - Not Paid", "Admin");

            // Assert
            result.Should().BeFalse();
        }

        [Theory]
        // Draft transitions
        [InlineData("Draft", "Client Approved - Not Paid", "Admin", true)]
        [InlineData("Draft", "Client Approved - Confidential", "Admin", true)]
        [InlineData("Draft", "Client Approved - Paid", "Admin", true)]
        [InlineData("Draft", "Hold", "Admin", true)]
        [InlineData("Draft", "Expired", "Admin", false)] // Invalid transition
        [InlineData("Draft", "Republished", "Admin", false)] // Invalid transition
        // Client Approved - Not Paid transitions
        [InlineData("Client Approved - Not Paid", "Client Approved - Paid", "Admin", true)]
        [InlineData("Client Approved - Not Paid", "Client Approved - Confidential", "Admin", true)]
        [InlineData("Client Approved - Not Paid", "Hold", "Admin", true)]
        [InlineData("Client Approved - Not Paid", "Expired", "Admin", true)]
        [InlineData("Client Approved - Not Paid", "Draft", "Admin", false)] // Invalid transition
        // Client Approved - Confidential transitions
        [InlineData("Client Approved - Confidential", "Client Approved - Paid", "Admin", true)]
        [InlineData("Client Approved - Confidential", "Hold", "Admin", true)]
        [InlineData("Client Approved - Confidential", "Expired", "Admin", true)]
        [InlineData("Client Approved - Confidential", "Draft", "Admin", false)] // Invalid transition
        // Client Approved - Paid transitions
        [InlineData("Client Approved - Paid", "Hold", "Admin", true)]
        [InlineData("Client Approved - Paid", "Expired", "Admin", true)]
        [InlineData("Client Approved - Paid", "Republished", "Admin", true)]
        [InlineData("Client Approved - Paid", "Expired - No Annual Verification", "Admin", true)]
        [InlineData("Client Approved - Paid", "Draft", "Admin", false)] // Invalid transition
        // Hold transitions
        [InlineData("Hold", "Draft", "Admin", true)]
        [InlineData("Hold", "Client Approved - Not Paid", "Admin", true)]
        [InlineData("Hold", "Client Approved - Confidential", "Admin", true)]
        [InlineData("Hold", "Client Approved - Paid", "Admin", true)]
        [InlineData("Hold", "Expired", "Admin", false)] // Invalid transition
        // Expired transitions
        [InlineData("Expired", "Republished", "Admin", true)]
        [InlineData("Expired", "Draft", "Admin", true)]
        [InlineData("Expired", "Client Approved - Paid", "Admin", false)] // Invalid transition
        // Republished transitions
        [InlineData("Republished", "Expired", "Admin", true)]
        [InlineData("Republished", "Expired - No Annual Verification", "Admin", true)]
        [InlineData("Republished", "Hold", "Admin", true)]
        [InlineData("Republished", "Draft", "Admin", false)] // Invalid transition
        // Expired - No Annual Verification (only final state)
        [InlineData("Expired - No Annual Verification", "Draft", "Admin", true)]
        [InlineData("Expired - No Annual Verification", "Expired", "Admin", false)] // Only Draft allowed
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
        [InlineData("Draft", "Client Approved - Not Paid", "Analyst", true)] // Analyst has all statuses
        [InlineData("Draft", "Hold", "Analyst", true)] // Analyst can put on hold
        [InlineData("Client Approved - Paid", "Republished", "Analyst", true)] // Analyst can republish
        [InlineData("Client Approved - Paid", "Expired", "Analyst", true)] // Analyst can expire
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
        [InlineData("Draft", "Client Approved - Not Paid", "External", false)] // External only has Draft status, can't transition to non-Draft
        [InlineData("Draft", "Hold", "External", false)] // External cannot put on hold (Hold not in creation statuses)
        [InlineData("Client Approved - Paid", "Expired", "External", false)] // External has no access beyond Draft
        [InlineData("Expired", "Draft", "External", true)] // External CAN transition to Draft (Draft is in their allowed statuses and Expired→Draft is valid)
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
            var (success, errorMessage) = await _service.TransitionCertificateStatusAsync(999, "Client Approved - Not Paid", 1);

            // Assert
            success.Should().BeFalse();
            errorMessage.Should().Be("Certificate not found");
        }

        [Fact]
        public async Task TransitionCertificateStatusAsync_InvalidTransition_ReturnsFailure()
        {
            // Arrange - Draft cannot go directly to Expired
            var cert = CreateTestCertificate(1, "Draft");
            _certificateRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(cert);

            // Act
            var (success, errorMessage) = await _service.TransitionCertificateStatusAsync(1, "Expired", 1);

            // Assert
            success.Should().BeFalse();
            errorMessage.Should().Contain("Invalid status transition");
            errorMessage.Should().Contain("Draft");
            errorMessage.Should().Contain("Expired");
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
            var (success, errorMessage) = await _service.TransitionCertificateStatusAsync(1, "Client Approved - Not Paid", 1);

            // Assert
            success.Should().BeTrue();
            errorMessage.Should().Be("Status updated successfully");
            cert.Status.Should().Be("Client Approved - Not Paid");
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
            await _service.TransitionCertificateStatusAsync(1, "Client Approved - Not Paid", 1);

            // Assert
            _certificateRepoMock.Verify(r => r.UpdateAsync(It.Is<Certificate>(
                c => c.Status == "Client Approved - Not Paid")), Times.Once);
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
            await _service.TransitionCertificateStatusAsync(1, "Client Approved - Not Paid", 1);

            // Assert - Verify audit history was logged to the database
            var historyEntry = await _context.CertificateStatusHistory
                .FirstOrDefaultAsync(h => h.CertificateId == 1);
            historyEntry.Should().NotBeNull();
            historyEntry!.PreviousStatus.Should().Be("Draft");
            historyEntry.NewStatus.Should().Be("Client Approved - Not Paid");
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
            await _service.TransitionCertificateStatusAsync(1, "Client Approved - Not Paid", 1);

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
            var (success, errorMessage) = await _service.TransitionCertificateStatusAsync(1, "Client Approved - Not Paid", 1);

            // Assert
            success.Should().BeFalse();
            errorMessage.Should().Be("An error occurred while updating the status");
        }

        [Fact]
        public async Task TransitionCertificateStatusAsync_FromExpiredNoAnnualVerification_OnlyDraftAllowed()
        {
            // Arrange - "Expired - No Annual Verification" can only transition to Draft
            var cert = CreateTestCertificate(1, "Expired - No Annual Verification");
            _certificateRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(cert);
            _certificateRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Certificate>()))
                .ReturnsAsync((Certificate c) => c);
            _unitOfWorkMock.Setup(u => u.CompleteAsync()).ReturnsAsync(1);

            // Act & Assert - Only Draft should succeed
            var allStatuses = CertificateStatusHelper.GetAllStatuses().Keys;
            foreach (var targetStatus in allStatuses)
            {
                // Reset cert status for each attempt
                cert.Status = "Expired - No Annual Verification";
                var (success, _) = await _service.TransitionCertificateStatusAsync(1, targetStatus, 1);
                if (targetStatus == "Draft")
                    success.Should().BeTrue($"Expired - No Annual Verification should transition to Draft");
                else
                    success.Should().BeFalse($"Expired - No Annual Verification should not transition to {targetStatus}");
            }
        }

        [Fact]
        public async Task TransitionCertificateStatusAsync_FromUnknownStatus_AlwaysFails()
        {
            // Arrange - An unknown/invalid status has no valid transitions
            var cert = CreateTestCertificate(1, "InvalidStatus");
            _certificateRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(cert);

            // Act & Assert - Try every possible target status
            var allStatuses = CertificateStatusHelper.GetAllStatuses().Keys;
            foreach (var targetStatus in allStatuses)
            {
                var (success, _) = await _service.TransitionCertificateStatusAsync(1, targetStatus, 1);
                success.Should().BeFalse($"Unknown status should not transition to {targetStatus}");
            }
        }

        [Fact]
        public async Task TransitionCertificateStatusAsync_FullHappyPath_DraftToClientApprovedPaid()
        {
            // Arrange - Walk the full happy path: Draft → Client Approved - Not Paid → Client Approved - Paid
            var cert = CreateTestCertificate(1, "Draft");
            _certificateRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(cert);
            _certificateRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Certificate>()))
                .ReturnsAsync((Certificate c) => c);
            _unitOfWorkMock.Setup(u => u.CompleteAsync()).ReturnsAsync(1);

            var transitions = new[] { "Client Approved - Not Paid", "Client Approved - Paid" };

            foreach (var targetStatus in transitions)
            {
                // Act
                var (success, message) = await _service.TransitionCertificateStatusAsync(1, targetStatus, 1);

                // Assert
                success.Should().BeTrue($"Transition to {targetStatus} should succeed");
                cert.Status.Should().Be(targetStatus);
            }

            // Verify all 2 audit entries were created
            var historyCount = await _context.CertificateStatusHistory
                .CountAsync(h => h.CertificateId == 1);
            historyCount.Should().Be(2);
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
        public async Task GetAvailableStatusesForCertificateAsync_DraftCert_AdminRole_ReturnsValidTransitions()
        {
            // Arrange
            var cert = CreateTestCertificate(1, "Draft");
            _certificateRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(cert);

            // Act
            var result = await _service.GetAvailableStatusesForCertificateAsync(1, "Admin");

            // Assert - Draft can go to: Client Approved - Not Paid, Client Approved - Confidential, Client Approved - Paid, Hold
            result.Should().Contain("Client Approved - Not Paid");
            result.Should().Contain("Client Approved - Confidential");
            result.Should().Contain("Client Approved - Paid");
            result.Should().Contain("Hold");
            result.Should().HaveCount(4);
        }

        [Fact]
        public async Task GetAvailableStatusesForCertificateAsync_ClientApprovedPaidCert_AdminRole_ReturnsAllValidTransitions()
        {
            // Arrange
            var cert = CreateTestCertificate(1, "Client Approved - Paid");
            _certificateRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(cert);

            // Act
            var result = await _service.GetAvailableStatusesForCertificateAsync(1, "Admin");

            // Assert - Client Approved - Paid can go to: Hold, Expired, Republished, Expired - No Annual Verification
            result.Should().Contain("Hold");
            result.Should().Contain("Expired");
            result.Should().Contain("Republished");
            result.Should().Contain("Expired - No Annual Verification");
            result.Should().HaveCount(4);
        }

        [Fact]
        public async Task GetAvailableStatusesForCertificateAsync_ClientApprovedPaidCert_AnalystRole_SameAsAdmin()
        {
            // Arrange - Analyst has same statuses as Admin in 8-state system
            var cert = CreateTestCertificate(1, "Client Approved - Paid");
            _certificateRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(cert);

            // Act
            var result = await _service.GetAvailableStatusesForCertificateAsync(1, "Analyst");

            // Assert - Analyst has all statuses, same transitions as Admin
            result.Should().Contain("Hold");
            result.Should().Contain("Expired");
            result.Should().Contain("Republished");
            result.Should().Contain("Expired - No Annual Verification");
            result.Should().HaveCount(4);
        }

        [Fact]
        public async Task GetAvailableStatusesForCertificateAsync_DraftCert_ExternalRole_ReturnsEmpty()
        {
            // Arrange - External users only have "Draft" in their role statuses.
            // From Draft, valid transitions are to non-Draft statuses, none of which are in External's allowed list.
            var cert = CreateTestCertificate(1, "Draft");
            _certificateRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(cert);

            // Act
            var result = await _service.GetAvailableStatusesForCertificateAsync(1, "External");

            // Assert - No valid transitions because External can only target "Draft" and Draft→Draft is not a valid transition
            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetAvailableStatusesForCertificateAsync_ExpiredNoAnnualVerificationCert_AdminRole_OnlyDraft()
        {
            // Arrange - "Expired - No Annual Verification" can only transition to Draft
            var cert = CreateTestCertificate(1, "Expired - No Annual Verification");
            _certificateRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(cert);

            // Act
            var result = await _service.GetAvailableStatusesForCertificateAsync(1, "Admin");

            // Assert
            result.Should().Contain("Draft");
            result.Should().HaveCount(1);
        }

        [Fact]
        public async Task GetAvailableStatusesForCertificateAsync_UnknownStatus_AnyRole_ReturnsEmpty()
        {
            // Arrange - An unknown status has no valid transitions
            var cert = CreateTestCertificate(1, "NonExistentStatus");
            _certificateRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(cert);

            // Act
            var result = await _service.GetAvailableStatusesForCertificateAsync(1, "Admin");

            // Assert
            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetAvailableStatusesForCertificateAsync_HoldCert_AnalystRole_ReturnsValidTransitions()
        {
            // Arrange - Hold can transition to Draft, Client Approved - Not Paid, Confidential, Paid
            var cert = CreateTestCertificate(1, "Hold");
            _certificateRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(cert);

            // Act
            var result = await _service.GetAvailableStatusesForCertificateAsync(1, "Analyst");

            // Assert
            result.Should().Contain("Draft");
            result.Should().Contain("Client Approved - Not Paid");
            result.Should().Contain("Client Approved - Confidential");
            result.Should().Contain("Client Approved - Paid");
            result.Should().HaveCount(4);
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
    /// Tests for CertificateStatusHelper static methods — the 8-state workflow per GCC client requirements
    /// </summary>
    public class CertificateStatusHelperTests
    {
        #region GetAllStatuses Tests

        [Fact]
        public void GetAllStatuses_Returns8Statuses()
        {
            var statuses = CertificateStatusHelper.GetAllStatuses();
            statuses.Should().HaveCount(8);
        }

        [Theory]
        [InlineData("Draft", "Draft")]
        [InlineData("Client Approved - Not Paid", "Client Approved - Not Paid")]
        [InlineData("Client Approved - Confidential", "Client Approved - Confidential")]
        [InlineData("Client Approved - Paid", "Client Approved - Paid")]
        [InlineData("Hold", "Hold")]
        [InlineData("Expired", "Expired")]
        [InlineData("Republished", "Republished")]
        [InlineData("Expired - No Annual Verification", "Expired - No Annual Verification")]
        public void GetAllStatuses_ContainsStatusWithCorrectDisplayName(string key, string expectedDisplayName)
        {
            var statuses = CertificateStatusHelper.GetAllStatuses();
            statuses.Should().ContainKey(key);
            statuses[key].Should().Be(expectedDisplayName);
        }

        #endregion

        #region GetCreationStatuses Tests

        [Fact]
        public void GetCreationStatuses_ReturnsOnlyDraft()
        {
            var statuses = CertificateStatusHelper.GetCreationStatuses();
            statuses.Should().HaveCount(1);
            statuses.Should().ContainKey("Draft");
        }

        #endregion

        #region GetStatusesForRole Tests

        [Fact]
        public void GetStatusesForRole_Admin_ReturnsAll8Statuses()
        {
            var statuses = CertificateStatusHelper.GetStatusesForRole("Admin");
            statuses.Should().HaveCount(8);
        }

        [Fact]
        public void GetStatusesForRole_Analyst_ReturnsAll8Statuses()
        {
            var statuses = CertificateStatusHelper.GetStatusesForRole("Analyst");
            statuses.Should().HaveCount(8);
        }

        [Fact]
        public void GetStatusesForRole_External_OnlyCreationStatuses()
        {
            var statuses = CertificateStatusHelper.GetStatusesForRole("External");
            statuses.Should().HaveCount(1);
            statuses.Should().ContainKey("Draft");
        }

        [Fact]
        public void GetStatusesForRole_UnknownRole_ReturnsCreationStatuses()
        {
            var statuses = CertificateStatusHelper.GetStatusesForRole("SomeRandomRole");
            statuses.Should().HaveCount(1);
            statuses.Should().ContainKey("Draft");
        }

        [Fact]
        public void GetStatusesForRole_NullRole_ReturnsCreationStatuses()
        {
            var statuses = CertificateStatusHelper.GetStatusesForRole(null!);
            statuses.Should().HaveCount(1);
        }

        [Fact]
        public void GetStatusesForRole_CaseInsensitive()
        {
            var adminStatuses = CertificateStatusHelper.GetStatusesForRole("admin");
            var AdminStatuses = CertificateStatusHelper.GetStatusesForRole("Admin");
            var ADMINStatuses = CertificateStatusHelper.GetStatusesForRole("ADMIN");

            adminStatuses.Should().HaveCount(8);
            AdminStatuses.Should().HaveCount(8);
            ADMINStatuses.Should().HaveCount(8);
        }

        #endregion

        #region IsValidTransition Tests - Complete Transition Matrix

        [Theory]
        // Draft transitions
        [InlineData("Draft", "Client Approved - Not Paid", true)]
        [InlineData("Draft", "Client Approved - Confidential", true)]
        [InlineData("Draft", "Client Approved - Paid", true)]
        [InlineData("Draft", "Hold", true)]
        [InlineData("Draft", "Expired", false)]
        [InlineData("Draft", "Republished", false)]
        // Client Approved - Not Paid transitions
        [InlineData("Client Approved - Not Paid", "Client Approved - Paid", true)]
        [InlineData("Client Approved - Not Paid", "Client Approved - Confidential", true)]
        [InlineData("Client Approved - Not Paid", "Hold", true)]
        [InlineData("Client Approved - Not Paid", "Expired", true)]
        [InlineData("Client Approved - Not Paid", "Draft", false)]
        [InlineData("Client Approved - Not Paid", "Republished", false)]
        // Client Approved - Confidential transitions
        [InlineData("Client Approved - Confidential", "Client Approved - Paid", true)]
        [InlineData("Client Approved - Confidential", "Hold", true)]
        [InlineData("Client Approved - Confidential", "Expired", true)]
        [InlineData("Client Approved - Confidential", "Draft", false)]
        [InlineData("Client Approved - Confidential", "Republished", false)]
        // Client Approved - Paid transitions
        [InlineData("Client Approved - Paid", "Hold", true)]
        [InlineData("Client Approved - Paid", "Expired", true)]
        [InlineData("Client Approved - Paid", "Republished", true)]
        [InlineData("Client Approved - Paid", "Expired - No Annual Verification", true)]
        [InlineData("Client Approved - Paid", "Draft", false)]
        [InlineData("Client Approved - Paid", "Client Approved - Not Paid", false)]
        // Hold transitions
        [InlineData("Hold", "Draft", true)]
        [InlineData("Hold", "Client Approved - Not Paid", true)]
        [InlineData("Hold", "Client Approved - Confidential", true)]
        [InlineData("Hold", "Client Approved - Paid", true)]
        [InlineData("Hold", "Expired", false)]
        [InlineData("Hold", "Republished", false)]
        // Expired transitions
        [InlineData("Expired", "Republished", true)]
        [InlineData("Expired", "Draft", true)]
        [InlineData("Expired", "Client Approved - Paid", false)]
        [InlineData("Expired", "Hold", false)]
        // Republished transitions
        [InlineData("Republished", "Expired", true)]
        [InlineData("Republished", "Expired - No Annual Verification", true)]
        [InlineData("Republished", "Hold", true)]
        [InlineData("Republished", "Draft", false)]
        [InlineData("Republished", "Client Approved - Paid", false)]
        // Expired - No Annual Verification transitions
        [InlineData("Expired - No Annual Verification", "Draft", true)]
        [InlineData("Expired - No Annual Verification", "Expired", false)]
        [InlineData("Expired - No Annual Verification", "Republished", false)]
        public void IsValidTransition_ReturnsExpectedResult(string from, string to, bool expected)
        {
            CertificateStatusHelper.IsValidTransition(from, to).Should().Be(expected,
                $"transition from '{from}' to '{to}' should be {expected}");
        }

        [Fact]
        public void IsValidTransition_UnknownFromStatus_ReturnsFalse()
        {
            CertificateStatusHelper.IsValidTransition("InvalidStatus", "Draft").Should().BeFalse();
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
        [InlineData("Client Approved - Not Paid", "Client Approved - Not Paid")]
        [InlineData("Client Approved - Paid", "Client Approved - Paid")]
        [InlineData("Hold", "Hold")]
        [InlineData("Expired", "Expired")]
        [InlineData("Republished", "Republished")]
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
        [InlineData("Client Approved - Paid", true)]
        [InlineData("Republished", true)]
        [InlineData("Draft", false)]
        [InlineData("Expired", false)]
        [InlineData("Hold", false)]
        [InlineData("Client Approved - Not Paid", false)]
        [InlineData("Expired - No Annual Verification", false)]
        public void IsActiveStatus_ReturnsExpectedResult(string status, bool expected)
        {
            CertificateStatusHelper.IsActiveStatus(status).Should().Be(expected);
        }

        #endregion

        #region IsFinalStatus Tests

        [Theory]
        [InlineData("Expired - No Annual Verification", true)]
        [InlineData("Draft", false)]
        [InlineData("Client Approved - Paid", false)]
        [InlineData("Expired", false)]
        [InlineData("Hold", false)]
        [InlineData("Republished", false)]
        public void IsFinalStatus_ReturnsExpectedResult(string status, bool expected)
        {
            CertificateStatusHelper.IsFinalStatus(status).Should().Be(expected);
        }

        #endregion

        #region IsNegativeStatus Tests

        [Theory]
        [InlineData("Expired", true)]
        [InlineData("Expired - No Annual Verification", true)]
        [InlineData("Draft", false)]
        [InlineData("Client Approved - Paid", false)]
        [InlineData("Hold", false)]
        [InlineData("Republished", false)]
        public void IsNegativeStatus_ReturnsExpectedResult(string status, bool expected)
        {
            CertificateStatusHelper.IsNegativeStatus(status).Should().Be(expected);
        }

        #endregion

        #region NeedsAttention Tests

        [Theory]
        [InlineData("Client Approved - Not Paid", true)]
        [InlineData("Hold", true)]
        [InlineData("Expired", true)]
        [InlineData("Draft", false)]
        [InlineData("Client Approved - Paid", false)]
        [InlineData("Republished", false)]
        [InlineData("Expired - No Annual Verification", false)]
        public void NeedsAttention_ReturnsExpectedResult(string status, bool expected)
        {
            CertificateStatusHelper.NeedsAttention(status).Should().Be(expected);
        }

        #endregion

        #region Workflow Consistency Tests

        [Fact]
        public void FinalStatuses_OnlyAllowTransitionBackToDraft()
        {
            // In the 8-state system, the "final" status (Expired - No Annual Verification)
            // can only transition back to Draft for reprocessing
            var finalStatuses = CertificateStatusHelper.GetAllStatuses().Keys
                .Where(CertificateStatusHelper.IsFinalStatus);

            foreach (var finalStatus in finalStatuses)
            {
                var allTargets = CertificateStatusHelper.GetAllStatuses().Keys;
                foreach (var target in allTargets)
                {
                    if (target == "Draft")
                    {
                        CertificateStatusHelper.IsValidTransition(finalStatus, target).Should().BeTrue(
                            $"Final status '{finalStatus}' should allow transition back to Draft");
                    }
                    else
                    {
                        CertificateStatusHelper.IsValidTransition(finalStatus, target).Should().BeFalse(
                            $"Final status '{finalStatus}' should not allow transition to '{target}'");
                    }
                }
            }
        }

        [Fact]
        public void AllStatuses_HaveTransitionRules()
        {
            // Every status should be a valid 'from' status in the transition map
            // (even if it has no valid transitions, like Expired - No Annual Verification)
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
        public void HappyPath_DraftToApprovedPaid_IsReachable()
        {
            // Verify the primary workflow path: Draft → Client Approved - Not Paid → Client Approved - Paid
            CertificateStatusHelper.IsValidTransition("Draft", "Client Approved - Not Paid").Should().BeTrue();
            CertificateStatusHelper.IsValidTransition("Client Approved - Not Paid", "Client Approved - Paid").Should().BeTrue();
        }

        [Fact]
        public void HoldPath_CanReturnToAnyApprovalState()
        {
            // Verify Hold can return to Draft or any Client Approved state
            CertificateStatusHelper.IsValidTransition("Hold", "Draft").Should().BeTrue();
            CertificateStatusHelper.IsValidTransition("Hold", "Client Approved - Not Paid").Should().BeTrue();
            CertificateStatusHelper.IsValidTransition("Hold", "Client Approved - Confidential").Should().BeTrue();
            CertificateStatusHelper.IsValidTransition("Hold", "Client Approved - Paid").Should().BeTrue();
        }

        [Fact]
        public void ExpiredPath_CanRepublishOrRestartAsDraft()
        {
            // Verify expired certificates can be republished or restarted
            CertificateStatusHelper.IsValidTransition("Expired", "Republished").Should().BeTrue();
            CertificateStatusHelper.IsValidTransition("Expired", "Draft").Should().BeTrue();
        }

        #endregion
    }
}
