using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using GCCWebApp.Models;
using GCCWebApp.Models.Api;
using GCCWebApp.Services;
using Microsoft.EntityFrameworkCore;
using GCCWebApp.Data;
using Swashbuckle.AspNetCore.Annotations;

namespace GCCWebApp.Controllers.Api
{
    /// <summary>
    /// Partner API Management
    /// </summary>
    /// <remarks>
    /// Provides administrative endpoints for managing partner API access, API keys, and usage statistics.
    /// Partners (Amazon, Novi, etc.) use API keys to access GCC data through dedicated endpoints.
    ///
    /// ## Features
    /// - Generate and revoke partner API keys
    /// - View partner-specific usage statistics
    /// - Manage partner API access permissions
    /// - Track API usage and rate limits
    ///
    /// ## Authentication
    /// All endpoints require Admin role authentication via:
    /// - Cookie-based session (GCC.Session)
    /// - JWT Bearer token
    ///
    /// ## Rate Limits
    /// Partner API keys have higher rate limits (2,000 requests/hour) compared to standard API keys.
    /// </remarks>
    [ApiController]
    [Route("api/admin/partner")]
    [Authorize(Policy = "AdminOnly")]
    [Produces("application/json")]
    [SwaggerTag("Manage partner API access, generate/revoke API keys, and view usage statistics")]
    public class PartnerApiController : ControllerBase
    {
        private readonly IPartnerApiKeyService _partnerApiKeyService;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<PartnerApiController> _logger;

        public PartnerApiController(
            IPartnerApiKeyService partnerApiKeyService,
            ApplicationDbContext context,
            ILogger<PartnerApiController> logger)
        {
            _partnerApiKeyService = partnerApiKeyService;
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Get all partners
        /// </summary>
        /// <remarks>
        /// Retrieves a list of all partners (Amazon, Novi, etc.) configured in the system.
        /// Use this endpoint to populate partner selection dropdowns or display partner information.
        ///
        /// **Sample Request:**
        /// ```
        /// GET /api/admin/partner/list
        /// Authorization: Bearer {token}
        /// ```
        ///
        /// **Sample Response:**
        /// ```json
        /// {
        ///   "success": true,
        ///   "data": [
        ///     {
        ///       "id": 1,
        ///       "name": "Amazon",
        ///       "isActive": true
        ///     },
        ///     {
        ///       "id": 2,
        ///       "name": "Novi",
        ///       "isActive": true
        ///     }
        ///   ]
        /// }
        /// ```
        /// </remarks>
        /// <response code="200">Successfully retrieved partners list</response>
        /// <response code="401">Unauthorized - Invalid or missing authentication</response>
        /// <response code="403">Forbidden - Requires Admin role</response>
        /// <response code="500">Internal server error</response>
        [HttpGet("list")]
        [SwaggerOperation(
            Summary = "Get all partners",
            Description = "Retrieves a list of all partners configured in the system",
            OperationId = "GetPartners",
            Tags = new[] { "Partner API Management" }
        )]
        [ProducesResponseType(typeof(StandardApiResponse<List<Partner>>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetPartners()
        {
            try
            {
                var partners = await _context.Partners
                    .OrderBy(p => p.Name)
                    .ToListAsync();

                return Ok(StandardApiResponse<List<Partner>>.SuccessResult(partners));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving partners");
                return StatusCode(500, StandardApiResponse<object>.ErrorResult(
                    "Failed to retrieve partners",
                    ApiErrorCodes.InternalError));
            }
        }

        /// <summary>
        /// Get API keys for a specific partner
        /// </summary>
        /// <remarks>
        /// Retrieves all API keys associated with a specific partner, including active and revoked keys.
        /// Use this to view, audit, or manage partner API access.
        ///
        /// **Sample Request:**
        /// ```
        /// GET /api/admin/partner/1/apikeys
        /// Authorization: Bearer {token}
        /// ```
        ///
        /// **Sample Response:**
        /// ```json
        /// {
        ///   "success": true,
        ///   "data": [
        ///     {
        ///       "id": 1,
        ///       "partnerId": 1,
        ///       "partnerName": "Amazon",
        ///       "apiKey": "amz_1a2b3c4d5e6f...",
        ///       "description": "Production API key",
        ///       "isActive": true,
        ///       "createdAt": "2025-01-15T10:30:00Z",
        ///       "expiresAt": "2026-01-15T10:30:00Z",
        ///       "lastUsedAt": "2025-11-13T14:22:00Z"
        ///     }
        ///   ]
        /// }
        /// ```
        /// </remarks>
        /// <param name="partnerId">The unique identifier of the partner</param>
        /// <response code="200">Successfully retrieved API keys</response>
        /// <response code="401">Unauthorized - Invalid or missing authentication</response>
        /// <response code="403">Forbidden - Requires Admin role</response>
        /// <response code="404">Partner not found</response>
        /// <response code="500">Internal server error</response>
        [HttpGet("{partnerId}/apikeys")]
        [SwaggerOperation(
            Summary = "Get API keys for a partner",
            Description = "Retrieves all API keys (active and revoked) for a specific partner",
            OperationId = "GetPartnerApiKeys",
            Tags = new[] { "Partner API Management" }
        )]
        [ProducesResponseType(typeof(StandardApiResponse<List<PartnerApiKey>>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetPartnerApiKeys([FromRoute] int partnerId)
        {
            try
            {
                var apiKeys = await _partnerApiKeyService.GetApiKeysByPartnerAsync(partnerId);
                return Ok(StandardApiResponse<List<PartnerApiKey>>.SuccessResult(apiKeys));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving API keys for partner {PartnerId}", partnerId);
                return StatusCode(500, StandardApiResponse<object>.ErrorResult(
                    "Failed to retrieve API keys",
                    ApiErrorCodes.InternalError));
            }
        }

        /// <summary>
        /// Generate a new API key for a partner
        /// </summary>
        /// <remarks>
        /// Creates a new API key for the specified partner with optional description and expiration date.
        /// The generated API key should be stored securely by the partner as it cannot be retrieved again.
        ///
        /// **Sample Request:**
        /// ```
        /// POST /api/admin/partner/1/apikey
        /// Authorization: Bearer {token}
        /// Content-Type: application/json
        ///
        /// {
        ///   "description": "Production API key for certificate access",
        ///   "expiresAt": "2026-01-15T00:00:00Z"
        /// }
        /// ```
        ///
        /// **Sample Response:**
        /// ```json
        /// {
        ///   "success": true,
        ///   "message": "API key generated successfully",
        ///   "data": {
        ///     "id": 5,
        ///     "partnerId": 1,
        ///     "partnerName": "Amazon",
        ///     "apiKey": "amz_9f8e7d6c5b4a3210...",
        ///     "description": "Production API key for certificate access",
        ///     "isActive": true,
        ///     "createdAt": "2025-11-13T15:30:00Z",
        ///     "createdBy": "admin",
        ///     "expiresAt": "2026-01-15T00:00:00Z"
        ///   }
        /// }
        /// ```
        ///
        /// **Important Notes:**
        /// - The API key is only shown once during generation
        /// - Store the API key securely - it cannot be retrieved later
        /// - If the key is lost, generate a new one and revoke the old one
        /// - Partner API keys have a higher rate limit (2,000 requests/hour)
        /// </remarks>
        /// <param name="partnerId">The unique identifier of the partner</param>
        /// <param name="request">API key generation request containing description and optional expiration</param>
        /// <response code="200">API key generated successfully</response>
        /// <response code="400">Bad request - Invalid partner ID or request data</response>
        /// <response code="401">Unauthorized - Invalid or missing authentication</response>
        /// <response code="403">Forbidden - Requires Admin role</response>
        /// <response code="500">Internal server error</response>
        [HttpPost("{partnerId}/apikey")]
        [SwaggerOperation(
            Summary = "Generate new API key",
            Description = "Creates a new API key for a partner with optional description and expiration date",
            OperationId = "GenerateApiKey",
            Tags = new[] { "Partner API Management" }
        )]
        [ProducesResponseType(typeof(StandardApiResponse<PartnerApiKey>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GenerateApiKey([FromRoute] int partnerId, [FromBody] GenerateApiKeyRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(StandardApiResponse<object>.ErrorResult(
                        "Invalid request data",
                        ApiErrorCodes.ValidationError));
                }

                var currentUser = User.Identity?.Name ?? "System";

                var apiKey = await _partnerApiKeyService.GenerateApiKeyAsync(
                    partnerId,
                    request.Description,
                    request.ExpiresAt,
                    currentUser);

                return Ok(StandardApiResponse<PartnerApiKey>.SuccessResult(
                    apiKey,
                    "API key generated successfully"));
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid partner ID {PartnerId}", partnerId);
                return BadRequest(StandardApiResponse<object>.ErrorResult(
                    ex.Message,
                    ApiErrorCodes.ValidationError));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating API key for partner {PartnerId}", partnerId);
                return StatusCode(500, StandardApiResponse<object>.ErrorResult(
                    "Failed to generate API key",
                    ApiErrorCodes.InternalError));
            }
        }

        /// <summary>
        /// Revoke a partner API key
        /// </summary>
        /// <remarks>
        /// Immediately revokes an API key, preventing it from being used for authentication.
        /// This action cannot be undone - you must generate a new key if access is needed again.
        ///
        /// **Sample Request:**
        /// ```
        /// DELETE /api/admin/partner/apikey/5
        /// Authorization: Bearer {token}
        /// ```
        ///
        /// **Sample Response:**
        /// ```json
        /// {
        ///   "success": true,
        ///   "message": "API key revoked successfully",
        ///   "data": {
        ///     "revoked": true
        ///   }
        /// }
        /// ```
        ///
        /// **Use Cases:**
        /// - API key has been compromised or exposed
        /// - Partner no longer needs access
        /// - Regular key rotation for security
        /// - Migrating to a new API key
        ///
        /// **Important Notes:**
        /// - Revocation is immediate and cannot be reversed
        /// - All requests using the revoked key will fail with 401 Unauthorized
        /// - Revoked keys are kept in the system for audit purposes
        /// </remarks>
        /// <param name="id">The unique identifier of the API key to revoke</param>
        /// <response code="200">API key revoked successfully</response>
        /// <response code="401">Unauthorized - Invalid or missing authentication</response>
        /// <response code="403">Forbidden - Requires Admin role</response>
        /// <response code="404">API key not found</response>
        /// <response code="500">Internal server error</response>
        [HttpDelete("apikey/{id}")]
        [SwaggerOperation(
            Summary = "Revoke API key",
            Description = "Immediately revokes a partner API key, preventing further use",
            OperationId = "RevokeApiKey",
            Tags = new[] { "Partner API Management" }
        )]
        [ProducesResponseType(typeof(StandardApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> RevokeApiKey([FromRoute] int id)
        {
            try
            {
                var currentUser = User.Identity?.Name ?? "System";

                var success = await _partnerApiKeyService.RevokeApiKeyAsync(id, currentUser);

                if (!success)
                {
                    return NotFound(StandardApiResponse<object>.ErrorResult(
                        "API key not found",
                        ApiErrorCodes.NotFound));
                }

                return Ok(StandardApiResponse<object>.SuccessResult(
                    new { revoked = true },
                    "API key revoked successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error revoking API key {Id}", id);
                return StatusCode(500, StandardApiResponse<object>.ErrorResult(
                    "Failed to revoke API key",
                    ApiErrorCodes.InternalError));
            }
        }

        /// <summary>
        /// Get usage statistics for a partner
        /// </summary>
        /// <remarks>
        /// Retrieves detailed usage statistics for a specific partner, including API call counts,
        /// rate limit information, and usage trends.
        ///
        /// **Sample Request:**
        /// ```
        /// GET /api/admin/partner/1/usage
        /// Authorization: Bearer {token}
        /// ```
        ///
        /// **Sample Response:**
        /// ```json
        /// {
        ///   "success": true,
        ///   "data": {
        ///     "partnerId": 1,
        ///     "partnerName": "Amazon",
        ///     "totalRequests": 15420,
        ///     "requestsToday": 342,
        ///     "requestsThisMonth": 8765,
        ///     "activeApiKeys": 2,
        ///     "revokedApiKeys": 1,
        ///     "rateLimit": 2000,
        ///     "rateLimitRemaining": 1658,
        ///     "lastRequestAt": "2025-11-13T15:45:00Z",
        ///     "topEndpoints": [
        ///       {
        ///         "endpoint": "/api/v1/certificates",
        ///         "count": 8520
        ///       },
        ///       {
        ///         "endpoint": "/api/v1/products",
        ///         "count": 4230
        ///       }
        ///     ]
        ///   }
        /// }
        /// ```
        ///
        /// **Use Cases:**
        /// - Monitor partner API usage and trends
        /// - Identify potential issues or unusual patterns
        /// - Plan capacity and rate limit adjustments
        /// - Generate usage reports for billing or analytics
        /// </remarks>
        /// <param name="partnerId">The unique identifier of the partner</param>
        /// <response code="200">Successfully retrieved usage statistics</response>
        /// <response code="401">Unauthorized - Invalid or missing authentication</response>
        /// <response code="403">Forbidden - Requires Admin role</response>
        /// <response code="404">Partner not found</response>
        /// <response code="500">Internal server error</response>
        [HttpGet("{partnerId}/usage")]
        [SwaggerOperation(
            Summary = "Get partner usage statistics",
            Description = "Retrieves detailed API usage statistics for a specific partner",
            OperationId = "GetUsageStats",
            Tags = new[] { "Partner API Management" }
        )]
        [ProducesResponseType(typeof(StandardApiResponse<Dictionary<string, object>>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetUsageStats([FromRoute] int partnerId)
        {
            try
            {
                var stats = await _partnerApiKeyService.GetUsageStatsAsync(partnerId);
                return Ok(StandardApiResponse<Dictionary<string, object>>.SuccessResult(stats));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving usage stats for partner {PartnerId}", partnerId);
                return StatusCode(500, StandardApiResponse<object>.ErrorResult(
                    "Failed to retrieve usage statistics",
                    ApiErrorCodes.InternalError));
            }
        }

        /// <summary>
        /// Get all partner API keys (admin overview)
        /// </summary>
        /// <remarks>
        /// Retrieves all API keys across all partners for administrative overview and monitoring.
        /// Useful for auditing, security reviews, and managing API access across the entire system.
        ///
        /// **Sample Request:**
        /// ```
        /// GET /api/admin/partner/apikeys/all
        /// Authorization: Bearer {token}
        /// ```
        ///
        /// **Sample Response:**
        /// ```json
        /// {
        ///   "success": true,
        ///   "data": [
        ///     {
        ///       "id": 1,
        ///       "partnerId": 1,
        ///       "partnerName": "Amazon",
        ///       "apiKey": "amz_1a2b3c4d...",
        ///       "description": "Production key",
        ///       "isActive": true,
        ///       "createdAt": "2025-01-15T10:30:00Z",
        ///       "expiresAt": "2026-01-15T10:30:00Z",
        ///       "lastUsedAt": "2025-11-13T14:22:00Z"
        ///     },
        ///     {
        ///       "id": 2,
        ///       "partnerId": 2,
        ///       "partnerName": "Novi",
        ///       "apiKey": "novi_9f8e7d6c...",
        ///       "description": "Staging key",
        ///       "isActive": true,
        ///       "createdAt": "2025-02-20T14:15:00Z",
        ///       "expiresAt": "2026-02-20T14:15:00Z",
        ///       "lastUsedAt": "2025-11-12T09:30:00Z"
        ///     }
        ///   ]
        /// }
        /// ```
        ///
        /// **Use Cases:**
        /// - System-wide API key audit
        /// - Identify expired or unused keys
        /// - Monitor all partner API access
        /// - Generate comprehensive security reports
        /// - Review API key lifecycle across partners
        /// </remarks>
        /// <response code="200">Successfully retrieved all API keys</response>
        /// <response code="401">Unauthorized - Invalid or missing authentication</response>
        /// <response code="403">Forbidden - Requires Admin role</response>
        /// <response code="500">Internal server error</response>
        [HttpGet("apikeys/all")]
        [SwaggerOperation(
            Summary = "Get all partner API keys",
            Description = "Retrieves all API keys across all partners for administrative overview",
            OperationId = "GetAllApiKeys",
            Tags = new[] { "Partner API Management" }
        )]
        [ProducesResponseType(typeof(StandardApiResponse<List<PartnerApiKey>>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetAllApiKeys()
        {
            try
            {
                var apiKeys = await _partnerApiKeyService.GetAllApiKeysAsync();
                return Ok(StandardApiResponse<List<PartnerApiKey>>.SuccessResult(apiKeys));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all API keys");
                return StatusCode(500, StandardApiResponse<object>.ErrorResult(
                    "Failed to retrieve API keys",
                    ApiErrorCodes.InternalError));
            }
        }
    }

    /// <summary>
    /// Request model for generating a new partner API key
    /// </summary>
    public class GenerateApiKeyRequest
    {
        /// <summary>
        /// Optional description for the API key (e.g., "Production key for certificate access")
        /// </summary>
        /// <example>Production API key for certificate access</example>
        public string? Description { get; set; }

        /// <summary>
        /// Optional expiration date for the API key. If not provided, the key will not expire.
        /// </summary>
        /// <example>2026-01-15T00:00:00Z</example>
        public DateTime? ExpiresAt { get; set; }
    }
}
