using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GCCWebApp.Data;
using GCCWebApp.Models;
using GCCWebApp.Models.Api;
using Asp.Versioning;
using System.ComponentModel.DataAnnotations;
using GCCWebApp.Attributes;

namespace GCCWebApp.Controllers.Api.v1
{
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/certificates/bulk")]
    [Authorize(AuthenticationSchemes = "ApiKey")]
    [ApiController]
    public class BulkCertificatesController : BaseApiController
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<BulkCertificatesController> _logger;

        public BulkCertificatesController(ApplicationDbContext context, ILogger<BulkCertificatesController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpPost("create")]
        [ProducesResponseType(typeof(StandardApiResponse<BulkOperationResult>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status207MultiStatus)]
        public async Task<IActionResult> BulkCreate([FromBody] List<Certificate> certificates)
        {
            if (certificates == null || !certificates.Any())
            {
                return BadRequestResponse("No certificates provided");
            }

            if (certificates.Count > 100)
            {
                return BadRequestResponse("Maximum 100 certificates can be created at once");
            }

            var result = new BulkOperationResult();
            var successfulCertificates = new List<Certificate>();

            foreach (var certificate in certificates)
            {
                try
                {
                    if (!TryValidateModel(certificate))
                    {
                        result.Failed.Add(new BulkOperationFailure
                        {
                            Index = certificates.IndexOf(certificate),
                            Identifier = certificate.Number,
                            Errors = ModelStateToApiErrors()
                        });
                        continue;
                    }

                    var exists = await _context.Certificates
                        .AnyAsync(c => c.Number == certificate.Number);

                    if (exists)
                    {
                        result.Failed.Add(new BulkOperationFailure
                        {
                            Index = certificates.IndexOf(certificate),
                            Identifier = certificate.Number,
                            Errors = new List<ApiError>
                            {
                                new ApiError(ApiErrorCodes.DuplicateResource,
                                    $"Certificate {certificate.Number} already exists")
                            }
                        });
                        continue;
                    }

                    certificate.CreatedAt = DateTime.UtcNow;
                    certificate.UpdatedAt = DateTime.UtcNow;

                    _context.Certificates.Add(certificate);
                    successfulCertificates.Add(certificate);
                    result.Successful.Add(certificate.Number);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to create certificate {Number}", certificate.Number);
                    result.Failed.Add(new BulkOperationFailure
                    {
                        Index = certificates.IndexOf(certificate),
                        Identifier = certificate.Number,
                        Errors = new List<ApiError>
                        {
                            new ApiError(ApiErrorCodes.InternalError, "Failed to create certificate")
                        }
                    });
                }
            }

            if (successfulCertificates.Any())
            {
                await _context.SaveChangesAsync();
            }

            result.TotalProcessed = certificates.Count;
            result.SuccessCount = result.Successful.Count;
            result.FailureCount = result.Failed.Count;

            if (result.Failed.Any() && result.Successful.Any())
            {
                return StatusCode(StatusCodes.Status207MultiStatus,
                    StandardApiResponse<BulkOperationResult>.SuccessResult(result,
                        "Partial success - some certificates were created"));
            }

            if (result.Failed.Any() && !result.Successful.Any())
            {
                return BadRequest(StandardApiResponse<BulkOperationResult>.ErrorResult(
                    new List<ApiError> { new ApiError(ApiErrorCodes.BadRequest,
                        "All certificate creation attempts failed") }));
            }

            return SuccessResponse(result, $"Successfully created {result.SuccessCount} certificates");
        }

        [HttpPut("update")]
        [ProducesResponseType(typeof(StandardApiResponse<BulkOperationResult>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status207MultiStatus)]
        public async Task<IActionResult> BulkUpdate([FromBody] List<Certificate> certificates)
        {
            if (certificates == null || !certificates.Any())
            {
                return BadRequestResponse("No certificates provided");
            }

            if (certificates.Count > 100)
            {
                return BadRequestResponse("Maximum 100 certificates can be updated at once");
            }

            var result = new BulkOperationResult();

            foreach (var certificate in certificates)
            {
                try
                {
                    if (!TryValidateModel(certificate))
                    {
                        result.Failed.Add(new BulkOperationFailure
                        {
                            Index = certificates.IndexOf(certificate),
                            Identifier = certificate.Number,
                            Errors = ModelStateToApiErrors()
                        });
                        continue;
                    }

                    var existingCert = await _context.Certificates
                        .FirstOrDefaultAsync(c => c.Id == certificate.Id);

                    if (existingCert == null)
                    {
                        result.Failed.Add(new BulkOperationFailure
                        {
                            Index = certificates.IndexOf(certificate),
                            Identifier = certificate.Number,
                            Errors = new List<ApiError>
                            {
                                new ApiError(ApiErrorCodes.NotFound,
                                    $"Certificate with ID {certificate.Id} not found")
                            }
                        });
                        continue;
                    }

                    existingCert.Number = certificate.Number;
                    existingCert.ProductId = certificate.ProductId;
                    existingCert.CompanyId = certificate.CompanyId;
                    existingCert.CertificationTypeId = certificate.CertificationTypeId;
                    existingCert.EffectiveDate = certificate.EffectiveDate;
                    existingCert.ExpirationDate = certificate.ExpirationDate;
                    existingCert.Status = certificate.Status;
                    existingCert.UpdatedAt = DateTime.UtcNow;

                    result.Successful.Add(certificate.Number);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to update certificate {Id}", certificate.Id);
                    result.Failed.Add(new BulkOperationFailure
                    {
                        Index = certificates.IndexOf(certificate),
                        Identifier = certificate.Number,
                        Errors = new List<ApiError>
                        {
                            new ApiError(ApiErrorCodes.InternalError, "Failed to update certificate")
                        }
                    });
                }
            }

            await _context.SaveChangesAsync();

            result.TotalProcessed = certificates.Count;
            result.SuccessCount = result.Successful.Count;
            result.FailureCount = result.Failed.Count;

            if (result.Failed.Any() && result.Successful.Any())
            {
                return StatusCode(StatusCodes.Status207MultiStatus,
                    StandardApiResponse<BulkOperationResult>.SuccessResult(result,
                        "Partial success - some certificates were updated"));
            }

            if (result.Failed.Any() && !result.Successful.Any())
            {
                return BadRequest(StandardApiResponse<BulkOperationResult>.ErrorResult(
                    new List<ApiError> { new ApiError(ApiErrorCodes.BadRequest,
                        "All certificate update attempts failed") }));
            }

            return SuccessResponse(result, $"Successfully updated {result.SuccessCount} certificates");
        }

        [HttpDelete]
        [ProducesResponseType(typeof(StandardApiResponse<BulkOperationResult>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> BulkDelete([FromBody] BulkDeleteRequest request)
        {
            if (request.Ids == null || !request.Ids.Any())
            {
                return BadRequestResponse("No certificate IDs provided");
            }

            if (request.Ids.Count > 100)
            {
                return BadRequestResponse("Maximum 100 certificates can be deleted at once");
            }

            var result = new BulkOperationResult();

            var certificates = await _context.Certificates
                .Where(c => request.Ids.Contains(c.Id))
                .ToListAsync();

            foreach (var certId in request.Ids)
            {
                var cert = certificates.FirstOrDefault(c => c.Id == certId);
                if (cert == null)
                {
                    result.Failed.Add(new BulkOperationFailure
                    {
                        Index = request.Ids.IndexOf(certId),
                        Identifier = certId.ToString(),
                        Errors = new List<ApiError>
                        {
                            new ApiError(ApiErrorCodes.NotFound,
                                $"Certificate with ID {certId} not found")
                        }
                    });
                    continue;
                }

                _context.Certificates.Remove(cert);
                result.Successful.Add(cert.Number);
            }

            await _context.SaveChangesAsync();

            result.TotalProcessed = request.Ids.Count;
            result.SuccessCount = result.Successful.Count;
            result.FailureCount = result.Failed.Count;

            return SuccessResponse(result, $"Successfully deleted {result.SuccessCount} certificates");
        }

        [HttpPost("validate")]
        [ProducesResponseType(typeof(StandardApiResponse<BulkValidationResult>), StatusCodes.Status200OK)]
        public async Task<IActionResult> ValidateBulk([FromBody] List<Certificate> certificates)
        {
            if (certificates == null || !certificates.Any())
            {
                return BadRequestResponse("No certificates provided for validation");
            }

            var result = new BulkValidationResult();

            foreach (var certificate in certificates)
            {
                var validationResult = new CertificateValidationResult
                {
                    Index = certificates.IndexOf(certificate),
                    Identifier = certificate.Number,
                    IsValid = true,
                    Errors = new List<ApiError>()
                };

                if (!TryValidateModel(certificate))
                {
                    validationResult.IsValid = false;
                    validationResult.Errors = ModelStateToApiErrors();
                }

                var exists = await _context.Certificates
                    .AnyAsync(c => c.Number == certificate.Number && c.Id != certificate.Id);

                if (exists)
                {
                    validationResult.IsValid = false;
                    validationResult.Errors.Add(new ApiError(
                        ApiErrorCodes.DuplicateResource,
                        $"Certificate number {certificate.Number} already exists"));
                }

                result.Results.Add(validationResult);
            }

            result.TotalValidated = certificates.Count;
            result.ValidCount = result.Results.Count(r => r.IsValid);
            result.InvalidCount = result.Results.Count(r => !r.IsValid);

            return SuccessResponse(result,
                $"Validation complete: {result.ValidCount} valid, {result.InvalidCount} invalid");
        }
    }

    public class BulkOperationResult
    {
        public int TotalProcessed { get; set; }
        public int SuccessCount { get; set; }
        public int FailureCount { get; set; }
        public List<string> Successful { get; set; } = new();
        public List<BulkOperationFailure> Failed { get; set; } = new();
    }

    public class BulkOperationFailure
    {
        public int Index { get; set; }
        public string Identifier { get; set; } = string.Empty;
        public List<ApiError> Errors { get; set; } = new();
    }

    public class BulkDeleteRequest
    {
        [Required]
        public List<int> Ids { get; set; } = new();
    }

    public class BulkValidationResult
    {
        public int TotalValidated { get; set; }
        public int ValidCount { get; set; }
        public int InvalidCount { get; set; }
        public List<CertificateValidationResult> Results { get; set; } = new();
    }

    public class CertificateValidationResult
    {
        public int Index { get; set; }
        public string Identifier { get; set; } = string.Empty;
        public bool IsValid { get; set; }
        public List<ApiError> Errors { get; set; } = new();
    }
}