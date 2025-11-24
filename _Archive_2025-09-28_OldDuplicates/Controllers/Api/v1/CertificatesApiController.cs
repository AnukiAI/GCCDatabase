using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using GCCWebApp.Models;
using System.ComponentModel.DataAnnotations;

namespace GCCWebApp.Controllers.Api.v1
{
    /// <summary>
    /// API Controller for Certificate data integration platform
    /// Handles certificate submission, sync status, and retailer integration
    /// </summary>
    [ApiController]
    [Route("api/v1/[controller]")]
    [Produces("application/json")]
    public class CertificatesController : ControllerBase
    {
        private readonly ILogger<CertificatesController> _logger;

        public CertificatesController(ILogger<CertificatesController> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Submit certificate for multi-retailer synchronizatio
        /// </summary>
        /// <param name="request">Certificate submission data</param>
        /// <returns>Submission result with tracking ID</returns>
        [HttpPost]
        [ProducesResponseType(typeof(CertificateSubmissionResult), 201)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        public async Task<ActionResult<CertificateSubmissionResult>> SubmitCertificate(
            [FromBody] CertificateSubmissionRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                // Generate tracking ID for this submission
                var trackingId = Guid.NewGuid().ToString();
                
                // Queue for processing to enabled retailers
                var enabledRetailers = request.TargetRetailers ?? new[] { "amazon", "wayfair", "target" };
                
                var result = new CertificateSubmissionResult
                {
                    TrackingId = trackingId,
                    CertificateId = request.CertificateNumber,
                    Status = "Queued",
                    TargetRetailers = enabledRetailers.ToList(),
                    SubmittedAt = DateTime.UtcNow,
                    EstimatedCompletionTime = DateTime.UtcNow.AddMinutes(15)
                };

                _logger.LogInformation("Certificate {CertId} submitted for sync to {Retailers}", 
                    request.CertificateNumber, string.Join(", ", enabledRetailers));

                return CreatedAtAction(nameof(GetSyncStatus), new { trackingId }, result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting certificate {CertId}", request.CertificateNumber);
                return StatusCode(500, "Internal server error processing certificate submission");
            }
        }

        /// <summary>
        /// Get synchronization status across all retailers
        /// </summary>
        /// <param name="trackingId">Tracking ID from submission</param>
        /// <returns>Current sync status for all retailers</returns>
        [HttpGet("{trackingId}/status")]
        [ProducesResponseType(typeof(CertificateSyncStatus), 200)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<CertificateSyncStatus>> GetSyncStatus(string trackingId)
        {
            try
            {
                // Mock status for demonstration - replace with actual status lookup
                var status = new CertificateSyncStatus
                {
                    TrackingId = trackingId,
                    OverallStatus = "In Progress",
                    LastUpdated = DateTime.UtcNow,
                    RetailerStatus = new Dictionary<string, RetailerSyncResult>
                    {
                        ["amazon"] = new RetailerSyncResult
                        {
                            Status = "Completed",
                            LastAttempt = DateTime.UtcNow.AddMinutes(-5),
                            Success = true,
                            Message = "Successfully synchronized to Amazon catalog"
                        },
                        ["wayfair"] = new RetailerSyncResult
                        {
                            Status = "In Progress",
                            LastAttempt = DateTime.UtcNow.AddMinutes(-2),
                            Success = null,
                            Message = "Sync in progress"
                        },
                        ["target"] = new RetailerSyncResult
                        {
                            Status = "Failed",
                            LastAttempt = DateTime.UtcNow.AddMinutes(-1),
                            Success = false,
                            Message = "Rate limit exceeded - will retry in 10 minutes",
                            NextRetryAt = DateTime.UtcNow.AddMinutes(10)
                        }
                    }
                };

                return Ok(status);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving sync status for {TrackingId}", trackingId);
                return StatusCode(500, "Error retrieving sync status");
            }
        }

        /// <summary>
        /// Get all certificates for the authenticated tenant
        /// </summary>
        /// <param name="page">Page number (1-based)</param>
        /// <param name="pageSize">Items per page (max 100)</param>
        /// <param name="status">Filter by status</param>
        /// <returns>Paginated list of certificates</returns>
        [HttpGet]
        [ProducesResponseType(typeof(PaginatedResult<CertificateDto>), 200)]
        public async Task<ActionResult<PaginatedResult<CertificateDto>>> GetCertificates(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? status = null)
        {
            pageSize = Math.Min(pageSize, 100); // Limit page size
            
            try
            {
                // Mock data - replace with actual database query
                var certificates = GetMockCertificates()
                    .Where(c => status == null || c.Status.Equals(status, StringComparison.OrdinalIgnoreCase))
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                var result = new PaginatedResult<CertificateDto>
                {
                    Items = certificates,
                    Page = page,
                    PageSize = pageSize,
                    TotalCount = 150, // Mock total
                    TotalPages = (int)Math.Ceiling(150.0 / pageSize)
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving certificates");
                return StatusCode(500, "Error retrieving certificates");
            }
        }

        /// <summary>
        /// Retry failed synchronization for specific retailers
        /// </summary>
        /// <param name="trackingId">Tracking ID</param>
        /// <param name="request">Retry request details</param>
        /// <returns>Updated sync status</returns>
        [HttpPost("{trackingId}/retry")]
        [ProducesResponseType(typeof(CertificateSyncStatus), 200)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<CertificateSyncStatus>> RetrySync(
            string trackingId,
            [FromBody] RetryRequest request)
        {
            try
            {
                _logger.LogInformation("Retrying sync for {TrackingId} on retailers: {Retailers}", 
                    trackingId, string.Join(", ", request.RetailerNames));

                // Queue retry jobs for specified retailers
                // Return updated status
                return await GetSyncStatus(trackingId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrying sync for {TrackingId}", trackingId);
                return StatusCode(500, "Error processing retry request");
            }
        }

        /// <summary>
        /// Get integration health status for all retailers
        /// </summary>
        /// <returns>Health status for each retailer integration</returns>
        [HttpGet("health")]
        [ProducesResponseType(typeof(IntegrationHealthStatus), 200)]
        public async Task<ActionResult<IntegrationHealthStatus>> GetHealth()
        {
            var health = new IntegrationHealthStatus
            {
                OverallStatus = "Healthy",
                LastChecked = DateTime.UtcNow,
                RetailerHealth = new Dictionary<string, RetailerHealth>
                {
                    ["amazon"] = new RetailerHealth
                    {
                        Status = "Healthy",
                        ResponseTime = TimeSpan.FromMilliseconds(250),
                        RateLimitRemaining = 950,
                        RateLimitReset = DateTime.UtcNow.AddHours(1),
                        LastSuccessfulSync = DateTime.UtcNow.AddMinutes(-3)
                    },
                    ["wayfair"] = new RetailerHealth
                    {
                        Status = "Degraded",
                        ResponseTime = TimeSpan.FromMilliseconds(1200),
                        RateLimitRemaining = 45,
                        RateLimitReset = DateTime.UtcNow.AddMinutes(30),
                        LastSuccessfulSync = DateTime.UtcNow.AddMinutes(-15)
                    },
                    ["target"] = new RetailerHealth
                    {
                        Status = "Unhealthy",
                        ResponseTime = null,
                        RateLimitRemaining = 0,
                        RateLimitReset = DateTime.UtcNow.AddMinutes(45),
                        LastSuccessfulSync = DateTime.UtcNow.AddHours(-2),
                        ErrorMessage = "API temporarily unavailable"
                    }
                }
            };

            return Ok(health);
        }

        #region Mock Data - Replace with actual data layer
        
        private List<CertificateDto> GetMockCertificates()
        {
            return new List<CertificateDto>
            {
                new CertificateDto
                {
                    CertificateNumber = "24-1605",
                    CertificationType = "Certified Environmental Facts",
                    CompanyName = "ASSA ABLOY Access & Egress Hardware Group, Inc.",
                    ProductName = "900ET Exit Device Trim with EcoFlex",
                    Status = "Active",
                    EffectiveDate = new DateTime(2024, 9, 1),
                    ExpirationDate = new DateTime(2027, 8, 31),
                    LastSyncDate = DateTime.UtcNow.AddHours(-2),
                    SyncedRetailers = new[] { "amazon", "wayfair" }
                },
                new CertificateDto
                {
                    CertificateNumber = "24-5637",
                    CertificationType = "Recycled Content",
                    CompanyName = "GAF",
                    ProductName = "EnergyGuard Polyiso Roof Insulation",
                    Status = "Active",
                    EffectiveDate = new DateTime(2024, 11, 1),
                    ExpirationDate = new DateTime(2025, 10, 31),
                    LastSyncDate = DateTime.UtcNow.AddHours(-1),
                    SyncedRetailers = new[] { "amazon", "target" }
                }
            };
        }

        #endregion
    }

    #region DTOs - Move to separate files in production
    
    public class CertificateSubmissionRequest
    {
        [Required]
        public string CertificateNumber { get; set; } = string.Empty;
        
        [Required]
        public string CertificationType { get; set; } = string.Empty;
        
        [Required]
        public string CompanyName { get; set; } = string.Empty;
        
        [Required]
        public string ProductName { get; set; } = string.Empty;
        
        public string? ProductType { get; set; }
        public DateTime EffectiveDate { get; set; }
        public DateTime ExpirationDate { get; set; }
        public string? Location { get; set; }
        public string? Country { get; set; }
        
        public Dictionary<string, object>? ProductIdentifiers { get; set; }
        public Dictionary<string, object>? CSFAttributes { get; set; }
        
        public string[]? TargetRetailers { get; set; }
        public Dictionary<string, object>? CustomMappings { get; set; }
    }

    public class CertificateSubmissionResult
    {
        public string TrackingId { get; set; } = string.Empty;
        public string CertificateId { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public List<string> TargetRetailers { get; set; } = new();
        public DateTime SubmittedAt { get; set; }
        public DateTime EstimatedCompletionTime { get; set; }
    }

    public class CertificateSyncStatus
    {
        public string TrackingId { get; set; } = string.Empty;
        public string OverallStatus { get; set; } = string.Empty;
        public DateTime LastUpdated { get; set; }
        public Dictionary<string, RetailerSyncResult> RetailerStatus { get; set; } = new();
    }

    public class RetailerSyncResult
    {
        public string Status { get; set; } = string.Empty;
        public DateTime LastAttempt { get; set; }
        public bool? Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public DateTime? NextRetryAt { get; set; }
    }

    public class CertificateDto
    {
        public string CertificateNumber { get; set; } = string.Empty;
        public string CertificationType { get; set; } = string.Empty;
        public string CompanyName { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime EffectiveDate { get; set; }
        public DateTime ExpirationDate { get; set; }
        public DateTime? LastSyncDate { get; set; }
        public string[] SyncedRetailers { get; set; } = Array.Empty<string>();
    }

    public class PaginatedResult<T>
    {
        public List<T> Items { get; set; } = new();
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public int TotalPages { get; set; }
    }

    public class RetryRequest
    {
        [Required]
        public string[] RetailerNames { get; set; } = Array.Empty<string>();
        public bool ForceRetry { get; set; }
    }

    public class IntegrationHealthStatus
    {
        public string OverallStatus { get; set; } = string.Empty;
        public DateTime LastChecked { get; set; }
        public Dictionary<string, RetailerHealth> RetailerHealth { get; set; } = new();
    }

    public class RetailerHealth
    {
        public string Status { get; set; } = string.Empty;
        public TimeSpan? ResponseTime { get; set; }
        public int RateLimitRemaining { get; set; }
        public DateTime RateLimitReset { get; set; }
        public DateTime? LastSuccessfulSync { get; set; }
        public string? ErrorMessage { get; set; }
    }

    #endregion
}