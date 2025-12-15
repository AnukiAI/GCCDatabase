using FluentAssertions;
using GCCWebApp.Models;

namespace GCCWebApp.Tests.Services
{
    /// <summary>
    /// Comprehensive tests for CertificateStatusHelper
    /// Tests all 14 status states and their transition rules
    /// </summary>
    public class CertificateStatusHelperTests
    {
        #region GetAllStatuses Tests

        [Fact]
        public void GetAllStatuses_ShouldReturn14Statuses()
        {
            // Act
            var statuses = CertificateStatusHelper.GetAllStatuses();

            // Assert
            statuses.Should().HaveCount(14, "There should be exactly 14 certificate statuses");
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
        public void GetAllStatuses_ShouldContainAllExpectedStatuses(string key, string displayName)
        {
            // Act
            var statuses = CertificateStatusHelper.GetAllStatuses();

            // Assert
            statuses.Should().ContainKey(key);
            statuses[key].Should().Be(displayName);
        }

        #endregion

        #region GetCreationStatuses Tests

        [Fact]
        public void GetCreationStatuses_ShouldOnlyReturnDraftAndPendingReview()
        {
            // Act
            var statuses = CertificateStatusHelper.GetCreationStatuses();

            // Assert
            statuses.Should().HaveCount(2);
            statuses.Should().ContainKey("Draft");
            statuses.Should().ContainKey("PendingReview");
        }

        [Fact]
        public void GetCreationStatuses_ShouldNotContainActiveOrOtherStatuses()
        {
            // Act
            var statuses = CertificateStatusHelper.GetCreationStatuses();

            // Assert
            statuses.Should().NotContainKey("Active");
            statuses.Should().NotContainKey("Approved");
            statuses.Should().NotContainKey("Expired");
        }

        #endregion

        #region GetStatusesForRole Tests

        [Fact]
        public void GetStatusesForRole_Admin_ShouldReturnAllStatuses()
        {
            // Act
            var statuses = CertificateStatusHelper.GetStatusesForRole("Admin");

            // Assert
            statuses.Should().HaveCount(14);
            statuses.Should().ContainKey("Rejected");
            statuses.Should().ContainKey("Revoked");
        }

        [Fact]
        public void GetStatusesForRole_AdminLowercase_ShouldReturnAllStatuses()
        {
            // Act
            var statuses = CertificateStatusHelper.GetStatusesForRole("admin");

            // Assert
            statuses.Should().HaveCount(14);
        }

        [Fact]
        public void GetStatusesForRole_Analyst_ShouldNotHaveRejectedOrRevoked()
        {
            // Act
            var statuses = CertificateStatusHelper.GetStatusesForRole("Analyst");

            // Assert
            statuses.Should().NotContainKey("Rejected");
            statuses.Should().NotContainKey("Revoked");
            statuses.Should().ContainKey("Active");
            statuses.Should().ContainKey("Approved");
        }

        [Fact]
        public void GetStatusesForRole_External_ShouldOnlyHaveCreationStatuses()
        {
            // Act
            var statuses = CertificateStatusHelper.GetStatusesForRole("External");

            // Assert
            statuses.Should().HaveCount(2);
            statuses.Should().ContainKey("Draft");
            statuses.Should().ContainKey("PendingReview");
            statuses.Should().NotContainKey("Active");
        }

        [Fact]
        public void GetStatusesForRole_UnknownRole_ShouldReturnCreationStatuses()
        {
            // Act
            var statuses = CertificateStatusHelper.GetStatusesForRole("UnknownRole");

            // Assert
            statuses.Should().HaveCount(2);
            statuses.Should().ContainKey("Draft");
            statuses.Should().ContainKey("PendingReview");
        }

        [Fact]
        public void GetStatusesForRole_NullRole_ShouldReturnCreationStatuses()
        {
            // Act
            var statuses = CertificateStatusHelper.GetStatusesForRole(null);

            // Assert
            statuses.Should().HaveCount(2);
        }

        #endregion

        #region IsValidTransition Tests - All 14 States

        // Draft transitions
        [Theory]
        [InlineData("Draft", "PendingReview", true)]
        [InlineData("Draft", "Cancelled", true)]
        [InlineData("Draft", "Active", false)]
        [InlineData("Draft", "Approved", false)]
        [InlineData("Draft", "UnderReview", false)]
        public void IsValidTransition_FromDraft_ShouldValidateCorrectly(string from, string to, bool expected)
        {
            CertificateStatusHelper.IsValidTransition(from, to).Should().Be(expected);
        }

        // PendingReview transitions
        [Theory]
        [InlineData("PendingReview", "UnderReview", true)]
        [InlineData("PendingReview", "Draft", true)]
        [InlineData("PendingReview", "Cancelled", true)]
        [InlineData("PendingReview", "OnHold", true)]
        [InlineData("PendingReview", "Active", false)]
        [InlineData("PendingReview", "Approved", false)]
        public void IsValidTransition_FromPendingReview_ShouldValidateCorrectly(string from, string to, bool expected)
        {
            CertificateStatusHelper.IsValidTransition(from, to).Should().Be(expected);
        }

        // UnderReview transitions
        [Theory]
        [InlineData("UnderReview", "Approved", true)]
        [InlineData("UnderReview", "PendingReview", true)]
        [InlineData("UnderReview", "Rejected", true)]
        [InlineData("UnderReview", "OnHold", true)]
        [InlineData("UnderReview", "Active", false)]
        [InlineData("UnderReview", "Draft", false)]
        public void IsValidTransition_FromUnderReview_ShouldValidateCorrectly(string from, string to, bool expected)
        {
            CertificateStatusHelper.IsValidTransition(from, to).Should().Be(expected);
        }

        // Approved transitions
        [Theory]
        [InlineData("Approved", "Active", true)]
        [InlineData("Approved", "UnderReview", true)]
        [InlineData("Approved", "OnHold", true)]
        [InlineData("Approved", "Draft", false)]
        [InlineData("Approved", "Cancelled", false)]
        public void IsValidTransition_FromApproved_ShouldValidateCorrectly(string from, string to, bool expected)
        {
            CertificateStatusHelper.IsValidTransition(from, to).Should().Be(expected);
        }

        // Active transitions
        [Theory]
        [InlineData("Active", "Suspended", true)]
        [InlineData("Active", "Expired", true)]
        [InlineData("Active", "PendingRenewal", true)]
        [InlineData("Active", "Revoked", true)]
        [InlineData("Active", "Archived", true)]
        [InlineData("Active", "Draft", false)]
        [InlineData("Active", "Approved", false)]
        public void IsValidTransition_FromActive_ShouldValidateCorrectly(string from, string to, bool expected)
        {
            CertificateStatusHelper.IsValidTransition(from, to).Should().Be(expected);
        }

        // Suspended transitions
        [Theory]
        [InlineData("Suspended", "Active", true)]
        [InlineData("Suspended", "Revoked", true)]
        [InlineData("Suspended", "Cancelled", true)]
        [InlineData("Suspended", "Archived", true)]
        [InlineData("Suspended", "Draft", false)]
        [InlineData("Suspended", "Approved", false)]
        public void IsValidTransition_FromSuspended_ShouldValidateCorrectly(string from, string to, bool expected)
        {
            CertificateStatusHelper.IsValidTransition(from, to).Should().Be(expected);
        }

        // Revoked transitions
        [Theory]
        [InlineData("Revoked", "Archived", true)]
        [InlineData("Revoked", "Active", false)]
        [InlineData("Revoked", "Draft", false)]
        [InlineData("Revoked", "Cancelled", false)]
        public void IsValidTransition_FromRevoked_ShouldValidateCorrectly(string from, string to, bool expected)
        {
            CertificateStatusHelper.IsValidTransition(from, to).Should().Be(expected);
        }

        // Expired transitions
        [Theory]
        [InlineData("Expired", "PendingRenewal", true)]
        [InlineData("Expired", "Archived", true)]
        [InlineData("Expired", "Active", true)] // Re-activation allowed
        [InlineData("Expired", "Draft", false)]
        [InlineData("Expired", "Approved", false)]
        public void IsValidTransition_FromExpired_ShouldValidateCorrectly(string from, string to, bool expected)
        {
            CertificateStatusHelper.IsValidTransition(from, to).Should().Be(expected);
        }

        // PendingRenewal transitions
        [Theory]
        [InlineData("PendingRenewal", "UnderReview", true)]
        [InlineData("PendingRenewal", "Renewed", true)]
        [InlineData("PendingRenewal", "Expired", true)]
        [InlineData("PendingRenewal", "Cancelled", true)]
        [InlineData("PendingRenewal", "Active", false)]
        [InlineData("PendingRenewal", "Draft", false)]
        public void IsValidTransition_FromPendingRenewal_ShouldValidateCorrectly(string from, string to, bool expected)
        {
            CertificateStatusHelper.IsValidTransition(from, to).Should().Be(expected);
        }

        // Renewed transitions
        [Theory]
        [InlineData("Renewed", "Active", true)]
        [InlineData("Renewed", "Draft", false)]
        [InlineData("Renewed", "Expired", false)]
        [InlineData("Renewed", "Cancelled", false)]
        public void IsValidTransition_FromRenewed_ShouldValidateCorrectly(string from, string to, bool expected)
        {
            CertificateStatusHelper.IsValidTransition(from, to).Should().Be(expected);
        }

        // Cancelled transitions (Final state)
        [Theory]
        [InlineData("Cancelled", "Draft", false)]
        [InlineData("Cancelled", "Active", false)]
        [InlineData("Cancelled", "Archived", false)]
        [InlineData("Cancelled", "PendingReview", false)]
        public void IsValidTransition_FromCancelled_ShouldNotAllowAnyTransition(string from, string to, bool expected)
        {
            CertificateStatusHelper.IsValidTransition(from, to).Should().Be(expected);
        }

        // Archived transitions (Final state)
        [Theory]
        [InlineData("Archived", "Draft", false)]
        [InlineData("Archived", "Active", false)]
        [InlineData("Archived", "Cancelled", false)]
        [InlineData("Archived", "PendingReview", false)]
        public void IsValidTransition_FromArchived_ShouldNotAllowAnyTransition(string from, string to, bool expected)
        {
            CertificateStatusHelper.IsValidTransition(from, to).Should().Be(expected);
        }

        // OnHold transitions
        [Theory]
        [InlineData("OnHold", "PendingReview", true)]
        [InlineData("OnHold", "UnderReview", true)]
        [InlineData("OnHold", "Approved", true)]
        [InlineData("OnHold", "Cancelled", true)]
        [InlineData("OnHold", "Active", false)]
        [InlineData("OnHold", "Draft", false)]
        public void IsValidTransition_FromOnHold_ShouldValidateCorrectly(string from, string to, bool expected)
        {
            CertificateStatusHelper.IsValidTransition(from, to).Should().Be(expected);
        }

        // Rejected transitions
        [Theory]
        [InlineData("Rejected", "Draft", true)] // Can restart workflow
        [InlineData("Rejected", "Archived", true)]
        [InlineData("Rejected", "Active", false)]
        [InlineData("Rejected", "Approved", false)]
        [InlineData("Rejected", "PendingReview", false)]
        public void IsValidTransition_FromRejected_ShouldValidateCorrectly(string from, string to, bool expected)
        {
            CertificateStatusHelper.IsValidTransition(from, to).Should().Be(expected);
        }

        // Edge cases
        [Fact]
        public void IsValidTransition_WithUnknownFromStatus_ShouldReturnFalse()
        {
            var result = CertificateStatusHelper.IsValidTransition("UnknownStatus", "Active");
            result.Should().BeFalse();
        }

        [Fact]
        public void IsValidTransition_SameStatus_ShouldReturnFalse()
        {
            var result = CertificateStatusHelper.IsValidTransition("Active", "Active");
            result.Should().BeFalse();
        }

        #endregion

        #region GetDisplayName Tests

        [Theory]
        [InlineData("Draft", "Draft")]
        [InlineData("PendingReview", "Pending Review")]
        [InlineData("UnderReview", "Under Review")]
        [InlineData("PendingRenewal", "Pending Renewal")]
        [InlineData("OnHold", "On Hold")]
        public void GetDisplayName_ShouldReturnFormattedName(string status, string expected)
        {
            CertificateStatusHelper.GetDisplayName(status).Should().Be(expected);
        }

        [Fact]
        public void GetDisplayName_WithUnknownStatus_ShouldReturnStatusAsIs()
        {
            var result = CertificateStatusHelper.GetDisplayName("UnknownStatus");
            result.Should().Be("UnknownStatus");
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
        [InlineData("Revoked", false)]
        [InlineData("Cancelled", false)]
        public void IsActiveStatus_ShouldIdentifyActiveStatuses(string status, bool expected)
        {
            CertificateStatusHelper.IsActiveStatus(status).Should().Be(expected);
        }

        #endregion

        #region IsFinalStatus Tests

        [Theory]
        [InlineData("Archived", true)]
        [InlineData("Cancelled", true)]
        [InlineData("Active", false)]
        [InlineData("Draft", false)]
        [InlineData("Revoked", false)] // Revoked is not final - can go to Archived
        [InlineData("Expired", false)]
        public void IsFinalStatus_ShouldIdentifyFinalStatuses(string status, bool expected)
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
        [InlineData("Expired", false)]
        [InlineData("Suspended", false)] // Suspended is not negative - can be reinstated
        public void IsNegativeStatus_ShouldIdentifyNegativeStatuses(string status, bool expected)
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
        [InlineData("Approved", false)]
        [InlineData("Archived", false)]
        public void NeedsAttention_ShouldIdentifyStatusesRequiringAttention(string status, bool expected)
        {
            CertificateStatusHelper.NeedsAttention(status).Should().Be(expected);
        }

        #endregion

        #region Workflow Scenarios

        [Fact]
        public void HappyPath_DraftToActive_ShouldBeValid()
        {
            // Verify the complete happy path workflow is valid
            CertificateStatusHelper.IsValidTransition("Draft", "PendingReview").Should().BeTrue();
            CertificateStatusHelper.IsValidTransition("PendingReview", "UnderReview").Should().BeTrue();
            CertificateStatusHelper.IsValidTransition("UnderReview", "Approved").Should().BeTrue();
            CertificateStatusHelper.IsValidTransition("Approved", "Active").Should().BeTrue();
        }

        [Fact]
        public void RenewalPath_ActiveToRenewedToActive_ShouldBeValid()
        {
            CertificateStatusHelper.IsValidTransition("Active", "PendingRenewal").Should().BeTrue();
            CertificateStatusHelper.IsValidTransition("PendingRenewal", "Renewed").Should().BeTrue();
            CertificateStatusHelper.IsValidTransition("Renewed", "Active").Should().BeTrue();
        }

        [Fact]
        public void SuspensionPath_ActiveToSuspendedToActive_ShouldBeValid()
        {
            CertificateStatusHelper.IsValidTransition("Active", "Suspended").Should().BeTrue();
            CertificateStatusHelper.IsValidTransition("Suspended", "Active").Should().BeTrue();
        }

        [Fact]
        public void RejectionPath_UnderReviewToRejectedToDraft_ShouldBeValid()
        {
            CertificateStatusHelper.IsValidTransition("UnderReview", "Rejected").Should().BeTrue();
            CertificateStatusHelper.IsValidTransition("Rejected", "Draft").Should().BeTrue();
        }

        [Fact]
        public void ExpirationPath_ActiveToExpiredToRenewal_ShouldBeValid()
        {
            CertificateStatusHelper.IsValidTransition("Active", "Expired").Should().BeTrue();
            CertificateStatusHelper.IsValidTransition("Expired", "PendingRenewal").Should().BeTrue();
            CertificateStatusHelper.IsValidTransition("PendingRenewal", "UnderReview").Should().BeTrue();
        }

        [Fact]
        public void RevocationPath_ActiveToRevokedToArchived_ShouldBeValid()
        {
            CertificateStatusHelper.IsValidTransition("Active", "Revoked").Should().BeTrue();
            CertificateStatusHelper.IsValidTransition("Revoked", "Archived").Should().BeTrue();
        }

        #endregion
    }
}
