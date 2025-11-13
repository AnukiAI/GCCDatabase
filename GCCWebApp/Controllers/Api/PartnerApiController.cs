using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using GCCWebApp.Models;
using GCCWebApp.Models.Api;
using GCCWebApp.Services;
using Microsoft.EntityFrameworkCore;
using GCCWebApp.Data;

namespace GCCWebApp.Controllers.Api
{
    [ApiController]
    [Route("api/admin/partner")]
    [Authorize(Policy = "AdminOnly")]
    [Produces("application/json")]
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
        /// GET /api/admin/partner/list - Get all partners
        /// </summary>
        [HttpGet("list")]
        [ProducesResponseType(typeof(StandardApiResponse<List<Partner>>), StatusCodes.Status200OK)]
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
        /// GET /api/admin/partner/{partnerId}/apikeys - Get API keys for a partner
        /// </summary>
        [HttpGet("{partnerId}/apikeys")]
        [ProducesResponseType(typeof(StandardApiResponse<List<PartnerApiKey>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetPartnerApiKeys(int partnerId)
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
        /// POST /api/admin/partner/{partnerId}/apikey - Generate new API key for a partner
        /// </summary>
        [HttpPost("{partnerId}/apikey")]
        [ProducesResponseType(typeof(StandardApiResponse<PartnerApiKey>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GenerateApiKey(int partnerId, [FromBody] GenerateApiKeyRequest request)
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
        /// DELETE /api/admin/partner/apikey/{id} - Revoke an API key
        /// </summary>
        [HttpDelete("apikey/{id}")]
        [ProducesResponseType(typeof(StandardApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> RevokeApiKey(int id)
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
        /// GET /api/admin/partner/{partnerId}/usage - Get usage statistics for a partner
        /// </summary>
        [HttpGet("{partnerId}/usage")]
        [ProducesResponseType(typeof(StandardApiResponse<Dictionary<string, object>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetUsageStats(int partnerId)
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
        /// GET /api/admin/partner/apikeys/all - Get all API keys (for admin overview)
        /// </summary>
        [HttpGet("apikeys/all")]
        [ProducesResponseType(typeof(StandardApiResponse<List<PartnerApiKey>>), StatusCodes.Status200OK)]
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

    // DTO for API key generation request
    public class GenerateApiKeyRequest
    {
        public string? Description { get; set; }
        public DateTime? ExpiresAt { get; set; }
    }
}
