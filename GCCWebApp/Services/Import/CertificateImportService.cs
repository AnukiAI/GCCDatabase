using CsvHelper;
using CsvHelper.Configuration;
using GCCWebApp.Data;
using GCCWebApp.Models;
using GCCWebApp.Models.Import;
using GCCWebApp.Services.Audit;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace GCCWebApp.Services.Import
{
    public class CertificateImportService : ICertificateImportService
    {
        private readonly ApplicationDbContext _context;
        private readonly IAuditService _auditService;
        private readonly IMemoryCache _cache;
        private readonly ILogger<CertificateImportService> _logger;
        private const string CACHE_KEY_PREFIX = "CertImport_";
        private const int CACHE_MINUTES = 30;

        public CertificateImportService(
            ApplicationDbContext context,
            IAuditService auditService,
            IMemoryCache cache,
            ILogger<CertificateImportService> logger)
        {
            _context = context;
            _auditService = auditService;
            _cache = cache;
            _logger = logger;
        }

        public async Task<CertificateImportPreviewResult> PreviewImportAsync(
            IFormFile csvFile,
            int userId,
            string username)
        {
            var result = new CertificateImportPreviewResult
            {
                FileName = csvFile.FileName,
                UploadedByUserId = userId,
                UploadedByUsername = username
            };

            try
            {
                // Parse CSV file
                var csvRows = await ParseCsvFileAsync(csvFile);
                result.TotalRows = csvRows.Count;

                // Load reference data once (more efficient than per-row lookups)
                var companies = await _context.Companies.ToListAsync();
                var products = await _context.Products.ToListAsync();
                var certTypes = await _context.CertificationTypes.ToListAsync();
                var existingCerts = await _context.Certificates
                    .Include(c => c.Company)
                    .Include(c => c.Product)
                    .Include(c => c.CertificationType)
                    .ToListAsync();

                // Process each row
                foreach (var csvRow in csvRows)
                {
                    var importRow = await ProcessRowAsync(
                        csvRow,
                        companies,
                        products,
                        certTypes,
                        existingCerts
                    );

                    // Categorize based on status
                    switch (importRow.RowStatus)
                    {
                        case ImportRowStatus.New:
                            result.NewRows.Add(importRow);
                            result.NewCertificates++;
                            result.ValidRows++;
                            break;

                        case ImportRowStatus.Modified:
                            result.ModifiedRows.Add(importRow);
                            result.ModifiedCertificates++;
                            result.ValidRows++;
                            break;

                        case ImportRowStatus.Unchanged:
                            result.UnchangedRows.Add(importRow);
                            result.UnchangedCertificates++;
                            result.ValidRows++;
                            break;

                        case ImportRowStatus.Error:
                            result.ErrorRows.Add(new CertificateImportError
                            {
                                RowNumber = importRow.RowNumber,
                                CertificateNumber = importRow.CertificateNumber,
                                ErrorType = "Validation",
                                ErrorMessages = importRow.ValidationMessages,
                                RawData = csvRow.RawData
                            });
                            result.InvalidRows++;
                            break;
                    }
                }

                // Check for critical errors
                result.HasCriticalErrors = result.TotalRows == result.InvalidRows;

                // Cache the result for later application
                _cache.Set(
                    CACHE_KEY_PREFIX + result.ImportId,
                    result,
                    TimeSpan.FromMinutes(CACHE_MINUTES)
                );

                _logger.LogInformation(
                    "Import preview generated: {ImportId}, {NewCount} new, {ModCount} modified, {ErrorCount} errors",
                    result.ImportId, result.NewCertificates, result.ModifiedCertificates, result.InvalidRows
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate import preview for file: {FileName}", csvFile.FileName);
                result.HasCriticalErrors = true;
                result.ErrorRows.Add(new CertificateImportError
                {
                    RowNumber = 0,
                    ErrorType = "System",
                    ErrorMessages = new List<string> { $"Failed to process file: {ex.Message}" }
                });
            }

            return result;
        }

        public async Task<ApplyImportResult> ApplyImportAsync(
            CertificateImportPreviewResult previewResult,
            int userId,
            string username,
            string? reason = null)
        {
            var result = new ApplyImportResult();

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var rowsToProcess = previewResult.NewRows.Concat(previewResult.ModifiedRows).ToList();

                foreach (var row in rowsToProcess)
                {
                    try
                    {
                        if (row.RowStatus == ImportRowStatus.New)
                        {
                            await CreateCertificateAsync(row, userId, username, reason);
                            result.CertificatesCreated++;
                        }
                        else if (row.RowStatus == ImportRowStatus.Modified)
                        {
                            await UpdateCertificateAsync(row, userId, username, reason);
                            result.CertificatesUpdated++;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to process row {RowNumber}: {CertNumber}",
                            row.RowNumber, row.CertificateNumber);
                        result.Errors.Add($"Row {row.RowNumber}: {ex.Message}");
                        result.CertificatesFailed++;
                    }
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                result.Success = result.CertificatesFailed == 0;
                result.Message = result.Success
                    ? $"Import completed successfully: {result.CertificatesCreated} created, {result.CertificatesUpdated} updated"
                    : $"Import completed with errors: {result.CertificatesFailed} failed";

                _logger.LogInformation(
                    "Import applied: {ImportId}, {Created} created, {Updated} updated, {Failed} failed",
                    previewResult.ImportId, result.CertificatesCreated, result.CertificatesUpdated, result.CertificatesFailed
                );

                // Remove from cache
                _cache.Remove(CACHE_KEY_PREFIX + previewResult.ImportId);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Failed to apply import: {ImportId}", previewResult.ImportId);
                result.Success = false;
                result.Message = $"Import failed: {ex.Message}";
                result.Errors.Add(ex.Message);
            }

            return result;
        }

        public async Task<CertificateImportPreviewResult?> GetCachedPreviewAsync(string importId)
        {
            return await Task.FromResult(_cache.Get<CertificateImportPreviewResult>(CACHE_KEY_PREFIX + importId));
        }

        #region Private Helper Methods

        private async Task<List<CertificateImportCsvRow>> ParseCsvFileAsync(IFormFile csvFile)
        {
            var rows = new List<CertificateImportCsvRow>();
            int rowNumber = 1; // Start at 1 (header is row 0)

            using var reader = new StreamReader(csvFile.OpenReadStream());
            using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HeaderValidated = null,
                MissingFieldFound = null,
                TrimOptions = TrimOptions.Trim
            });

            await csv.ReadAsync();
            csv.ReadHeader();

            while (await csv.ReadAsync())
            {
                rowNumber++;
                rows.Add(new CertificateImportCsvRow
                {
                    RowNumber = rowNumber,
                    CertificateNumber = csv.GetField("CertificateNumber") ?? "",
                    CompanyName = csv.GetField("CompanyName") ?? "",
                    ProductName = csv.GetField("ProductName") ?? "",
                    CertificationType = csv.GetField("CertificationType") ?? "",
                    Status = csv.GetField("Status") ?? "active",
                    EffectiveDate = csv.GetField("EffectiveDate") ?? "",
                    ExpirationDate = csv.GetField("ExpirationDate") ?? "",
                    Location = csv.GetField("Location") ?? "",
                    Country = csv.GetField("Country") ?? "",
                    Asin = csv.GetField("Asin") ?? "",
                    Upc = csv.GetField("Upc") ?? "",
                    Gtin = csv.GetField("Gtin") ?? "",
                    Ean = csv.GetField("Ean") ?? "",
                    Partner = csv.GetField("Partner") ?? "None",
                    RawData = csv.Context.Parser.RawRecord
                });
            }

            return rows;
        }

        private async Task<CertificateImportRow> ProcessRowAsync(
            CertificateImportCsvRow csvRow,
            List<Company> companies,
            List<Product> products,
            List<CertificationType> certTypes,
            List<Certificate> existingCerts)
        {
            var row = new CertificateImportRow
            {
                RowNumber = csvRow.RowNumber,
                CertificateNumber = csvRow.CertificateNumber,
                CompanyName = csvRow.CompanyName,
                ProductName = csvRow.ProductName,
                CertificationType = csvRow.CertificationType,
                Status = csvRow.Status,
                Location = csvRow.Location,
                Country = csvRow.Country,
                Asin = csvRow.Asin,
                Upc = csvRow.Upc,
                Gtin = csvRow.Gtin,
                Ean = csvRow.Ean,
                Partner = csvRow.Partner
            };

            // Validate required fields
            if (string.IsNullOrWhiteSpace(row.CertificateNumber))
                row.ValidationMessages.Add("Certificate Number is required");

            if (string.IsNullOrWhiteSpace(row.CompanyName))
                row.ValidationMessages.Add("Company Name is required");

            if (string.IsNullOrWhiteSpace(row.ProductName))
                row.ValidationMessages.Add("Product Name is required");

            if (string.IsNullOrWhiteSpace(row.CertificationType))
                row.ValidationMessages.Add("Certification Type is required");

            // Parse dates
            if (!string.IsNullOrWhiteSpace(csvRow.EffectiveDate))
            {
                if (DateTime.TryParse(csvRow.EffectiveDate, out var effectiveDate))
                    row.EffectiveDate = effectiveDate;
                else
                    row.ValidationMessages.Add($"Invalid Effective Date format: {csvRow.EffectiveDate}");
            }

            if (!string.IsNullOrWhiteSpace(csvRow.ExpirationDate))
            {
                if (DateTime.TryParse(csvRow.ExpirationDate, out var expirationDate))
                    row.ExpirationDate = expirationDate;
                else
                    row.ValidationMessages.Add($"Invalid Expiration Date format: {csvRow.ExpirationDate}");
            }

            // Lookup Company (Option B: must exist)
            var company = companies.FirstOrDefault(c =>
                c.Name.Equals(row.CompanyName, StringComparison.OrdinalIgnoreCase));
            if (company != null)
                row.CompanyId = company.Id;
            else
                row.ValidationMessages.Add($"Company not found: {row.CompanyName}");

            // Lookup Product (Option B: must exist)
            var product = products.FirstOrDefault(p =>
                p.Name.Equals(row.ProductName, StringComparison.OrdinalIgnoreCase));
            if (product != null)
                row.ProductId = product.Id;
            else
                row.ValidationMessages.Add($"Product not found: {row.ProductName}");

            // Lookup CertificationType (must exist)
            var certType = certTypes.FirstOrDefault(ct =>
                ct.Name.Equals(row.CertificationType, StringComparison.OrdinalIgnoreCase));
            if (certType != null)
                row.CertificationTypeId = certType.Id;
            else
                row.ValidationMessages.Add($"Certification Type not found: {row.CertificationType}");

            // If validation failed, mark as error
            if (row.ValidationMessages.Count > 0)
            {
                row.RowStatus = ImportRowStatus.Error;
                return row;
            }

            // Check if certificate exists
            var existing = existingCerts.FirstOrDefault(c =>
                c.Number.Equals(row.CertificateNumber, StringComparison.OrdinalIgnoreCase));

            if (existing == null)
            {
                // New certificate
                row.RowStatus = ImportRowStatus.New;
            }
            else
            {
                // Existing certificate - detect changes
                row.ExistingCertificateId = existing.Id;
                row.Changes = DetectChanges(existing, row);

                if (row.Changes.Count > 0)
                    row.RowStatus = ImportRowStatus.Modified;
                else
                    row.RowStatus = ImportRowStatus.Unchanged;
            }

            return await Task.FromResult(row);
        }

        private List<FieldChange> DetectChanges(Certificate existing, CertificateImportRow newData)
        {
            var changes = new List<FieldChange>();

            // Compare each field
            if (existing.CompanyId != newData.CompanyId)
                changes.Add(new FieldChange
                {
                    FieldName = "Company",
                    OldValue = existing.Company?.Name,
                    NewValue = newData.CompanyName
                });

            if (existing.ProductId != newData.ProductId)
                changes.Add(new FieldChange
                {
                    FieldName = "Product",
                    OldValue = existing.Product?.Name,
                    NewValue = newData.ProductName
                });

            if (existing.CertificationTypeId != newData.CertificationTypeId)
                changes.Add(new FieldChange
                {
                    FieldName = "CertificationType",
                    OldValue = existing.CertificationType?.Name,
                    NewValue = newData.CertificationType
                });

            if (existing.Status != newData.Status)
                changes.Add(new FieldChange
                {
                    FieldName = "Status",
                    OldValue = existing.Status,
                    NewValue = newData.Status
                });

            if (existing.EffectiveDate.Date != (newData.EffectiveDate?.Date ?? existing.EffectiveDate.Date))
                changes.Add(new FieldChange
                {
                    FieldName = "EffectiveDate",
                    OldValue = existing.EffectiveDate.ToString("yyyy-MM-dd"),
                    NewValue = newData.EffectiveDate?.ToString("yyyy-MM-dd")
                });

            if (existing.ExpirationDate.Date != (newData.ExpirationDate?.Date ?? existing.ExpirationDate.Date))
                changes.Add(new FieldChange
                {
                    FieldName = "ExpirationDate",
                    OldValue = existing.ExpirationDate.ToString("yyyy-MM-dd"),
                    NewValue = newData.ExpirationDate?.ToString("yyyy-MM-dd")
                });

            if ((existing.Location ?? "") != (newData.Location ?? ""))
                changes.Add(new FieldChange
                {
                    FieldName = "Location",
                    OldValue = existing.Location,
                    NewValue = newData.Location
                });

            if ((existing.Country ?? "") != (newData.Country ?? ""))
                changes.Add(new FieldChange
                {
                    FieldName = "Country",
                    OldValue = existing.Country,
                    NewValue = newData.Country
                });

            if ((existing.Asin ?? "") != (newData.Asin ?? ""))
                changes.Add(new FieldChange
                {
                    FieldName = "Asin",
                    OldValue = existing.Asin,
                    NewValue = newData.Asin
                });

            if ((existing.Upc ?? "") != (newData.Upc ?? ""))
                changes.Add(new FieldChange
                {
                    FieldName = "Upc",
                    OldValue = existing.Upc,
                    NewValue = newData.Upc
                });

            if ((existing.Gtin ?? "") != (newData.Gtin ?? ""))
                changes.Add(new FieldChange
                {
                    FieldName = "Gtin",
                    OldValue = existing.Gtin,
                    NewValue = newData.Gtin
                });

            if ((existing.Ean ?? "") != (newData.Ean ?? ""))
                changes.Add(new FieldChange
                {
                    FieldName = "Ean",
                    OldValue = existing.Ean,
                    NewValue = newData.Ean
                });

            if ((existing.Partner ?? "None") != (newData.Partner ?? "None"))
                changes.Add(new FieldChange
                {
                    FieldName = "Partner",
                    OldValue = existing.Partner,
                    NewValue = newData.Partner
                });

            return changes;
        }

        private async Task CreateCertificateAsync(
            CertificateImportRow row,
            int userId,
            string username,
            string? reason)
        {
            var certificate = new Certificate
            {
                Number = row.CertificateNumber,
                CompanyId = row.CompanyId!.Value,
                ProductId = row.ProductId!.Value,
                CertificationTypeId = row.CertificationTypeId!.Value,
                Status = row.Status,
                EffectiveDate = row.EffectiveDate ?? DateTime.UtcNow,
                ExpirationDate = row.ExpirationDate ?? DateTime.UtcNow.AddYears(1),
                Location = row.Location,
                Country = row.Country,
                Asin = row.Asin,
                Upc = row.Upc,
                Gtin = row.Gtin,
                Ean = row.Ean,
                Partner = row.Partner ?? "None",
                CreatedBy = userId,
                UpdatedBy = userId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Certificates.Add(certificate);
            await _context.SaveChangesAsync();

            // Log to audit
            await _auditService.LogEntityChangeAsync(
                certificate.Id.ToString(),
                certificate.Number,
                AuditAction.Create,
                null,
                certificate,
                userId,
                username,
                reason ?? $"Created via CSV import",
                AuditChangeSource.Import
            );
        }

        private async Task UpdateCertificateAsync(
            CertificateImportRow row,
            int userId,
            string username,
            string? reason)
        {
            var certificate = await _context.Certificates
                .Include(c => c.Company)
                .Include(c => c.Product)
                .Include(c => c.CertificationType)
                .FirstOrDefaultAsync(c => c.Id == row.ExistingCertificateId!.Value);

            if (certificate == null)
            {
                throw new InvalidOperationException($"Certificate not found: {row.CertificateNumber}");
            }

            // Capture old state for audit
            var oldCertificate = new Certificate
            {
                Id = certificate.Id,
                Number = certificate.Number,
                CompanyId = certificate.CompanyId,
                ProductId = certificate.ProductId,
                CertificationTypeId = certificate.CertificationTypeId,
                Status = certificate.Status,
                EffectiveDate = certificate.EffectiveDate,
                ExpirationDate = certificate.ExpirationDate,
                Location = certificate.Location,
                Country = certificate.Country,
                Asin = certificate.Asin,
                Upc = certificate.Upc,
                Gtin = certificate.Gtin,
                Ean = certificate.Ean,
                Partner = certificate.Partner
            };

            // Apply updates
            if (row.CompanyId.HasValue) certificate.CompanyId = row.CompanyId.Value;
            if (row.ProductId.HasValue) certificate.ProductId = row.ProductId.Value;
            if (row.CertificationTypeId.HasValue) certificate.CertificationTypeId = row.CertificationTypeId.Value;
            certificate.Status = row.Status;
            if (row.EffectiveDate.HasValue) certificate.EffectiveDate = row.EffectiveDate.Value;
            if (row.ExpirationDate.HasValue) certificate.ExpirationDate = row.ExpirationDate.Value;
            certificate.Location = row.Location;
            certificate.Country = row.Country;
            certificate.Asin = row.Asin;
            certificate.Upc = row.Upc;
            certificate.Gtin = row.Gtin;
            certificate.Ean = row.Ean;
            certificate.Partner = row.Partner;
            certificate.UpdatedBy = userId;
            certificate.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // Log to audit
            await _auditService.LogEntityChangeAsync(
                certificate.Id.ToString(),
                certificate.Number,
                AuditAction.Update,
                oldCertificate,
                certificate,
                userId,
                username,
                reason ?? $"Updated via CSV import",
                AuditChangeSource.Import
            );
        }

        #endregion
    }
}
