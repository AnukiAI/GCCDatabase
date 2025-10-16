using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using GCCWebApp.Models;
using GCCWebApp.Models.Api;
using GCCWebApp.Services;
using GreenCircleCertified.ViewModels;
using System.ComponentModel.DataAnnotations;

namespace GCCWebApp.Controllers.Api
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class CertificatesApiController : ControllerBase
    {
        private readonly ILogger<CertificatesApiController> _logger;
        private readonly ICertificateService _certificateService;
        private readonly IValidationService _validationService;
        private readonly IFileUploadService _fileUploadService;

        public CertificatesApiController(
            ILogger<CertificatesApiController> logger,
            ICertificateService certificateService,
            IValidationService validationService,
            IFileUploadService fileUploadService)
        {
            _logger = logger;
            _certificateService = certificateService;
            _validationService = validationService;
            _fileUploadService = fileUploadService;
        }

        /// <summary>
        /// Get certificates with filtering, sorting, and pagination
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(CertificateListResponse), 200)]
        [ProducesResponseType(typeof(ApiErrorResponse), 400)]
        public async Task<ActionResult<CertificateListResponse>> GetCertificates(
            [FromQuery] CertificateListRequest request)
        {
            try
            {
                _logger.LogInformation("API request for certificates with filters: {@Request}", request);

                var result = await _certificateService.GetCertificatesAsync(request);
                
                if (result.Success)
                {
                    return Ok(result);
                }

                return BadRequest(new ApiErrorResponse
                {
                    Message = result.Message,
                    Errors = result.Errors
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving certificates via API");
                return StatusCode(500, new ApiErrorResponse 
                { 
                    Message = "An internal server error occurred while retrieving certificates." 
                });
            }
        }

        /// <summary>
        /// Get a specific certificate by ID
        /// </summary>
        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(CertificateDetailsViewModel), 200)]
        [ProducesResponseType(typeof(ApiErrorResponse), 404)]
        public async Task<ActionResult<CertificateDetailsViewModel>> GetCertificate(int id, 
            [FromQuery] string userRole = "Public")
        {
            try
            {
                _logger.LogInformation("API request for certificate {CertificateId} with role {UserRole}", id, userRole);

                var certificate = await _certificateService.GetCertificateDetailsAsync(id, userRole);
                
                if (certificate == null)
                {
                    return NotFound(new ApiErrorResponse 
                    { 
                        Message = $"Certificate with ID {id} not found." 
                    });
                }

                return Ok(certificate);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving certificate {CertificateId} via API", id);
                return StatusCode(500, new ApiErrorResponse 
                { 
                    Message = "An internal server error occurred while retrieving the certificate." 
                });
            }
        }

        /// <summary>
        /// Get certificate by certificate number
        /// </summary>
        [HttpGet("by-number/{certificateNumber}")]
        [ProducesResponseType(typeof(CertificateDetailsViewModel), 200)]
        [ProducesResponseType(typeof(ApiErrorResponse), 404)]
        public async Task<ActionResult<CertificateDetailsViewModel>> GetCertificateByNumber(
            string certificateNumber, [FromQuery] string userRole = "Public")
        {
            try
            {
                _logger.LogInformation("API request for certificate number {CertificateNumber} with role {UserRole}", 
                    certificateNumber, userRole);

                var certificate = await _certificateService.GetCertificateByCertificateNumberAsync(certificateNumber, userRole);
                
                if (certificate == null)
                {
                    return NotFound(new ApiErrorResponse 
                    { 
                        Message = $"Certificate with number '{certificateNumber}' not found." 
                    });
                }

                return Ok(certificate);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving certificate by number {CertificateNumber} via API", certificateNumber);
                return StatusCode(500, new ApiErrorResponse 
                { 
                    Message = "An internal server error occurred while retrieving the certificate." 
                });
            }
        }

        /// <summary>
        /// Create a new certificate (Admin only)
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(CertificateDetailsViewModel), 201)]
        [ProducesResponseType(typeof(ApiErrorResponse), 400)]
        public async Task<ActionResult<CertificateDetailsViewModel>> CreateCertificate(
            [FromBody] CreateCertificateRequest request)
        {
            try
            {
                _logger.LogInformation("API request to create certificate for company {CompanyId}, product {ProductId}", 
                    request.CompanyId, request.ProductId);

                // Convert request to Certificate model
                var certificate = MapToCertificate(request);

                // Validate certificate
                var validationResult = await _validationService.ValidateCertificateAsync(certificate);
                if (!validationResult.IsValid)
                {
                    return BadRequest(new ApiErrorResponse
                    {
                        Message = "Validation failed.",
                        Errors = validationResult.Errors
                    });
                }

                // Create certificate (convert to CertificateEditViewModel)
                var certificateViewModel = MapToCertificateEditViewModel(certificate);
                var certificateId = await _certificateService.CreateCertificateAsync(certificateViewModel, "Admin");
                var createdCertificate = await _certificateService.GetCertificateDetailsAsync(certificateId, "Admin");
                
                if (createdCertificate == null)
                {
                    return BadRequest(new ApiErrorResponse 
                    { 
                        Message = "Failed to create certificate." 
                    });
                }

                return CreatedAtAction(nameof(GetCertificate), new { id = createdCertificate.Id }, createdCertificate);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating certificate via API");
                return StatusCode(500, new ApiErrorResponse 
                { 
                    Message = "An internal server error occurred while creating the certificate." 
                });
            }
        }

        /// <summary>
        /// Update an existing certificate (Admin only)
        /// </summary>
        [HttpPut("{id:int}")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(CertificateDetailsViewModel), 200)]
        [ProducesResponseType(typeof(ApiErrorResponse), 400)]
        [ProducesResponseType(typeof(ApiErrorResponse), 404)]
        public async Task<ActionResult<CertificateDetailsViewModel>> UpdateCertificate(int id, 
            [FromBody] UpdateCertificateRequest request)
        {
            try
            {
                _logger.LogInformation("API request to update certificate {CertificateId}", id);

                // Check if certificate exists
                var existingCertificate = await _certificateService.GetCertificateDetailsAsync(id, "Admin");
                if (existingCertificate == null)
                {
                    return NotFound(new ApiErrorResponse 
                    { 
                        Message = $"Certificate with ID {id} not found." 
                    });
                }

                // Convert request to Certificate model
                var certificate = MapToCertificate(request);
                certificate.Id = id;

                // Validate certificate
                var validationResult = await _validationService.ValidateCertificateAsync(certificate);
                if (!validationResult.IsValid)
                {
                    return BadRequest(new ApiErrorResponse
                    {
                        Message = "Validation failed.",
                        Errors = validationResult.Errors
                    });
                }

                // Update certificate (convert to CertificateEditViewModel)
                var certificateViewModel = MapToCertificateEditViewModel(certificate);
                var success = await _certificateService.UpdateCertificateAsync(id, certificateViewModel, "Admin");
                
                if (!success)
                {
                    return BadRequest(new ApiErrorResponse 
                    { 
                        Message = "Failed to update certificate." 
                    });
                }
                
                var updatedCertificate = await _certificateService.GetCertificateDetailsAsync(id, "Admin");
                
                if (updatedCertificate == null)
                {
                    return BadRequest(new ApiErrorResponse 
                    { 
                        Message = "Failed to update certificate." 
                    });
                }

                return Ok(updatedCertificate);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating certificate {CertificateId} via API", id);
                return StatusCode(500, new ApiErrorResponse 
                { 
                    Message = "An internal server error occurred while updating the certificate." 
                });
            }
        }

        /// <summary>
        /// Delete a certificate (Admin only)
        /// </summary>
        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(ApiSuccessResponse), 200)]
        [ProducesResponseType(typeof(ApiErrorResponse), 404)]
        public async Task<ActionResult<ApiSuccessResponse>> DeleteCertificate(int id)
        {
            try
            {
                _logger.LogInformation("API request to delete certificate {CertificateId}", id);

                var success = await _certificateService.DeleteCertificateAsync(id, "Admin");
                
                if (!success)
                {
                    return NotFound(new ApiErrorResponse 
                    { 
                        Message = $"Certificate with ID {id} not found or could not be deleted." 
                    });
                }

                return Ok(new ApiSuccessResponse 
                { 
                    Message = "Certificate deleted successfully.",
                    Success = true
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting certificate {CertificateId} via API", id);
                return StatusCode(500, new ApiErrorResponse 
                { 
                    Message = "An internal server error occurred while deleting the certificate." 
                });
            }
        }

        /// <summary>
        /// Upload a certificate file (Admin only)
        /// </summary>
        [HttpPost("{id:int}/upload")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(FileUploadResponse), 200)]
        [ProducesResponseType(typeof(ApiErrorResponse), 400)]
        [ProducesResponseType(typeof(ApiErrorResponse), 404)]
        public async Task<ActionResult<FileUploadResponse>> UploadCertificateFile(int id, IFormFile file)
        {
            try
            {
                _logger.LogInformation("API request to upload file for certificate {CertificateId}", id);

                // Check if certificate exists
                var certificate = await _certificateService.GetCertificateDetailsAsync(id, "Admin");
                if (certificate == null)
                {
                    return NotFound(new ApiErrorResponse 
                    { 
                        Message = $"Certificate with ID {id} not found." 
                    });
                }

                // Upload file
                var uploadResult = await _fileUploadService.UploadCertificateAsync(file, certificate.CertificateNumber);
                
                if (!uploadResult.IsSuccess)
                {
                    return BadRequest(new ApiErrorResponse
                    {
                        Message = "File upload failed.",
                        Errors = uploadResult.Errors
                    });
                }

                // Update certificate with file path (would need to implement this method)
                // await _certificateService.UpdateCertificateFilePathAsync(id, uploadResult.FilePath);

                return Ok(new FileUploadResponse
                {
                    Success = true,
                    FilePath = uploadResult.FilePath,
                    FileName = uploadResult.FileName,
                    FileSize = uploadResult.FileSize,
                    Message = "File uploaded successfully."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading file for certificate {CertificateId} via API", id);
                return StatusCode(500, new ApiErrorResponse 
                { 
                    Message = "An internal server error occurred while uploading the file." 
                });
            }
        }

        /// <summary>
        /// Download a certificate file
        /// Public certificates can be downloaded without authentication
        /// Non-public certificates require authentication
        /// </summary>
        [HttpGet("{id:int}/download")]
        [ProducesResponseType(typeof(FileResult), 200)]
        [ProducesResponseType(typeof(ApiErrorResponse), 404)]
        [ProducesResponseType(typeof(ApiErrorResponse), 401)]
        public async Task<ActionResult> DownloadCertificateFile(int id)
        {
            try
            {
                // Determine user role from authentication context
                var userRole = "Public";
                if (User.Identity?.IsAuthenticated == true)
                {
                    userRole = User.FindFirst("Role")?.Value ??
                               User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value ??
                               "Public";
                }

                _logger.LogInformation("API request to download file for certificate {CertificateId} with role {UserRole}",
                    id, userRole);

                // For public certificates (active status), allow download without auth
                // Partner-specific certificates and non-active certificates will be filtered by the service layer
                // based on the userRole parameter

                var downloadModel = await _certificateService.GetCertificateDownloadAsync(id, userRole);

                if (downloadModel == null)
                {
                    return NotFound(new ApiErrorResponse
                    {
                        Message = $"Certificate file with ID {id} not found or not accessible."
                    });
                }

                return File(downloadModel.FileContent, downloadModel.ContentType, downloadModel.FileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading file for certificate {CertificateId} via API", id);
                return StatusCode(500, new ApiErrorResponse
                {
                    Message = "An internal server error occurred while downloading the file."
                });
            }
        }

        /// <summary>
        /// Export certificates to various formats
        /// Requires Analyst or Admin role
        /// </summary>
        [HttpPost("export")]
        [Authorize(Policy = "AnalystOrAdmin")]
        [ProducesResponseType(typeof(FileResult), 200)]
        [ProducesResponseType(typeof(ApiErrorResponse), 400)]
        [ProducesResponseType(typeof(ApiErrorResponse), 401)]
        public async Task<ActionResult> ExportCertificates([FromBody] CertificateExportRequest request)
        {
            try
            {
                _logger.LogInformation("API request to export certificates in {Format} format", request.Format);

                var listRequest = new CertificateListRequest
                {
                    Page = 1,
                    PageSize = int.MaxValue, // Get all certificates for export
                    UserRole = request.UserRole ?? "Public",
                    Filters = new CertificateSearchFilters
                    {
                        SearchTerm = request.SearchTerm ?? string.Empty
                    }
                };

                var exportData = await _certificateService.ExportCertificatesAsync(listRequest, request.Format);
                
                if (exportData == null)
                {
                    return BadRequest(new ApiErrorResponse 
                    { 
                        Message = "No certificates found to export or export failed." 
                    });
                }

                var contentType = request.Format.ToLower() switch
                {
                    "csv" => "text/csv",
                    "excel" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    "pdf" => "application/pdf",
                    _ => "text/csv"
                };

                var fileName = $"certificates_export_{DateTime.Now:yyyyMMdd_HHmmss}.{request.Format.ToLower()}";
                
                return File(exportData, contentType, fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting certificates in {Format} format", request.Format);
                return StatusCode(500, new ApiErrorResponse 
                { 
                    Message = "An internal server error occurred while exporting certificates." 
                });
            }
        }

        #region Private Helper Methods

        private Certificate MapToCertificate(CreateCertificateRequest request)
        {
            return new Certificate
            {
                Number = request.CertificateNumber ?? string.Empty,
                Status = request.Status ?? "active",
                EffectiveDate = request.EffectiveDate,
                ExpirationDate = request.ExpirationDate,
                Location = request.Location,
                Country = request.Country,
                Asin = request.Asin,
                Upc = request.Upc,
                Gtin = request.Gtin,
                Ean = request.Ean,
                CompanyId = request.CompanyId,
                ProductId = request.ProductId,
                CertificationTypeId = request.CertificationTypeId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
        }

        private Certificate MapToCertificate(UpdateCertificateRequest request)
        {
            return new Certificate
            {
                Number = request.CertificateNumber ?? string.Empty,
                Status = request.Status ?? "active",
                EffectiveDate = request.EffectiveDate,
                ExpirationDate = request.ExpirationDate,
                Location = request.Location,
                Country = request.Country,
                Asin = request.Asin,
                Upc = request.Upc,
                Gtin = request.Gtin,
                Ean = request.Ean,
                CompanyId = request.CompanyId,
                ProductId = request.ProductId,
                CertificationTypeId = request.CertificationTypeId,
                UpdatedAt = DateTime.UtcNow
            };
        }

        private CertificateEditViewModel MapToCertificateEditViewModel(Certificate certificate)
        {
            return new CertificateEditViewModel
            {
                Id = certificate.Id,
                Number = certificate.Number,
                Status = certificate.Status,
                EffectiveDate = certificate.EffectiveDate,
                ExpirationDate = certificate.ExpirationDate,
                Location = certificate.Location,
                Country = certificate.Country,
                Asin = certificate.Asin,
                Upc = certificate.Upc,
                Gtin = certificate.Gtin,
                Ean = certificate.Ean,
                CompanyId = certificate.CompanyId,
                ProductId = certificate.ProductId,
                CertificationTypeId = certificate.CertificationTypeId
            };
        }

        #endregion
    }

    #region Request/Response Models

    public class CreateCertificateRequest
    {
        public string? CertificateNumber { get; set; }
        
        [Required]
        public string? Status { get; set; }
        
        [Required]
        public DateTime EffectiveDate { get; set; }
        
        [Required]
        public DateTime ExpirationDate { get; set; }
        
        public string? Location { get; set; }
        public string? Country { get; set; }
        public string? Asin { get; set; }
        public string? Upc { get; set; }
        public string? Gtin { get; set; }
        public string? Ean { get; set; }
        
        [Required]
        public int CompanyId { get; set; }
        
        [Required]
        public int ProductId { get; set; }
        
        [Required]
        public int CertificationTypeId { get; set; }
    }

    public class UpdateCertificateRequest : CreateCertificateRequest
    {
        // Inherits all properties from CreateCertificateRequest
    }

    public class ApiErrorResponse
    {
        public required string Message { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
        public string? TraceId { get; set; }
    }

    public class ApiSuccessResponse
    {
        public bool Success { get; set; }
        public required string Message { get; set; }
        public object? Data { get; set; }
    }

    public class FileUploadResponse
    {
        public bool Success { get; set; }
        public string? FilePath { get; set; }
        public string? FileName { get; set; }
        public long FileSize { get; set; }
        public required string Message { get; set; }
    }

    #endregion
}