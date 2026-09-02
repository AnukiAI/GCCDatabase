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
    // DEF-131: previously this controller used `[Authorize(AuthenticationSchemes = "ApiKey")]`
    // which let any Partner API key holder POST/PUT/DELETE certificates. Partners are read-only
    // per the Amazon Developer Guide contract; bulk mutation is internal-only. Switched to the
    // AnalystOrAdmin policy (default cookie scheme) so Partner API keys are rejected. No code in
    // the repo calls these endpoints, so the lockdown has zero functional impact internally.
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/certificates/bulk")]
    [Authorize(Policy = "AnalystOrAdmin")]
    [ApiController]
    public class BulkCertificatesController : BaseApiController
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<BulkCertificatesController> _logger;

        // DEF-337 (completion): bulk deletes route through the certificate service so they are
        // audited and capture cascade children exactly as single deletes do.
        private readonly GCCWebApp.Services.ICertificateService _certificateService;

        // DEF-391: bulk create and bulk update wrote to the context with no audit row, so the
        // one endpoint that can change 100 certificates in a single call was also the one that
        // left no record of it. Delete was already covered (DEF-337, above).
        private readonly GCCWebApp.Services.Audit.IAuditService _auditService;

        public BulkCertificatesController(
            ApplicationDbContext context,
            ILogger<BulkCertificatesController> logger,
            GCCWebApp.Services.ICertificateService certificateService,
            GCCWebApp.Services.Audit.IAuditService auditService)
        {
            _context = context;
            _logger = logger;
            _certificateService = certificateService;
            _auditService = auditService;
        }

        /// <summary>
        /// DEF-391: one audit write for the bulk endpoints, per certificate.
        ///
        /// Called AFTER the single SaveChangesAsync that commits the whole batch — a bulk call
        /// is one transaction, and a half-audited batch would be worse than an unaudited one.
        /// Failures are logged, never thrown: a partner or analyst must not receive a 500 for a
        /// batch that actually committed.
        /// </summary>
        private async Task AuditBulkChangeAsync(
            string action, Certificate? before, Certificate after)
        {
            try
            {
                await _auditService.LogEntityChangeAsync(
                    after.Id.ToString(),
                    after.Number,
                    action,
                    before,
                    after,
                    0,                                   // resolved from the principal / service account
                    User?.Identity?.Name ?? "Bulk API",
                    reason: "Bulk API operation",
                    changeSource: AuditChangeSource.API);
            }
            catch (Exception auditEx)
            {
                _logger.LogError(auditEx,
                    "AUDIT GAP: certificate {Number} (id {Id}) was changed by a bulk {Action} but the audit row was not written",
                    after.Number, after.Id, action);
            }
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

                // DEF-391: audit after the save, when every row has its assigned Id.
                foreach (var created in successfulCertificates)
                {
                    await AuditBulkChangeAsync(AuditAction.Create, null, created);
                }
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

            // DEF-391: (stored state before the edit, entity after the edit) per row, so the
            // audit can be written once the batch commits. Kept as pairs rather than audited
            // inline because the SaveChangesAsync below is what makes any of it real.
            var auditPairs = new List<(Certificate Before, Certificate After)>();

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

                    // DEF-391: detached read of the stored row BEFORE the assignments below.
                    // existingCert is tracked, so it cannot be its own "before".
                    var beforeUpdate = await _context.Certificates
                        .AsNoTracking()
                        .FirstOrDefaultAsync(c => c.Id == certificate.Id);

                    existingCert.Number = certificate.Number;
                    existingCert.ProductId = certificate.ProductId;
                    existingCert.CompanyId = certificate.CompanyId;
                    existingCert.CertificationTypeId = certificate.CertificationTypeId;
                    existingCert.EffectiveDate = certificate.EffectiveDate;
                    existingCert.ExpirationDate = certificate.ExpirationDate;
                    existingCert.Status = certificate.Status;
                    existingCert.UpdatedAt = DateTime.UtcNow;

                    if (beforeUpdate != null)
                    {
                        auditPairs.Add((beforeUpdate, existingCert));
                    }

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

            // DEF-391: one audit row per changed field, per certificate, after the commit.
            foreach (var (before, after) in auditPairs)
            {
                await AuditBulkChangeAsync(AuditAction.Update, before, after);
            }

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

                // DEF-337 (completion): was a direct Remove — no audit, and every cascade child
                // destroyed silently, 100 certificates at a time. Deleting through the service
                // captures the snapshot and children per certificate.
                if (await _certificateService.DeleteCertificateAsync(
                        certId, userRole: "API", userId: 0, username: null))
                {
                    result.Successful.Add(cert.Number);
                }
                else
                {
                    result.Failed.Add(new BulkOperationFailure
                    {
                        Index = request.Ids.IndexOf(certId),
                        Identifier = certId.ToString(),
                        Errors = new List<ApiError>
                        {
                            new ApiError(ApiErrorCodes.InternalError,
                                $"Failed to delete certificate with ID {certId}")
                        }
                    });
                }
            }

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