using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using GCCWebApp.Models;
using GCCWebApp.Services;
using System.Security.Claims;

namespace GCCWebApp.Controllers.Api
{
    /// <summary>
    /// API Controller for Product ID CRUD operations
    /// Handles all Product ID management for certificates
    /// </summary>
    [Route("api/productids")]
    [ApiController]
    [Authorize(Policy = "AnalystOrAdmin")]
    public class ProductIdApiController : ControllerBase
    {
        private readonly IProductIdService _productIdService;
        private readonly ILogger<ProductIdApiController> _logger;

        public ProductIdApiController(IProductIdService productIdService, ILogger<ProductIdApiController> logger)
        {
            _productIdService = productIdService;
            _logger = logger;
        }

        /// <summary>
        /// Get all product IDs for a certificate
        /// </summary>
        /// <param name="certificateId">Certificate ID</param>
        /// <returns>List of product IDs</returns>
        [HttpGet("certificate/{certificateId}")]
        public async Task<ActionResult> GetProductIdsByCertificate(
            int certificateId,
            [FromQuery] int page = 0,
            [FromQuery] int pageSize = 0)
        {
            try
            {
                var productIds = await _productIdService.GetProductIdsByCertificateAsync(certificateId);
                var totalCount = productIds.Count;

                // If pagination requested, apply it
                if (page > 0 && pageSize > 0)
                {
                    var pagedData = productIds.Skip((page - 1) * pageSize).Take(pageSize).ToList();
                    return Ok(new
                    {
                        success = true,
                        data = pagedData,
                        totalCount,
                        page,
                        pageSize,
                        totalPages = (int)Math.Ceiling((double)totalCount / pageSize),
                        message = "Product IDs retrieved successfully"
                    });
                }

                return Ok(new ApiResponse<List<CertificateProductId>>
                {
                    Success = true,
                    Data = productIds,
                    Message = "Product IDs retrieved successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving product IDs for certificate {CertificateId}", certificateId);
                return StatusCode(500, new ApiResponse<List<CertificateProductId>>
                {
                    Success = false,
                    Message = "Error retrieving product IDs"
                });
            }
        }

        /// <summary>
        /// Get a specific product ID by its ID
        /// </summary>
        /// <param name="id">Product ID</param>
        /// <returns>Product ID details</returns>
        [HttpGet("{id}")]
        public async Task<ActionResult<ApiResponse<CertificateProductId>>> GetProductId(int id)
        {
            try
            {
                var productId = await _productIdService.GetProductIdAsync(id);
                if (productId == null)
                {
                    return NotFound(new ApiResponse<CertificateProductId>
                    {
                        Success = false,
                        Message = "Product ID not found"
                    });
                }

                return Ok(new ApiResponse<CertificateProductId>
                {
                    Success = true,
                    Data = productId,
                    Message = "Product ID retrieved successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving product ID {Id}", id);
                return StatusCode(500, new ApiResponse<CertificateProductId>
                {
                    Success = false,
                    Message = "Error retrieving product ID"
                });
            }
        }

        /// <summary>
        /// Create a new product ID
        /// </summary>
        /// <param name="productId">Product ID data</param>
        /// <returns>Created product ID</returns>
        [HttpPost]
        public async Task<ActionResult<ApiResponse<CertificateProductId>>> CreateProductId([FromBody] CertificateProductId productId)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new ApiResponse<CertificateProductId>
                    {
                        Success = false,
                        Message = "Invalid product ID data",
                        Errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()
                    });
                }

                // Set audit fields
                var userId = GetCurrentUserId();
                productId.CreatedAt = DateTime.UtcNow;
                productId.CreatedBy = userId;

                var isValid = await _productIdService.ValidateProductIdAsync(productId.ProductIdType, productId.ProductIdValue);
                if (!isValid)
                {
                    return BadRequest(new ApiResponse<CertificateProductId>
                    {
                        Success = false,
                        Message = "Product ID validation failed"
                    });
                }

                var createdProductId = await _productIdService.CreateProductIdAsync(productId);

                return CreatedAtAction(nameof(GetProductId), new { id = createdProductId.Id },
                    new ApiResponse<CertificateProductId>
                    {
                        Success = true,
                        Data = createdProductId,
                        Message = "Product ID created successfully"
                    });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating product ID");
                return StatusCode(500, new ApiResponse<CertificateProductId>
                {
                    Success = false,
                    Message = "Error creating product ID"
                });
            }
        }

        /// <summary>
        /// Update an existing product ID
        /// </summary>
        /// <param name="id">Product ID</param>
        /// <param name="productId">Updated product ID data</param>
        /// <returns>Updated product ID</returns>
        [HttpPut("{id}")]
        public async Task<ActionResult<ApiResponse<CertificateProductId>>> UpdateProductId(int id, [FromBody] CertificateProductId productId)
        {
            try
            {
                if (id != productId.Id)
                {
                    return BadRequest(new ApiResponse<CertificateProductId>
                    {
                        Success = false,
                        Message = "Product ID mismatch"
                    });
                }

                if (!ModelState.IsValid)
                {
                    return BadRequest(new ApiResponse<CertificateProductId>
                    {
                        Success = false,
                        Message = "Invalid product ID data",
                        Errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()
                    });
                }

                // Set audit fields
                var userId = GetCurrentUserId();
                productId.UpdatedAt = DateTime.UtcNow;
                productId.UpdatedBy = userId;

                var isValid = await _productIdService.ValidateProductIdAsync(productId.ProductIdType, productId.ProductIdValue);
                if (!isValid)
                {
                    return BadRequest(new ApiResponse<CertificateProductId>
                    {
                        Success = false,
                        Message = "Product ID validation failed"
                    });
                }

                var updatedProductId = await _productIdService.UpdateProductIdAsync(productId);
                if (updatedProductId == null)
                {
                    return NotFound(new ApiResponse<CertificateProductId>
                    {
                        Success = false,
                        Message = "Product ID not found"
                    });
                }

                return Ok(new ApiResponse<CertificateProductId>
                {
                    Success = true,
                    Data = updatedProductId,
                    Message = "Product ID updated successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating product ID {Id}", id);
                return StatusCode(500, new ApiResponse<CertificateProductId>
                {
                    Success = false,
                    Message = "Error updating product ID"
                });
            }
        }

        /// <summary>
        /// Delete a product ID
        /// </summary>
        /// <param name="id">Product ID</param>
        /// <returns>Success status</returns>
        [HttpDelete("{id}")]
        public async Task<ActionResult<ApiResponse<object>>> DeleteProductId(int id)
        {
            try
            {
                var success = await _productIdService.DeleteProductIdAsync(id);
                if (!success)
                {
                    return NotFound(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Product ID not found"
                    });
                }

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Message = "Product ID deleted successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting product ID {Id}", id);
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Error deleting product ID"
                });
            }
        }

        /// <summary>
        /// Import product IDs from CSV file
        /// </summary>
        /// <param name="certificateId">Certificate ID</param>
        /// <param name="file">CSV file</param>
        /// <param name="overwriteExisting">Whether to overwrite existing product IDs</param>
        /// <returns>Import results</returns>
        [HttpPost("certificate/{certificateId}/import")]
        public async Task<ActionResult<ApiResponse<BulkImportResult>>> ImportProductIds(
            int certificateId,
            IFormFile file,
            [FromForm] bool overwriteExisting = false)
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    return BadRequest(new ApiResponse<BulkImportResult>
                    {
                        Success = false,
                        Message = "No file provided"
                    });
                }

                if (!file.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
                {
                    return BadRequest(new ApiResponse<BulkImportResult>
                    {
                        Success = false,
                        Message = "Only CSV files are supported"
                    });
                }

                var userId = GetCurrentUserId();
                using var stream = file.OpenReadStream();
                var result = await _productIdService.ImportFromCsvAsync(certificateId, stream, userId, overwriteExisting);

                return Ok(new ApiResponse<BulkImportResult>
                {
                    Success = true,
                    Data = result,
                    Message = $"Import completed. {result.SuccessCount} records imported, {result.ErrorCount} errors"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error importing product IDs for certificate {CertificateId}", certificateId);
                return StatusCode(500, new ApiResponse<BulkImportResult>
                {
                    Success = false,
                    Message = "Error importing product IDs"
                });
            }
        }

        /// <summary>
        /// Export product IDs to CSV
        /// </summary>
        /// <param name="certificateId">Certificate ID</param>
        /// <returns>CSV file</returns>
        [HttpGet("certificate/{certificateId}/export")]
        public async Task<IActionResult> ExportProductIds(int certificateId)
        {
            try
            {
                var csvData = await _productIdService.ExportToCsvAsync(certificateId);
                // Always return the CSV (even if only headers) - let the user see the empty template
                return File(csvData ?? Array.Empty<byte>(), "text/csv", $"certificate_{certificateId}_product_ids.csv");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting product IDs for certificate {CertificateId}", certificateId);
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Error exporting product IDs"
                });
            }
        }

        /// <summary>
        /// Download sample CSV template
        /// </summary>
        /// <returns>Sample CSV file</returns>
        [HttpGet("sample-csv")]
        public IActionResult DownloadSampleCsv()
        {
            try
            {
                // CR-015: Status removed from sample CSV — auto-derived from parent cert.
                var sampleCsv = "Type,Product ID,Partner,ProductName,ProductSku,Description";
                var sampleData = System.Text.Encoding.UTF8.GetBytes(sampleCsv);
                return File(sampleData, "text/csv", "product_ids_sample.csv");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating sample CSV");
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Error generating sample CSV"
                });
            }
        }

        /// <summary>
        /// Validate a product ID without saving
        /// </summary>
        /// <param name="productId">Product ID to validate</param>
        /// <returns>Validation result</returns>
        [HttpPost("validate")]
        public async Task<ActionResult<ApiResponse<bool>>> ValidateProductId([FromBody] CertificateProductId productId)
        {
            try
            {
                var isValid = await _productIdService.ValidateProductIdAsync(productId.ProductIdType, productId.ProductIdValue);
                return Ok(new ApiResponse<bool>
                {
                    Success = true,
                    Data = isValid,
                    Message = isValid ? "Product ID is valid" : "Product ID validation failed"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating product ID");
                return StatusCode(500, new ApiResponse<bool>
                {
                    Success = false,
                    Message = "Error validating product ID"
                });
            }
        }

        /// <summary>
        /// Get current user ID from claims
        /// </summary>
        /// <returns>User ID or null</returns>
        private int? GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(userIdClaim, out int userId))
            {
                return userId;
            }
            return null;
        }
    }

    /// <summary>
    /// Standard API response wrapper
    /// </summary>
    /// <typeparam name="T">Response data type</typeparam>
    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public T? Data { get; set; }
        public string? Message { get; set; }
        public List<string>? Errors { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}