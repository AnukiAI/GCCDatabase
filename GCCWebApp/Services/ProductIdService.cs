using GCCWebApp.Data;
using GCCWebApp.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;
using System.Text.Json;

namespace GCCWebApp.Services
{
    /// <summary>
    /// CR-018: Paged result for the Product Identifier list. The API endpoint
    /// returns this shape when paging params are supplied.
    /// </summary>
    public class PagedProductIdsResult
    {
        public List<CertificateProductId> Items { get; set; } = new();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
    }

    public interface IProductIdService
    {
        Task<List<CertificateProductId>> GetProductIdsByCertificateAsync(int certificateId);

        /// <summary>
        /// CR-018: Paged variant — pushes sort, filter, and search down to SQL
        /// so we transmit one page (≤pageSize rows) instead of the full list.
        /// Used by the Edit page's Product Identifier widget.
        /// </summary>
        /// <param name="sortColumn">type | value | name | sku | partner | created | createdBy</param>
        /// <param name="sortDirection">asc | desc</param>
        Task<PagedProductIdsResult> GetProductIdsByCertificatePagedAsync(
            int certificateId,
            int page,
            int pageSize,
            string? sortColumn = null,
            string? sortDirection = null,
            string? search = null,
            string? typeFilter = null,
            string? partnerFilter = null);

        Task<CertificateProductId?> GetProductIdAsync(int id);
        Task<CertificateProductId> CreateProductIdAsync(CertificateProductId productId);
        Task<CertificateProductId> UpdateProductIdAsync(CertificateProductId productId);
        Task<bool> DeleteProductIdAsync(int id);
        /// <summary>
        /// Syncs all product IDs on a certificate to the auto-derived status from the cert's status (CR-015).
        /// Called whenever a cert's status changes (manual edit, bulk update, auto-expire).
        /// Returns the number of product ID rows that were updated.
        /// </summary>
        Task<int> SyncProductIdStatusesForCertificateAsync(int certificateId);
        Task<bool> ValidateProductIdAsync(string type, string value);
        Task<BulkImportResult> ImportProductIdsAsync(int certificateId, List<ProductIdImportItem> productIds, int? userId = null, bool overwriteExisting = false);
        Task<BulkImportResult> ImportFromCsvAsync(int certificateId, Stream csvStream, int? userId = null, bool overwriteExisting = false);
        Task<byte[]> ExportToCsvAsync(int certificateId);
        Task<byte[]> ExportToExcelAsync(int certificateId);
        Task<List<string>> GetValidationErrorsAsync(ProductIdImportItem item, int? certificateId = null);
        Task<bool> CheckDuplicateAsync(string type, string value, int? excludeId = null, int? certificateId = null);
        Dictionary<string, string> GetProductIdTypeDescriptions();
        string FormatProductId(string type, string value);
        Task<int> GetProductIdCountByCertificateAsync(int certificateId);
    }

    public class ProductIdService : IProductIdService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ProductIdService> _logger;

        // Product ID validation patterns
        private readonly Dictionary<string, string> _validationPatterns = new()
        {
            { "ASIN", @"^[A-Z0-9]{10}$" },
            { "UPC", @"^[0-9]{12}$" },
            { "GTIN", @"^[0-9]{8}$|^[0-9]{12}$|^[0-9]{13}$|^[0-9]{14}$" },
            { "EAN", @"^[0-9]{13}$" },
            { "ISBN", @"^(?:ISBN(?:-1[03])?:? )?(?=[0-9X]{10}$|(?=(?:[0-9]+[- ]){3})[- 0-9X]{13}$|97[89][0-9]{10}$|(?=(?:[0-9]+[- ]){4})[- 0-9]{17}$)(?:97[89][- ]?)?[0-9]{1,5}[- ]?[0-9]+[- ]?[0-9]+[- ]?[0-9X]$" },
            { "SKU", @"^[A-Z0-9\-_]{3,50}$" },
            { "MPN", @"^[A-Z0-9\-_.]{1,50}$" }
        };

        public ProductIdService(ApplicationDbContext context, ILogger<ProductIdService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<List<CertificateProductId>> GetProductIdsByCertificateAsync(int certificateId)
        {
            return await _context.CertificateProductIds
                .Where(p => p.CertificateId == certificateId && p.IsActive)
                .OrderBy(p => p.DisplayOrder ?? int.MaxValue)
                .ThenBy(p => p.ProductIdType)
                .ThenBy(p => p.CreatedAt)
                .Include(p => p.CreatedByUser)
                .Include(p => p.UpdatedByUser)
                .ToListAsync();
        }

        public async Task<PagedProductIdsResult> GetProductIdsByCertificatePagedAsync(
            int certificateId,
            int page,
            int pageSize,
            string? sortColumn = null,
            string? sortDirection = null,
            string? search = null,
            string? typeFilter = null,
            string? partnerFilter = null)
        {
            // CR-018: Push sort + filter + search into SQL. Load only one page
            // of rows (and Created/Updated user joins for that page) instead
            // of the full collection.

            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 50;
            if (pageSize > 500) pageSize = 500; // cap defensively

            IQueryable<CertificateProductId> q = _context.CertificateProductIds
                .Where(p => p.CertificateId == certificateId && p.IsActive);

            // Filters
            if (!string.IsNullOrWhiteSpace(typeFilter))
            {
                q = q.Where(p => p.ProductIdType == typeFilter);
            }
            if (!string.IsNullOrWhiteSpace(partnerFilter))
            {
                q = q.Where(p => p.Partner == partnerFilter);
            }
            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim();
                q = q.Where(p =>
                    EF.Functions.Like(p.ProductIdValue, $"%{s}%") ||
                    (p.ProductName != null && EF.Functions.Like(p.ProductName, $"%{s}%")) ||
                    (p.ProductSku != null && EF.Functions.Like(p.ProductSku, $"%{s}%")));
            }

            // Total count BEFORE paging (used for pagination math).
            var total = await q.CountAsync();

            // Sort. Default to the same order as the un-paged method so behavior
            // is consistent when the JS hasn't picked a column yet.
            var dirAsc = !string.Equals(sortDirection, "desc", StringComparison.OrdinalIgnoreCase);
            q = (sortColumn?.ToLowerInvariant()) switch
            {
                "type" => dirAsc ? q.OrderBy(p => p.ProductIdType) : q.OrderByDescending(p => p.ProductIdType),
                "value" => dirAsc ? q.OrderBy(p => p.ProductIdValue) : q.OrderByDescending(p => p.ProductIdValue),
                "name" => dirAsc ? q.OrderBy(p => p.ProductName) : q.OrderByDescending(p => p.ProductName),
                "sku" => dirAsc ? q.OrderBy(p => p.ProductSku) : q.OrderByDescending(p => p.ProductSku),
                "partner" => dirAsc ? q.OrderBy(p => p.Partner) : q.OrderByDescending(p => p.Partner),
                "created" => dirAsc ? q.OrderBy(p => p.CreatedAt) : q.OrderByDescending(p => p.CreatedAt),
                "createdby" => dirAsc
                    ? q.OrderBy(p => p.CreatedByUser != null ? p.CreatedByUser.Username : "")
                    : q.OrderByDescending(p => p.CreatedByUser != null ? p.CreatedByUser.Username : ""),
                _ => q.OrderBy(p => p.DisplayOrder ?? int.MaxValue)
                      .ThenBy(p => p.ProductIdType)
                      .ThenBy(p => p.CreatedAt),
            };

            // Stable secondary sort by Id so equal sort keys don't shuffle
            // between page fetches (would cause rows to skip / repeat across
            // pages otherwise).
            q = ((IOrderedQueryable<CertificateProductId>)q).ThenBy(p => p.Id);

            var items = await q
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Include(p => p.CreatedByUser)
                .Include(p => p.UpdatedByUser)
                .ToListAsync();

            var totalPages = total == 0 ? 0 : (int)Math.Ceiling(total / (double)pageSize);

            return new PagedProductIdsResult
            {
                Items = items,
                TotalCount = total,
                Page = page,
                PageSize = pageSize,
                TotalPages = totalPages,
            };
        }

        public async Task<CertificateProductId?> GetProductIdAsync(int id)
        {
            return await _context.CertificateProductIds
                .Include(p => p.Certificate)
                .Include(p => p.CreatedByUser)
                .Include(p => p.UpdatedByUser)
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task<CertificateProductId> CreateProductIdAsync(CertificateProductId productId)
        {
            try
            {
                // Validate the product ID
                if (!await ValidateProductIdAsync(productId.ProductIdType, productId.ProductIdValue))
                {
                    throw new ArgumentException($"Invalid {productId.ProductIdType} format: {productId.ProductIdValue}");
                }

                // Check for duplicates within the same certificate
                if (await CheckDuplicateAsync(productId.ProductIdType, productId.ProductIdValue, certificateId: productId.CertificateId))
                {
                    throw new InvalidOperationException($"Product ID {productId.ProductIdValue} already exists on this certificate");
                }

                // CR-015: Status is derived from parent cert, not user input.
                var parentCertStatus = await _context.Certificates
                    .Where(c => c.Id == productId.CertificateId)
                    .Select(c => c.Status)
                    .FirstOrDefaultAsync();
                productId.Status = CertificateProductId.DeriveStatusFromCert(parentCertStatus);

                // Set audit fields
                productId.CreatedAt = DateTime.UtcNow;
                productId.UpdatedAt = DateTime.UtcNow;

                // Set display order if not provided
                if (!productId.DisplayOrder.HasValue)
                {
                    var maxOrder = await _context.CertificateProductIds
                        .Where(p => p.CertificateId == productId.CertificateId)
                        .MaxAsync(p => (int?)p.DisplayOrder) ?? 0;
                    productId.DisplayOrder = maxOrder + 1;
                }

                _context.CertificateProductIds.Add(productId);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Created product ID {ProductIdType}:{ProductIdValue} for certificate {CertificateId}",
                    productId.ProductIdType, productId.ProductIdValue, productId.CertificateId);

                return productId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating product ID {ProductIdType}:{ProductIdValue} for certificate {CertificateId}",
                    productId.ProductIdType, productId.ProductIdValue, productId.CertificateId);
                throw;
            }
        }

        public async Task<CertificateProductId> UpdateProductIdAsync(CertificateProductId productId)
        {
            try
            {
                var existing = await _context.CertificateProductIds.FindAsync(productId.Id);
                if (existing == null)
                {
                    throw new ArgumentException("Product ID not found");
                }

                // Validate the product ID if it changed
                if (existing.ProductIdType != productId.ProductIdType || existing.ProductIdValue != productId.ProductIdValue)
                {
                    if (!await ValidateProductIdAsync(productId.ProductIdType, productId.ProductIdValue))
                    {
                        throw new ArgumentException($"Invalid {productId.ProductIdType} format: {productId.ProductIdValue}");
                    }

                    // Check for duplicates within the same certificate (excluding current record)
                    if (await CheckDuplicateAsync(productId.ProductIdType, productId.ProductIdValue, productId.Id, productId.CertificateId))
                    {
                        throw new InvalidOperationException($"Product ID {productId.ProductIdValue} already exists on this certificate");
                    }
                }

                // Update fields
                existing.ProductIdType = productId.ProductIdType;
                existing.ProductIdValue = productId.ProductIdValue;
                // CR-015: Status is derived from parent cert, not user input.
                var parentCertStatus = await _context.Certificates
                    .Where(c => c.Id == existing.CertificateId)
                    .Select(c => c.Status)
                    .FirstOrDefaultAsync();
                existing.Status = CertificateProductId.DeriveStatusFromCert(parentCertStatus);
                existing.Partner = productId.Partner;
                existing.ProductName = productId.ProductName;
                existing.ProductSku = productId.ProductSku;
                existing.Description = productId.Description;
                existing.IsActive = productId.IsActive;
                existing.IsPublic = productId.IsPublic;
                existing.DisplayOrder = productId.DisplayOrder;
                existing.UpdatedBy = productId.UpdatedBy;
                existing.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Updated product ID {Id} for certificate {CertificateId}",
                    productId.Id, productId.CertificateId);

                return existing;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating product ID {Id}", productId.Id);
                throw;
            }
        }

        public async Task<bool> DeleteProductIdAsync(int id)
        {
            try
            {
                var productId = await _context.CertificateProductIds.FindAsync(id);
                if (productId == null)
                {
                    return false;
                }

                _context.CertificateProductIds.Remove(productId);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Deleted product ID {Id}", id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting product ID {Id}", id);
                throw;
            }
        }

        public async Task<int> SyncProductIdStatusesForCertificateAsync(int certificateId)
        {
            // CR-015: Cascade cert status to all child product IDs.
            var certStatus = await _context.Certificates
                .Where(c => c.Id == certificateId)
                .Select(c => c.Status)
                .FirstOrDefaultAsync();

            var derivedStatus = CertificateProductId.DeriveStatusFromCert(certStatus);

            var rows = await _context.CertificateProductIds
                .Where(p => p.CertificateId == certificateId && p.Status != derivedStatus)
                .ToListAsync();

            foreach (var row in rows)
            {
                row.Status = derivedStatus;
                row.UpdatedAt = DateTime.UtcNow;
            }

            if (rows.Count > 0)
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation(
                    "CR-015 cascade: synced {Count} product IDs on cert {CertId} to status '{Status}'",
                    rows.Count, certificateId, derivedStatus);
            }

            return rows.Count;
        }

        public Task<bool> ValidateProductIdAsync(string type, string value)
        {
            if (string.IsNullOrWhiteSpace(type) || string.IsNullOrWhiteSpace(value))
            {
                return Task.FromResult(false);
            }

            type = type.ToUpper();

            // Special validation for specific types
            switch (type)
            {
                case "ASIN":
                    return Task.FromResult(ValidateAsin(value));
                case "UPC":
                    return Task.FromResult(ValidateUpc(value));
                case "GTIN":
                    return Task.FromResult(ValidateGtin(value));
                case "EAN":
                    return Task.FromResult(ValidateEan(value));
                case "ISBN":
                    return Task.FromResult(ValidateIsbn(value));
                case "SKU":
                case "MPN":
                case "OTHER":
                    return Task.FromResult(!string.IsNullOrWhiteSpace(value) && value.Length <= 50);
                default:
                    return Task.FromResult(!string.IsNullOrWhiteSpace(value) && value.Length <= 50);
            }
        }

        public async Task<BulkImportResult> ImportProductIdsAsync(int certificateId, List<ProductIdImportItem> productIds, int? userId = null, bool overwriteExisting = false)
        {
            // DEF-112: previously this method called CreateProductIdAsync per row,
            // which itself fired 3 DB queries (duplicate-check, cert-status, MAX(DisplayOrder))
            // plus a SaveChanges. For 1000 rows that was ~4000 roundtrips and routinely
            // timed out. Now: pre-fetch existing rows + cert status once, do duplicate
            // detection in memory, and commit all inserts in a single SaveChanges.
            var result = new BulkImportResult { TotalProcessed = productIds.Count };
            var now = DateTime.UtcNow;

            var parentCertStatus = await _context.Certificates
                .Where(c => c.Id == certificateId)
                .Select(c => c.Status)
                .FirstOrDefaultAsync();
            var derivedStatus = CertificateProductId.DeriveStatusFromCert(parentCertStatus);

            var existingRows = await _context.CertificateProductIds
                .Where(p => p.CertificateId == certificateId)
                .ToListAsync();

            // CR-015 dup scope: per-cert. Key = upper(type) + ":" + trimmed value.
            var existingByKey = new Dictionary<string, CertificateProductId>(StringComparer.OrdinalIgnoreCase);
            foreach (var row in existingRows.Where(r => r.IsActive))
            {
                var key = $"{(row.ProductIdType ?? string.Empty).ToUpper()}:{(row.ProductIdValue ?? string.Empty).Trim()}";
                existingByKey[key] = row;
            }

            var nextDisplayOrder = existingRows.Count > 0
                ? (existingRows.Max(p => p.DisplayOrder) ?? 0) + 1
                : 1;

            if (overwriteExisting)
            {
                var importKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var item in productIds)
                {
                    if (string.IsNullOrWhiteSpace(item.Type) || string.IsNullOrWhiteSpace(item.Value)) continue;
                    importKeys.Add($"{item.Type.ToUpper()}:{item.Value.Trim()}");
                }
                foreach (var existing in existingRows.Where(e => e.IsActive))
                {
                    var key = $"{(existing.ProductIdType ?? string.Empty).ToUpper()}:{(existing.ProductIdValue ?? string.Empty).Trim()}";
                    if (!importKeys.Contains(key))
                    {
                        existing.IsActive = false;
                        existing.UpdatedBy = userId;
                        existing.UpdatedAt = now;
                    }
                }
            }

            foreach (var item in productIds)
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(item.Type))
                    {
                        result.ErrorCount++;
                        result.Errors.Add($":{item.Value} - Type is required");
                        continue;
                    }
                    if (string.IsNullOrWhiteSpace(item.Value))
                    {
                        result.ErrorCount++;
                        result.Errors.Add($"{item.Type}: - Value is required");
                        continue;
                    }

                    if (!await ValidateProductIdAsync(item.Type, item.Value))
                    {
                        result.ErrorCount++;
                        result.Errors.Add($"{item.Type}:{item.Value} - Invalid {item.Type} format");
                        continue;
                    }

                    var key = $"{item.Type.ToUpper()}:{item.Value.Trim()}";
                    if (existingByKey.ContainsKey(key))
                    {
                        // Match prior contract: in overwrite mode, dup = silent success (kept).
                        if (overwriteExisting)
                        {
                            result.SuccessCount++;
                        }
                        else
                        {
                            result.ErrorCount++;
                            result.Errors.Add($"{item.Type}:{item.Value} - Product ID already exists on this certificate");
                        }
                        continue;
                    }

                    var entity = new CertificateProductId
                    {
                        CertificateId = certificateId,
                        ProductIdType = item.Type.ToUpper(),
                        ProductIdValue = item.Value.Trim(),
                        Status = derivedStatus,
                        Partner = item.Partner,
                        ProductName = item.ProductName,
                        ProductSku = item.ProductSku,
                        Description = item.Description,
                        CreatedBy = userId,
                        CreatedAt = now,
                        UpdatedAt = now,
                        IsActive = true,
                        IsPublic = true,
                        DisplayOrder = nextDisplayOrder++
                    };

                    _context.CertificateProductIds.Add(entity);
                    existingByKey[key] = entity; // prevent within-batch dup
                    result.SuccessCount++;
                }
                catch (Exception ex)
                {
                    result.ErrorCount++;
                    result.Errors.Add($"{item.Type}:{item.Value} - {ex.Message}");
                    _logger.LogError(ex, "Error queuing bulk import row {Type}:{Value}", item.Type, item.Value);
                }
            }

            try
            {
                if (_context.ChangeTracker.HasChanges())
                {
                    await _context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Bulk SaveChanges failed for certificate {CertificateId}", certificateId);
                // Pessimistic: if the batched save blew up, every queued insert is gone.
                // Move all "successes" into errors so the caller sees the truth.
                result.Errors.Add($"Database save failed: {ex.Message}");
                result.ErrorCount += result.SuccessCount;
                result.SuccessCount = 0;
            }

            _logger.LogInformation(
                "Bulk import for certificate {CertificateId}: {Success} succeeded, {Errors} failed",
                certificateId, result.SuccessCount, result.ErrorCount);

            return result;
        }

        public async Task<BulkImportResult> ImportFromCsvAsync(int certificateId, Stream csvStream, int? userId = null, bool overwriteExisting = false)
        {
            var productIds = new List<ProductIdImportItem>();

            using var reader = new StreamReader(csvStream, System.Text.Encoding.UTF8);
            var headerLine = await reader.ReadLineAsync();
            if (string.IsNullOrWhiteSpace(headerLine))
            {
                throw new ArgumentException("CSV file is empty");
            }

            var headers = headerLine.Split(',').Select(h => h.Trim('"').Trim()).ToList();

            // DEF-111: accept either "Value" (legacy) or "Product ID" (new template) for the value column.
            if (!headers.Contains("Type", StringComparer.OrdinalIgnoreCase))
            {
                throw new ArgumentException("Missing required header: Type");
            }
            var hasValueColumn = headers.Contains("Value", StringComparer.OrdinalIgnoreCase)
                || headers.Contains("Product ID", StringComparer.OrdinalIgnoreCase)
                || headers.Contains("ProductId", StringComparer.OrdinalIgnoreCase);
            if (!hasValueColumn)
            {
                throw new ArgumentException("Missing required header: Product ID (or Value)");
            }

            string? line;
            var lineNumber = 1;
            while ((line = await reader.ReadLineAsync()) != null)
            {
                lineNumber++;
                if (string.IsNullOrWhiteSpace(line)) continue;

                try
                {
                    var values = line.Split(',').Select(v => v.Trim('"').Trim()).ToList();

                    // DEF-111: header lookup is tolerant of both legacy ("ProductName"/"ProductSku")
                    // and new-template ("Product Name"/"SKU") column names so older templates on
                    // disk continue to import correctly. "Product ID" also accepted as a synonym
                    // for "Value" since the displayed column reads "Product ID" not "Value".
                    var item = new ProductIdImportItem
                    {
                        Type = GetValueByHeader(headers, values, "Type") ?? "",
                        Value = GetValueByHeader(headers, values, "Value")
                            ?? GetValueByHeader(headers, values, "Product ID")
                            ?? GetValueByHeader(headers, values, "ProductId")
                            ?? "",
                        // CR-015: Status header from CSV is ignored; status auto-derives from parent cert.
                        Partner = GetValueByHeader(headers, values, "Partner"),
                        ProductName = GetValueByHeader(headers, values, "ProductName")
                            ?? GetValueByHeader(headers, values, "Product Name"),
                        ProductSku = GetValueByHeader(headers, values, "ProductSku")
                            ?? GetValueByHeader(headers, values, "SKU"),
                        Description = GetValueByHeader(headers, values, "Description")
                    };

                    productIds.Add(item);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning("Error parsing CSV line {LineNumber}: {Error}", lineNumber, ex.Message);
                }
            }

            return await ImportProductIdsAsync(certificateId, productIds, userId, overwriteExisting);
        }

        public async Task<byte[]> ExportToCsvAsync(int certificateId)
        {
            var productIds = await GetProductIdsByCertificateAsync(certificateId);

            using var memoryStream = new MemoryStream();
            using var writer = new StreamWriter(memoryStream);

            // Write header
            await writer.WriteLineAsync("Type,Product ID,Status,Partner,ProductName,ProductSku,Description,CreatedAt,CreatedBy");

            // Write data
            foreach (var productId in productIds)
            {
                var line = $"\"{productId.ProductIdType}\",\"{productId.ProductIdValue}\",\"{productId.Status}\",\"{productId.Partner ?? ""}\",\"{productId.ProductName ?? ""}\",\"{productId.ProductSku ?? ""}\",\"{productId.Description ?? ""}\",\"{productId.CreatedAt:yyyy-MM-dd}\",\"{productId.CreatedByUser?.Username ?? ""}\"";
                await writer.WriteLineAsync(line);
            }

            await writer.FlushAsync();
            return memoryStream.ToArray();
        }

        public async Task<byte[]> ExportToExcelAsync(int certificateId)
        {
            // For now, return CSV format. Can be enhanced later with a proper Excel library
            return await ExportToCsvAsync(certificateId);
        }

        public async Task<List<string>> GetValidationErrorsAsync(ProductIdImportItem item, int? certificateId = null)
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(item.Type))
            {
                errors.Add("Type is required");
            }

            if (string.IsNullOrWhiteSpace(item.Value))
            {
                errors.Add("Value is required");
            }

            if (!string.IsNullOrWhiteSpace(item.Type) && !string.IsNullOrWhiteSpace(item.Value))
            {
                if (!await ValidateProductIdAsync(item.Type, item.Value))
                {
                    errors.Add($"Invalid {item.Type} format");
                }

                if (await CheckDuplicateAsync(item.Type, item.Value, certificateId: certificateId))
                {
                    errors.Add("Product ID already exists on this certificate");
                }
            }

            return errors;
        }

        public async Task<bool> CheckDuplicateAsync(string type, string value, int? excludeId = null, int? certificateId = null)
        {
            var query = _context.CertificateProductIds
                .Where(p => p.ProductIdType == type.ToUpper() && p.ProductIdValue == value.Trim());

            if (excludeId.HasValue)
            {
                query = query.Where(p => p.Id != excludeId.Value);
            }

            // Scope duplicate check to the same certificate
            if (certificateId.HasValue)
            {
                query = query.Where(p => p.CertificateId == certificateId.Value);
            }

            return await query.AnyAsync();
        }

        public Dictionary<string, string> GetProductIdTypeDescriptions()
        {
            return new Dictionary<string, string>
            {
                { "ASIN", "Amazon Standard Identification Number (10 characters)" },
                { "UPC", "Universal Product Code (12 digits)" },
                { "GTIN", "Global Trade Item Number (8, 12, 13, or 14 digits)" },
                { "EAN", "European Article Number (13 digits)" },
                { "ISBN", "International Standard Book Number" },
                { "SKU", "Stock Keeping Unit (3-50 alphanumeric)" },
                { "MPN", "Manufacturer Part Number (1-50 alphanumeric)" },
                { "OTHER", "Other identifier type (up to 50 characters)" }
            };
        }

        public string FormatProductId(string type, string value)
        {
            return $"{type.ToUpper()}: {value}";
        }

        public async Task<int> GetProductIdCountByCertificateAsync(int certificateId)
        {
            return await _context.CertificateProductIds
                .CountAsync(p => p.CertificateId == certificateId && p.IsActive);
        }

        #region Private Helper Methods

        private bool ValidateAsin(string asin)
        {
            return !string.IsNullOrWhiteSpace(asin) &&
                   asin.Length == 10 &&
                   Regex.IsMatch(asin, @"^[A-Z0-9]{10}$");
        }

        private bool ValidateUpc(string upc)
        {
            return !string.IsNullOrWhiteSpace(upc) &&
                   upc.Length == 12 &&
                   Regex.IsMatch(upc, @"^[0-9]{12}$") &&
                   ValidateUpcCheckDigit(upc);
        }

        private bool ValidateUpcCheckDigit(string upc)
        {
            if (upc.Length != 12) return false;

            var sum = 0;
            for (int i = 0; i < 11; i++)
            {
                var digit = int.Parse(upc[i].ToString());
                sum += (i % 2 == 0) ? digit * 3 : digit;
            }

            var checkDigit = (10 - (sum % 10)) % 10;
            return checkDigit == int.Parse(upc[11].ToString());
        }

        private bool ValidateGtin(string gtin)
        {
            return !string.IsNullOrWhiteSpace(gtin) &&
                   (gtin.Length == 8 || gtin.Length == 12 || gtin.Length == 13 || gtin.Length == 14) &&
                   Regex.IsMatch(gtin, @"^[0-9]+$");
        }

        private bool ValidateEan(string ean)
        {
            return !string.IsNullOrWhiteSpace(ean) &&
                   ean.Length == 13 &&
                   Regex.IsMatch(ean, @"^[0-9]{13}$") &&
                   ValidateEanCheckDigit(ean);
        }

        private bool ValidateEanCheckDigit(string ean)
        {
            if (ean.Length != 13) return false;

            var sum = 0;
            for (int i = 0; i < 12; i++)
            {
                var digit = int.Parse(ean[i].ToString());
                sum += (i % 2 == 0) ? digit : digit * 3;
            }

            var checkDigit = (10 - (sum % 10)) % 10;
            return checkDigit == int.Parse(ean[12].ToString());
        }

        private bool ValidateIsbn(string isbn)
        {
            // Enhanced ISBN validation with checksum verification
            if (string.IsNullOrWhiteSpace(isbn)) return false;

            var cleanIsbn = Regex.Replace(isbn.ToUpperInvariant(), @"[^0-9X]", "");

            if (cleanIsbn.Length == 10)
            {
                return ValidateIsbn10Checksum(cleanIsbn);
            }
            else if (cleanIsbn.Length == 13)
            {
                return ValidateIsbn13Checksum(cleanIsbn);
            }

            return false;
        }

        /// <summary>
        /// Validates ISBN-10 checksum using modulo 11 algorithm
        /// ISBN-10: Sum of (digit × position) mod 11 = 0
        /// </summary>
        private bool ValidateIsbn10Checksum(string isbn10)
        {
            if (isbn10.Length != 10) return false;

            int sum = 0;
            for (int i = 0; i < 10; i++)
            {
                int digit;
                if (isbn10[i] == 'X')
                {
                    // X is only valid in the last position and represents 10
                    if (i != 9) return false;
                    digit = 10;
                }
                else if (char.IsDigit(isbn10[i]))
                {
                    digit = isbn10[i] - '0';
                }
                else
                {
                    return false;
                }

                // Multiply by weight (10 - position)
                sum += digit * (10 - i);
            }

            return sum % 11 == 0;
        }

        /// <summary>
        /// Validates ISBN-13 checksum using EAN-13 algorithm
        /// ISBN-13: Alternating 1/3 weights, mod 10 = 0
        /// </summary>
        private bool ValidateIsbn13Checksum(string isbn13)
        {
            if (isbn13.Length != 13) return false;

            // ISBN-13 must start with 978 or 979
            if (!isbn13.StartsWith("978") && !isbn13.StartsWith("979")) return false;

            // X is not valid in ISBN-13
            if (isbn13.Contains('X')) return false;

            int sum = 0;
            for (int i = 0; i < 13; i++)
            {
                if (!char.IsDigit(isbn13[i])) return false;

                int digit = isbn13[i] - '0';
                // Alternating weights: 1 for even positions, 3 for odd positions
                sum += (i % 2 == 0) ? digit : digit * 3;
            }

            return sum % 10 == 0;
        }

        private string? GetValueByHeader(List<string> headers, List<string> values, string headerName)
        {
            var index = headers.FindIndex(h => h.Equals(headerName, StringComparison.OrdinalIgnoreCase));
            return index >= 0 && index < values.Count ? values[index] : null;
        }

        #endregion
    }
}