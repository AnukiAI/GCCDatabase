using GCCWebApp.Models;
using GCCWebApp.Repositories;
using GCCWebApp.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace GCCWebApp.Services
{
    /// <summary>
    /// Service for managing certificate workflow and status transitions
    /// Enforces business rules for certificate lifecycle management
    /// </summary>
    public interface ICertificateWorkflowService
    {
        Task<bool> CanTransitionToStatusAsync(int certificateId, string newStatus, string currentUserRole);
        Task<(bool Success, string ErrorMessage)> TransitionCertificateStatusAsync(int certificateId, string newStatus, int userId);
        Task<List<string>> GetAvailableStatusesForCertificateAsync(int certificateId, string userRole);
        Task CheckAndUpdateExpiringCertificatesAsync();
        Task<Dictionary<string, int>> GetStatusStatisticsAsync();
    }

    public class CertificateWorkflowService : ICertificateWorkflowService
    {
        private readonly ICertificateRepository _certificateRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<CertificateWorkflowService> _logger;
        private readonly ApplicationDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CertificateWorkflowService(
            ICertificateRepository certificateRepository,
            IUnitOfWork unitOfWork,
            ILogger<CertificateWorkflowService> logger,
            ApplicationDbContext context,
            IHttpContextAccessor httpContextAccessor)
        {
            _certificateRepository = certificateRepository;
            _unitOfWork = unitOfWork;
            _logger = logger;
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        /// <summary>
        /// Checks if a certificate can transition to a new status
        /// </summary>
        public async Task<bool> CanTransitionToStatusAsync(int certificateId, string newStatus, string currentUserRole)
        {
            var certificate = await _certificateRepository.GetByIdAsync(certificateId);
            if (certificate == null)
                return false;

            // Check if user role has permission for the new status
            var availableStatuses = CertificateStatusHelper.GetStatusesForRole(currentUserRole);
            if (!availableStatuses.ContainsKey(newStatus))
                return false;

            // Check if transition is valid based on business rules
            return CertificateStatusHelper.IsValidTransition(certificate.Status, newStatus);
        }

        /// <summary>
        /// Transitions a certificate to a new status with validation
        /// </summary>
        public async Task<(bool Success, string ErrorMessage)> TransitionCertificateStatusAsync(int certificateId, string newStatus, int userId)
        {
            try
            {
                var certificate = await _certificateRepository.GetByIdAsync(certificateId);
                if (certificate == null)
                    return (false, "Certificate not found");

                var oldStatus = certificate.Status;

                // Validate transition
                if (!CertificateStatusHelper.IsValidTransition(oldStatus, newStatus))
                {
                    return (false, $"Invalid status transition from '{CertificateStatusHelper.GetDisplayName(oldStatus)}' to '{CertificateStatusHelper.GetDisplayName(newStatus)}'");
                }

                // Update certificate
                certificate.Status = newStatus;
                certificate.UpdatedBy = userId;
                certificate.UpdatedAt = DateTime.UtcNow;

                await _certificateRepository.UpdateAsync(certificate);
                await _unitOfWork.CompleteAsync();

                _logger.LogInformation("Certificate {CertificateId} status changed from {OldStatus} to {NewStatus} by user {UserId}",
                    certificateId, oldStatus, newStatus, userId);

                // Log status change to audit trail
                await LogStatusChangeAsync(certificate, oldStatus, newStatus, userId);

                // Trigger additional business logic based on new status
                await HandleStatusChangeBusinessLogic(certificate, oldStatus, newStatus, userId);

                return (true, "Status updated successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error transitioning certificate {CertificateId} to status {Status}", certificateId, newStatus);
                return (false, "An error occurred while updating the status");
            }
        }

        /// <summary>
        /// Gets available status options for a certificate based on current status and user role
        /// </summary>
        public async Task<List<string>> GetAvailableStatusesForCertificateAsync(int certificateId, string userRole)
        {
            var certificate = await _certificateRepository.GetByIdAsync(certificateId);
            if (certificate == null)
                return new List<string>();

            var roleStatuses = CertificateStatusHelper.GetStatusesForRole(userRole);
            var availableStatuses = new List<string>();

            foreach (var status in roleStatuses.Keys)
            {
                if (CertificateStatusHelper.IsValidTransition(certificate.Status, status))
                {
                    availableStatuses.Add(status);
                }
            }

            return availableStatuses;
        }

        /// <summary>
        /// Automated task to check and update certificates that are expiring soon
        /// </summary>
        public async Task CheckAndUpdateExpiringCertificatesAsync()
        {
            try
            {
                var expiringDate = DateTime.UtcNow.AddDays(30); // 30 days warning
                var expiringCertificates = await _certificateRepository.GetExpiringCertificatesAsync(expiringDate);

                foreach (var certificate in expiringCertificates)
                {
                    if (certificate.Status == "active" && certificate.ExpirationDate <= expiringDate)
                    {
                        var oldStatus = certificate.Status;
                        certificate.Status = "expiring_soon";
                        certificate.UpdatedAt = DateTime.UtcNow;
                        await _certificateRepository.UpdateAsync(certificate);

                        _logger.LogInformation("Certificate {CertificateNumber} automatically updated to 'expiring_soon' status",
                            certificate.Number);

                        // Log automated status change (using system user ID 0 for automated tasks)
                        await LogStatusChangeAsync(certificate, oldStatus, "expiring_soon", 0,
                            "Automated transition: Certificate expires within 30 days", isAutomated: true);
                    }

                    // Check for expired certificates
                    if ((certificate.Status == "active" || certificate.Status == "expiring_soon") &&
                        certificate.ExpirationDate <= DateTime.UtcNow)
                    {
                        var oldStatus = certificate.Status;
                        certificate.Status = "expired";
                        certificate.UpdatedAt = DateTime.UtcNow;
                        await _certificateRepository.UpdateAsync(certificate);

                        _logger.LogInformation("Certificate {CertificateNumber} automatically expired",
                            certificate.Number);

                        // Log automated status change (using system user ID 0 for automated tasks)
                        await LogStatusChangeAsync(certificate, oldStatus, "expired", 0,
                            "Automated transition: Certificate expiration date reached", isAutomated: true);
                    }
                }

                await _unitOfWork.CompleteAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking and updating expiring certificates");
            }
        }

        /// <summary>
        /// Gets statistics for certificate statuses
        /// </summary>
        public async Task<Dictionary<string, int>> GetStatusStatisticsAsync()
        {
            try
            {
                return await _certificateRepository.GetCertificateStatisticsAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting certificate status statistics");
                return new Dictionary<string, int>();
            }
        }

        /// <summary>
        /// Logs status change to audit trail
        /// </summary>
        private async Task LogStatusChangeAsync(Certificate certificate, string oldStatus, string newStatus, int userId, string? reason = null, bool isAutomated = false)
        {
            try
            {
                var httpContext = _httpContextAccessor.HttpContext;

                var historyEntry = new CertificateStatusHistory
                {
                    CertificateId = certificate.Id,
                    CertificateNumber = certificate.Number,
                    PreviousStatus = oldStatus,
                    NewStatus = newStatus,
                    Reason = reason,
                    ChangedBy = userId,
                    ChangedAt = DateTime.UtcNow,
                    IsAutomatedTransition = isAutomated,
                    ChangeSource = isAutomated ? "Automated" : "Manual",
                    IpAddress = httpContext?.Connection?.RemoteIpAddress?.ToString(),
                    UserAgent = httpContext?.Request?.Headers["User-Agent"].ToString()
                };

                // Get user details for denormalization
                var user = await _context.Users.FindAsync(userId);
                if (user != null)
                {
                    historyEntry.ChangedByUsername = user.Username;
                }

                // Calculate duration in previous status
                var previousStatusEntry = await _context.CertificateStatusHistory
                    .Where(h => h.CertificateId == certificate.Id)
                    .OrderByDescending(h => h.ChangedAt)
                    .FirstOrDefaultAsync();

                if (previousStatusEntry != null)
                {
                    var duration = (DateTime.UtcNow - previousStatusEntry.ChangedAt).TotalDays;
                    historyEntry.DurationInPreviousStatus = (int)Math.Round(duration);
                }

                _context.CertificateStatusHistory.Add(historyEntry);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Status history logged for certificate {CertificateNumber}: {OldStatus} -> {NewStatus}",
                    certificate.Number, oldStatus, newStatus);
            }
            catch (Exception ex)
            {
                // Don't fail the status change if logging fails
                _logger.LogError(ex, "Error logging status change for certificate {CertificateId}", certificate.Id);
            }
        }

        /// <summary>
        /// Handles additional business logic when status changes
        /// </summary>
        private Task HandleStatusChangeBusinessLogic(Certificate certificate, string oldStatus, string newStatus, int userId)
        {
            try
            {
                // Example business logic for specific status changes
                switch (newStatus)
                {
                    case "final_approval":
                        // Could trigger PDF generation, notification emails, etc.
                        _logger.LogInformation("Certificate {Number} reached final approval - triggering completion workflow", 
                            certificate.Number);
                        break;

                    case "active":
                        // Certificate becomes active - trigger partner notifications if needed
                        if (!string.IsNullOrEmpty(certificate.Partner) && certificate.Partner != "None")
                        {
                            _logger.LogInformation("Certificate {Number} activated for partner {Partner}", 
                                certificate.Number, certificate.Partner);
                            // Could trigger partner-specific workflows here
                        }
                        break;

                    case "expired":
                        // Handle expiration - could trigger renewal workflows
                        _logger.LogInformation("Certificate {Number} expired - checking for renewal eligibility", 
                            certificate.Number);
                        break;

                    case "cancelled":
                    case "client_declined":
                        // Handle cancellation/decline - could trigger refund workflows
                        _logger.LogInformation("Certificate {Number} cancelled/declined - cleaning up resources", 
                            certificate.Number);
                        break;
                }

                // Additional workflow logic could be added here
                // - Email notifications
                // - Partner API calls
                // - Document generation
                // - Audit logging
                // - Webhook triggers

            }
            catch (Exception ex)
            {
                // Don't fail the status update if business logic fails
                _logger.LogError(ex, "Error in status change business logic for certificate {CertificateId}", certificate.Id);
            }
            
            return Task.CompletedTask;
        }
    }
}