using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using GCCWebApp.Models;
using GCCWebApp.Models.Api;
using GCCWebApp.Services;

namespace GCCWebApp.Controllers.Api
{
    [ApiController]
    [Route("api/admin/amazon")]
    [Authorize(Policy = "AdminOnly")]
    [Produces("application/json")]
    public class AmazonApiController : ControllerBase
    {
        private readonly IAmazonApiService _amazonApiService;
        private readonly ILogger<AmazonApiController> _logger;

        public AmazonApiController(IAmazonApiService amazonApiService, ILogger<AmazonApiController> logger)
        {
            _amazonApiService = amazonApiService;
            _logger = logger;
        }

        /// <summary>
        /// GET /api/admin/amazon/settings - Load current Amazon API settings
        /// </summary>
        [HttpGet("settings")]
        [ProducesResponseType(typeof(StandardApiResponse<AmazonApiSettings>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetSettings()
        {
            try
            {
                var settings = await _amazonApiService.GetSettingsAsync();

                if (settings == null)
                {
                    // Return default settings if none exist
                    settings = new AmazonApiSettings
                    {
                        AccessKeyId = "",
                        SecretAccessKey = "",
                        RefreshToken = "",
                        Region = "",
                        MarketplaceId = "",
                        IsEnabled = false,
                        SyncIntervalHours = 24,
                        SyncProducts = true,
                        SyncCertificates = true,
                        SyncCompliance = true
                    };
                }

                return Ok(StandardApiResponse<AmazonApiSettings>.SuccessResult(settings));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving Amazon API settings");
                return StatusCode(500, StandardApiResponse<object>.ErrorResult(
                    "Failed to retrieve Amazon API settings",
                    ApiErrorCodes.InternalError));
            }
        }

        /// <summary>
        /// POST /api/admin/amazon/settings - Save Amazon API settings
        /// </summary>
        [HttpPost("settings")]
        [ProducesResponseType(typeof(StandardApiResponse<AmazonApiSettings>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> SaveSettings([FromBody] AmazonApiSettingsDto request)
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

                var settings = new AmazonApiSettings
                {
                    IsEnabled = request.IsEnabled,
                    SyncIntervalHours = request.SyncIntervalHours,
                    AccessKeyId = request.AccessKeyId,
                    SecretAccessKey = request.SecretAccessKey,
                    RefreshToken = request.RefreshToken,
                    Region = request.Region,
                    MarketplaceId = request.MarketplaceId,
                    SyncProducts = request.SyncProducts,
                    SyncCertificates = request.SyncCertificates,
                    SyncCompliance = request.SyncCompliance,
                    WebhookUrl = request.WebhookUrl
                };

                var savedSettings = await _amazonApiService.SaveSettingsAsync(settings, currentUser);

                return Ok(StandardApiResponse<AmazonApiSettings>.SuccessResult(
                    savedSettings,
                    "Amazon API settings saved successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving Amazon API settings");
                return StatusCode(500, StandardApiResponse<object>.ErrorResult(
                    "Failed to save Amazon API settings",
                    ApiErrorCodes.InternalError));
            }
        }

        /// <summary>
        /// POST /api/admin/amazon/test - Test Amazon API connection
        /// </summary>
        [HttpPost("test")]
        [ProducesResponseType(typeof(StandardApiResponse<object>), StatusCodes.Status200OK)]
        public async Task<IActionResult> TestConnection([FromBody] AmazonApiSettingsDto request)
        {
            try
            {
                var settings = new AmazonApiSettings
                {
                    AccessKeyId = request.AccessKeyId,
                    SecretAccessKey = request.SecretAccessKey,
                    RefreshToken = request.RefreshToken,
                    Region = request.Region,
                    MarketplaceId = request.MarketplaceId,
                    IsEnabled = true,
                    SyncIntervalHours = 24,
                    SyncProducts = true,
                    SyncCertificates = true,
                    SyncCompliance = true
                };

                var success = await _amazonApiService.TestConnectionAsync(settings);

                if (success)
                {
                    return Ok(StandardApiResponse<object>.SuccessResult(
                        new { connected = true },
                        "Connection test successful! Amazon API is reachable."));
                }
                else
                {
                    return Ok(StandardApiResponse<object>.ErrorResult(
                        "Connection test failed. Please verify your credentials.",
                        ApiErrorCodes.Unauthorized));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error testing Amazon API connection");
                return Ok(StandardApiResponse<object>.ErrorResult(
                    $"Connection test failed: {ex.Message}",
                    ApiErrorCodes.InternalError));
            }
        }

        /// <summary>
        /// POST /api/admin/amazon/sync - Trigger manual sync
        /// </summary>
        [HttpPost("sync")]
        [ProducesResponseType(typeof(StandardApiResponse<object>), StatusCodes.Status200OK)]
        public async Task<IActionResult> TriggerSync()
        {
            try
            {
                var currentUser = User.Identity?.Name ?? "System";

                var (success, message, recordsProcessed) = await _amazonApiService.TriggerSyncAsync(currentUser);

                if (success)
                {
                    return Ok(StandardApiResponse<object>.SuccessResult(
                        new { recordsProcessed },
                        message));
                }
                else
                {
                    return Ok(StandardApiResponse<object>.ErrorResult(
                        message,
                        ApiErrorCodes.InternalError));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error triggering Amazon sync");
                return StatusCode(500, StandardApiResponse<object>.ErrorResult(
                    $"Sync failed: {ex.Message}",
                    ApiErrorCodes.InternalError));
            }
        }

        /// <summary>
        /// GET /api/admin/amazon/history - Get sync history
        /// </summary>
        [HttpGet("history")]
        [ProducesResponseType(typeof(StandardApiResponse<List<AmazonSyncHistory>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetSyncHistory([FromQuery] int count = 10)
        {
            try
            {
                var history = await _amazonApiService.GetSyncHistoryAsync(count);

                return Ok(StandardApiResponse<List<AmazonSyncHistory>>.SuccessResult(history));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving Amazon sync history");
                return StatusCode(500, StandardApiResponse<object>.ErrorResult(
                    "Failed to retrieve sync history",
                    ApiErrorCodes.InternalError));
            }
        }
    }

    // DTO for request validation
    public class AmazonApiSettingsDto
    {
        public bool IsEnabled { get; set; }
        public int SyncIntervalHours { get; set; } = 24;

        [Required]
        public required string AccessKeyId { get; set; }

        [Required]
        public required string SecretAccessKey { get; set; }

        [Required]
        public required string RefreshToken { get; set; }

        [Required]
        public required string Region { get; set; }

        [Required]
        public required string MarketplaceId { get; set; }

        public bool SyncProducts { get; set; } = true;
        public bool SyncCertificates { get; set; } = true;
        public bool SyncCompliance { get; set; } = true;
        public string? WebhookUrl { get; set; }
    }
}
