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
        /// Get product IDs for a certificate. CR-018: when paging params are
        /// supplied (page>0 &amp; pageSize>0), pushes sort, filter, and search
        /// into SQL and returns one page of rows with totals. When neither
        /// param is supplied, returns the full list (preserved for export
        /// and any back-compat callers).
        /// </summary>
        [HttpGet("certificate/{certificateId}")]
        public async Task<ActionResult> GetProductIdsByCertificate(
            int certificateId,
            [FromQuery] int page = 0,
            [FromQuery] int pageSize = 0,
            [FromQuery] string? sort = null,
            [FromQuery] string? dir = null,
            [FromQuery] string? search = null,
            [FromQuery] string? type = null,
            [FromQuery] string? partner = null)
        {
            try
            {
                // DEF-121: DTO projection so User credential fields never reach JSON.

                // CR-018: Server-paged path.
                if (page > 0 && pageSize > 0)
                {
                    var paged = await _productIdService.GetProductIdsByCertificatePagedAsync(
                        certificateId, page, pageSize, sort, dir, search, type, partner);

                    return Ok(new
                    {
                        success = true,
                        data = paged.Items.Select(ProductIdResponseDto.FromEntity).ToList(),
                        totalCount = paged.TotalCount,
                        page = paged.Page,
                        pageSize = paged.PageSize,
                        totalPages = paged.TotalPages,
                        message = "Product IDs retrieved successfully"
                    });
                }

                // Un-paged path — used by export and legacy callers. Stays in
                // memory because the export needs the full collection anyway.
                var productIds = await _productIdService.GetProductIdsByCertificateAsync(certificateId);
                var dtos = productIds.Select(ProductIdResponseDto.FromEntity).ToList();

                return Ok(new ApiResponse<List<ProductIdResponseDto>>
                {
                    Success = true,
                    Data = dtos,
                    Message = "Product IDs retrieved successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving product IDs for certificate {CertificateId}", certificateId);
                return StatusCode(500, new ApiResponse<List<ProductIdResponseDto>>
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
        public async Task<ActionResult<ApiResponse<ProductIdResponseDto>>> GetProductId(int id)
        {
            try
            {
                var productId = await _productIdService.GetProductIdAsync(id);
                if (productId == null)
                {
                    return NotFound(new ApiResponse<ProductIdResponseDto>
                    {
                        Success = false,
                        Message = "Product ID not found"
                    });
                }

                return Ok(new ApiResponse<ProductIdResponseDto>
                {
                    Success = true,
                    Data = ProductIdResponseDto.FromEntity(productId),
                    Message = "Product ID retrieved successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving product ID {Id}", id);
                return StatusCode(500, new ApiResponse<ProductIdResponseDto>
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
        public async Task<ActionResult<ApiResponse<ProductIdResponseDto>>> CreateProductId([FromBody] CertificateProductId productId)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new ApiResponse<ProductIdResponseDto>
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
                    return BadRequest(new ApiResponse<ProductIdResponseDto>
                    {
                        Success = false,
                        Message = "Product ID validation failed"
                    });
                }

                var createdProductId = await _productIdService.CreateProductIdAsync(productId);

                // DEF-121: re-fetch with includes so the DTO can populate CreatedByUser.Username
                // for the immediate row render. Falls back to the create result if re-fetch fails.
                var hydrated = await _productIdService.GetProductIdAsync(createdProductId.Id) ?? createdProductId;

                return CreatedAtAction(nameof(GetProductId), new { id = createdProductId.Id },
                    new ApiResponse<ProductIdResponseDto>
                    {
                        Success = true,
                        Data = ProductIdResponseDto.FromEntity(hydrated),
                        Message = "Product ID created successfully"
                    });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating product ID");
                return StatusCode(500, new ApiResponse<ProductIdResponseDto>
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
        public async Task<ActionResult<ApiResponse<ProductIdResponseDto>>> UpdateProductId(int id, [FromBody] CertificateProductId productId)
        {
            try
            {
                if (id != productId.Id)
                {
                    return BadRequest(new ApiResponse<ProductIdResponseDto>
                    {
                        Success = false,
                        Message = "Product ID mismatch"
                    });
                }

                if (!ModelState.IsValid)
                {
                    return BadRequest(new ApiResponse<ProductIdResponseDto>
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
                    return BadRequest(new ApiResponse<ProductIdResponseDto>
                    {
                        Success = false,
                        Message = "Product ID validation failed"
                    });
                }

                var updatedProductId = await _productIdService.UpdateProductIdAsync(productId);
                if (updatedProductId == null)
                {
                    return NotFound(new ApiResponse<ProductIdResponseDto>
                    {
                        Success = false,
                        Message = "Product ID not found"
                    });
                }

                // DEF-121: re-fetch with includes so the DTO can populate user nav properties.
                var hydrated = await _productIdService.GetProductIdAsync(updatedProductId.Id) ?? updatedProductId;

                return Ok(new ApiResponse<ProductIdResponseDto>
                {
                    Success = true,
                    Data = ProductIdResponseDto.FromEntity(hydrated),
                    Message = "Product ID updated successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating product ID {Id}", id);
                return StatusCode(500, new ApiResponse<ProductIdResponseDto>
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
        /// DEF-194 cross-page completion: bulk-delete product IDs for a cert
        /// in a single request. Two modes:
        ///   { ids: [1,2,3] }                          — delete just these IDs
        ///   { deleteAll: true, search, type, partner } — delete every row on
        ///     the cert that matches the supplied filters (empty filters =
        ///     wipe everything on the cert).
        /// The service constrains all deletes to certificateId, so a forged
        /// payload can't reach product IDs on a different cert.
        /// </summary>
        [HttpPost("certificate/{certificateId}/bulk-delete")]
        public async Task<ActionResult<ApiResponse<object>>> BulkDeleteProductIds(
            int certificateId,
            [FromBody] BulkProductIdDeleteRequest request)
        {
            try
            {
                if (request == null)
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Request body is required"
                    });
                }

                if (!request.DeleteAll && (request.Ids == null || request.Ids.Count == 0))
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Either ids or deleteAll must be provided"
                    });
                }

                var deleted = await _productIdService.BulkDeleteProductIdsAsync(
                    certificateId,
                    request.DeleteAll ? null : request.Ids,
                    request.Search,
                    request.Type,
                    request.Partner);

                return Ok(new ApiResponse<object>
                {
                    Success = true,
                    Data = new { deleted },
                    Message = $"Deleted {deleted} product ID(s)"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error bulk-deleting product IDs for certificate {CertificateId}",
                    certificateId);
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "Error processing bulk delete"
                });
            }
        }

        /// <summary>
        /// Bulk-create product IDs from a JSON payload of pre-parsed items.
        ///
        /// DEF-112: the previous frontend made one POST per row, hitting the
        /// AspNetCoreRateLimit cap of 100/min for */api/* once row counts crossed
        /// 100. This endpoint commits the whole batch in a single request.
        /// </summary>
        [HttpPost("certificate/{certificateId}/bulk")]
        public async Task<ActionResult<ApiResponse<BulkImportResult>>> BulkCreateProductIds(
            int certificateId,
            [FromBody] BulkProductIdImportRequest request)
        {
            try
            {
                if (request?.Items == null || request.Items.Count == 0)
                {
                    return BadRequest(new ApiResponse<BulkImportResult>
                    {
                        Success = false,
                        Message = "No items provided"
                    });
                }

                var userId = GetCurrentUserId();
                var result = await _productIdService.ImportProductIdsAsync(
                    certificateId, request.Items, userId, request.OverwriteExisting);

                return Ok(new ApiResponse<BulkImportResult>
                {
                    Success = true,
                    Data = result,
                    Message = $"Bulk import completed. {result.SuccessCount} succeeded, {result.ErrorCount} failed"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error bulk-importing product IDs for certificate {CertificateId}",
                    certificateId);
                return StatusCode(500, new ApiResponse<BulkImportResult>
                {
                    Success = false,
                    Message = "Error processing bulk import"
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
                // DEF-111: column order matches the on-screen Product ID Manager table
                // and the in-memory export.
                var sampleCsv = "Type,Product ID,Product Name,SKU,Partner,Description";
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
    /// JSON body for the bulk-create endpoint. Keeps the API surface friendly to
    /// the frontend (no multipart upload) and lets the caller honor user
    /// per-row deselection in the import-preview modal.
    /// </summary>
    public class BulkProductIdImportRequest
    {
        public List<ProductIdImportItem> Items { get; set; } = new();
        public bool OverwriteExisting { get; set; } = false;
    }

    /// <summary>
    /// JSON body for the bulk-delete endpoint (DEF-194 cross-page).
    /// Either supply Ids (specific rows) OR set DeleteAll=true with optional
    /// Search/Type/Partner filters that mirror the table's current filter chips.
    /// </summary>
    public class BulkProductIdDeleteRequest
    {
        public List<int>? Ids { get; set; }
        public bool DeleteAll { get; set; } = false;
        public string? Search { get; set; }
        public string? Type { get; set; }
        public string? Partner { get; set; }
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