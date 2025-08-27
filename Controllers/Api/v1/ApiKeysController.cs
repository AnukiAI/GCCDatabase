using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using GCCWebApp.Controllers.Api;
using GCCWebApp.Models;
using GCCWebApp.Models.Api;
using GCCWebApp.Services;
using Asp.Versioning;

namespace GCCWebApp.Controllers.Api.v1
{
    [ApiVersion("1.0")]
    [Authorize(Policy = "AdminOnly")]
    public class ApiKeysController : BaseApiController
    {
        private readonly IApiKeyService _apiKeyService;
        private readonly ILogger<ApiKeysController> _logger;

        public ApiKeysController(IApiKeyService apiKeyService, ILogger<ApiKeysController> logger)
        {
            _apiKeyService = apiKeyService;
            _logger = logger;
        }

        /// <summary>
        /// Get all API keys
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(StandardApiResponse<IEnumerable<object>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetApiKeys()
        {
            try
            {
                var apiKeys = await _apiKeyService.GetAllApiKeysAsync();
                
                var sanitizedKeys = apiKeys.Select(k => new
                {
                    k.Id,
                    k.Name,
                    k.Description,
                    k.IsActive,
                    k.CreatedAt,
                    k.LastUsedAt,
                    k.ExpiresAt,
                    k.CreatedBy,
                    k.RateLimitPerMinute,
                    k.RateLimitPerHour,
                    k.AllowedIPs,
                    k.AllowedOrigins,
                    k.Scopes,
                    IsExpired = k.IsExpired,
                    KeyPreview = k.Key?.Length > 8 ? $"{k.Key[..8]}..." : "***"
                });

                return SuccessResponse(sanitizedKeys);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving API keys");
                return HandleError(ex, "Failed to retrieve API keys");
            }
        }

        /// <summary>
        /// Get an API key by ID
        /// </summary>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(StandardApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetApiKey(int id)
        {
            try
            {
                var apiKey = await _apiKeyService.GetApiKeyByIdAsync(id);
                
                if (apiKey == null)
                    return NotFoundResponse($"API Key with ID {id} not found");

                var sanitizedKey = new
                {
                    apiKey.Id,
                    apiKey.Name,
                    apiKey.Description,
                    apiKey.IsActive,
                    apiKey.CreatedAt,
                    apiKey.LastUsedAt,
                    apiKey.ExpiresAt,
                    apiKey.CreatedBy,
                    apiKey.RateLimitPerMinute,
                    apiKey.RateLimitPerHour,
                    apiKey.AllowedIPs,
                    apiKey.AllowedOrigins,
                    apiKey.Scopes,
                    IsExpired = apiKey.IsExpired,
                    KeyPreview = apiKey.Key?.Length > 8 ? $"{apiKey.Key[..8]}..." : "***"
                };

                return SuccessResponse(sanitizedKey);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving API key {ApiKeyId}", id);
                return HandleError(ex, $"Failed to retrieve API key {id}");
            }
        }

        /// <summary>
        /// Create a new API key
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(ApiResult<object>), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateApiKey([FromBody] CreateApiKeyRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var currentUser = User.Identity?.Name ?? "System";
                
                var apiKey = await _apiKeyService.CreateApiKeyAsync(
                    request.Name,
                    request.Description,
                    currentUser,
                    request.ExpiresAt);

                var response = new
                {
                    apiKey.Id,
                    apiKey.Name,
                    apiKey.Key, // Only shown once during creation
                    apiKey.Description,
                    apiKey.IsActive,
                    apiKey.CreatedAt,
                    apiKey.ExpiresAt,
                    apiKey.CreatedBy,
                    apiKey.RateLimitPerMinute,
                    apiKey.RateLimitPerHour,
                    Message = "API Key created successfully. Save the key securely - it won't be shown again."
                };

                return CreatedResponse(response, $"/api/v1/apikeys/{apiKey.Id}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating API key");
                return HandleError(ex, "Failed to create API key");
            }
        }

        /// <summary>
        /// Revoke an API key
        /// </summary>
        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> RevokeApiKey(int id)
        {
            try
            {
                var success = await _apiKeyService.RevokeApiKeyAsync(id);
                
                if (!success)
                    return NotFoundResponse($"API Key with ID {id} not found");

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error revoking API key {ApiKeyId}", id);
                return HandleError(ex, $"Failed to revoke API key {id}");
            }
        }
    }

    public class CreateApiKeyRequest
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public DateTime? ExpiresAt { get; set; }
    }
}