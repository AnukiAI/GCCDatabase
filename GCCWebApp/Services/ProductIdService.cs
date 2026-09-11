using GCCWebApp.Data;
using GCCWebApp.Models;
using GCCWebApp.Services.Audit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using CsvHelper;                 // DEF-419: RFC 4180 parsing, incl. quoted multi-line fields
using CsvHelper.Configuration;
using System.Globalization;
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
        /// DEF-194 cross-page completion: bulk-delete product IDs in a single
        /// SQL round-trip. Two modes:
        ///   - ids != null: delete only the listed IDs (server-side
        ///     constrained to the cert so a forged payload can't delete
        ///     siblings on another cert).
        ///   - ids == null (deleteAll): delete every product ID on the cert
        ///     that matches the supplied filters. Filter semantics mirror
        ///     GetProductIdsByCertificatePagedAsync so what the user sees on
        ///     the table is exactly what gets deleted.
        /// Returns the number of rows deleted.
        /// </summary>
        Task<int> BulkDeleteProductIdsAsync(
            int certificateId,
            List<int>? ids,
            string? search = null,
            string? typeFilter = null,
            string? partnerFilter = null);

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
        private readonly GCCWebApp.Services.Audit.IAuditService _auditService;

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

        public ProductIdService(
            ApplicationDbContext context,
            ILogger<ProductIdService> logger,
            GCCWebApp.Services.Audit.IAuditService auditService)
        {
            _context = context;
            _logger = logger;
            _auditService = auditService;
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
                .Include(p => p.Partners) // CR-171: multi-partner round-trip
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
                .Include(p => p.Partners) // CR-171: multi-partner round-trip
                .AsSplitQuery()
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
                .Include(p => p.Partners) // CR-171: multi-partner round-trip
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        // CR-171: resolve a comma/pipe-delimited partner string into the Partners (M2M) collection
        // and normalize the denormalized primary to the first matched name. "None"/blank → no tags.
        private async Task SyncPartnersAsync(CertificateProductId pid)
        {
            var names = (pid.Partner ?? string.Empty)
                .Split(new[] { ',', '|' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(n => n.Trim())
                .Where(n => n.Length > 0 && !n.Equals("None", StringComparison.OrdinalIgnoreCase))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            pid.Partners.Clear();
            if (names.Any())
            {
                var matched = await _context.Partners
                    .Where(p => !p.IsDeleted && names.Contains(p.Name))
                    .ToListAsync();
                foreach (var p in matched) pid.Partners.Add(p);
                pid.Partner = matched.FirstOrDefault()?.Name ?? pid.Partner;
            }
        }

        public async Task<CertificateProductId> CreateProductIdAsync(CertificateProductId productId)
        {
            try
            {
                // DEF-161: canonicalise type + value so DB never stores e.g. lowercase ASIN.
                productId.ProductIdType = (productId.ProductIdType ?? string.Empty).ToUpperInvariant();
                productId.ProductIdValue = NormalizeValueForType(productId.ProductIdType, productId.ProductIdValue).Trim();

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
                await SyncPartnersAsync(productId); // CR-171: persist per-product partner tags
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
                // CR-171: Include Partners so the M2M can be re-synced from the posted Partner string.
                var existing = await _context.CertificateProductIds
                    .Include(p => p.Partners)
                    .FirstOrDefaultAsync(p => p.Id == productId.Id);
                if (existing == null)
                {
                    throw new ArgumentException("Product ID not found");
                }

                // DEF-161: canonicalise type + value before comparison and persist.
                productId.ProductIdType = (productId.ProductIdType ?? string.Empty).ToUpperInvariant();
                productId.ProductIdValue = NormalizeValueForType(productId.ProductIdType, productId.ProductIdValue).Trim();

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
                await SyncPartnersAsync(existing); // CR-171: re-sync per-product partner tags

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
                var productId = await _context.CertificateProductIds
                    .Include(p => p.Partners)
                    .FirstOrDefaultAsync(p => p.Id == id);
                if (productId == null)
                {
                    return false;
                }

                // DEF-337 (completion): product identifiers are what partners look certificates
                // up by (ASIN, GTIN, SKU), and this delete wrote no audit at all — a removed
                // identifier simply stopped resolving with no record of what it had been.
                // CR-171 Phase 2 also hangs per-identifier partner sharing off this row via a
                // cascading join table, so the visibility grants go with it.
                var children = AuditJoinRowCollector.Collect(
                    _context, typeof(CertificateProductId), nameof(CertificateProductId.Partners),
                    id.ToString(), productId.Partners, p => p.Id.ToString(), p => p.Name);

                await _auditService.LogEntityDeleteWithChildrenAsync(
                    id.ToString(), productId.ProductIdValue, productId, children,
                    userId: 0, username: string.Empty,
                    reason: $"Product identifier deleted from certificate {productId.CertificateId}",
                    changeSource: AuditChangeSource.WebUI);

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

        public async Task<int> BulkDeleteProductIdsAsync(
            int certificateId,
            List<int>? ids,
            string? search = null,
            string? typeFilter = null,
            string? partnerFilter = null)
        {
            try
            {
                IQueryable<CertificateProductId> q = _context.CertificateProductIds
                    .Where(p => p.CertificateId == certificateId);

                if (ids != null)
                {
                    if (ids.Count == 0) return 0;
                    q = q.Where(p => ids.Contains(p.Id));
                }
                else
                {
                    // deleteAll path — apply the same filter semantics as the
                    // paged GET so the rows deleted match the rows the user
                    // can see in the table when they clicked "Select all N".
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
                }

                // DEF-337 (completion): ExecuteDeleteAsync issues a set-based DELETE straight to
                // SQL — the rows never enter the change tracker, so nothing can be captured
                // after this line. Snapshot them first. This path can wipe every identifier on
                // a certificate in one call ("deleteAll" mode), which made it the largest
                // unaudited destructive operation left in the system.
                var doomed = await q.AsNoTracking().Include(p => p.Partners).ToListAsync();

                foreach (var row in doomed)
                {
                    await _auditService.LogEntityDeleteWithChildrenAsync(
                        row.Id.ToString(), row.ProductIdValue, row,
                        AuditJoinRowCollector.Collect(
                            _context, typeof(CertificateProductId), nameof(CertificateProductId.Partners),
                            row.Id.ToString(), row.Partners, p => p.Id.ToString(), p => p.Name),
                        userId: 0, username: string.Empty,
                        reason: $"Bulk delete of product identifiers on certificate {certificateId} " +
                                $"(mode: {(ids == null ? "deleteAll" : "ids")}, {doomed.Count} row(s))",
                        changeSource: AuditChangeSource.WebUI);
                }

                var deleted = await q.ExecuteDeleteAsync();

                _logger.LogInformation(
                    "Bulk-deleted {Count} product IDs for certificate {CertificateId} (mode: {Mode})",
                    deleted, certificateId, ids == null ? "deleteAll" : "ids");

                return deleted;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error bulk-deleting product IDs for certificate {CertificateId}",
                    certificateId);
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
            // DEF-161: normalise value before regex match so lowercase Amazon
            // ASINs (e.g. `b08n5wrwnw`) pass — same fix as the client side.
            value = NormalizeValueForType(type, value).Trim();

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

        /// <summary>
        /// DEF-161: Canonicalise a Product ID value for its type. ASIN, UPC, GTIN,
        /// EAN, ISBN, SKU, and MPN are uppercase by spec; OTHER is freeform and
        /// preserved as the user typed it.
        /// </summary>
        public static string NormalizeValueForType(string? type, string? value)
        {
            if (string.IsNullOrEmpty(value)) return value ?? string.Empty;
            var upperTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "ASIN", "UPC", "GTIN", "EAN", "ISBN", "SKU", "MPN"
            };
            return type != null && upperTypes.Contains(type) ? value.ToUpperInvariant() : value;
        }

        /// <summary>
        /// CR-171 partner resolution, shared by the insert path and the DEF-364 reactivation path
        /// so the two can never drift apart. Clears first: a reactivated row must not keep partners
        /// the new import did not ask for, because partner assignment decides who can see the ID.
        /// </summary>
        private static void ApplyPartners(CertificateProductId entity, string? partnerCell,
            IDictionary<string, Partner> partnerByName)
        {
            entity.Partners.Clear();
            foreach (var pname in (partnerCell ?? string.Empty)
                         .Split(new[] { ',', '|' }, StringSplitOptions.RemoveEmptyEntries)
                         .Select(n => n.Trim())
                         .Where(n => n.Length > 0 && !n.Equals("None", StringComparison.OrdinalIgnoreCase))
                         .Distinct(StringComparer.OrdinalIgnoreCase))
            {
                if (partnerByName.TryGetValue(pname, out var partnerEntity))
                    entity.Partners.Add(partnerEntity);
            }
            entity.Partner = entity.Partners.FirstOrDefault()?.Name ?? partnerCell;
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

            // CR-171: preload partners once so per-row partner resolution stays in memory (no N queries).
            var partnerByName = await _context.Partners
                .Where(p => !p.IsDeleted)
                .ToDictionaryAsync(p => p.Name, StringComparer.OrdinalIgnoreCase);

            // DEF-364: Partners are Included because a reactivated row must re-resolve its
            // partner set exactly like a fresh insert (CR-171) instead of keeping a stale one.
            var existingRows = await _context.CertificateProductIds
                .Include(p => p.Partners)
                .Where(p => p.CertificateId == certificateId)
                .ToListAsync();

            // CR-015 dup scope: per-cert. Key = upper(type) + ":" + trimmed value.
            // DEF-364: the map covers INACTIVE rows too. The unique index
            // IX_CertificateProductIds_CertificateId_ProductIdType_ProductIdValue has no
            // IsActive filter, so a soft-deactivated row still owns its slot in the index.
            // Keying on active rows only let a guaranteed-doomed INSERT through, and because
            // the commit below is a single batched SaveChanges, that one row took the whole
            // import down with it.
            var existingByKey = new Dictionary<string, CertificateProductId>(StringComparer.OrdinalIgnoreCase);
            foreach (var row in existingRows)
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
                    if (existingByKey.TryGetValue(key, out var existingRow))
                    {
                        // DEF-364: the row is still there, just deactivated — re-importing it
                        // means "put it back". Reactivate in place rather than queuing an INSERT
                        // the unique index is certain to reject. This also makes the importer
                        // agree with CheckDuplicateAsync, which never filtered on IsActive.
                        if (!existingRow.IsActive)
                        {
                            existingRow.IsActive = true;
                            existingRow.Status = derivedStatus;   // CR-015: status follows the cert
                            existingRow.ProductName = item.ProductName;
                            existingRow.ProductSku = item.ProductSku;
                            existingRow.Description = item.Description;
                            existingRow.UpdatedBy = userId;
                            existingRow.UpdatedAt = now;
                            ApplyPartners(existingRow, item.Partner, partnerByName);
                            result.SuccessCount++;
                            continue;
                        }

                        // Already active. Match prior contract: in overwrite mode, dup = silent success (kept).
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
                        // DEF-161: canonicalise on bulk import too — same rule as single create.
                        ProductIdValue = NormalizeValueForType(item.Type.ToUpper(), item.Value).Trim(),
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

                    // CR-171: resolve the (possibly delimited) Partner cell into the M2M collection.
                    ApplyPartners(entity, item.Partner, partnerByName);

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

            // DEF-419: this method used to read the file with ReadLineAsync() and split each
            // line on ',' with no quote handling at all — so a value containing a comma shifted
            // every later column, and a quoted field containing a hard line break (Excel
            // Alt+Enter) was torn into two bogus rows. The visible symptom was a row importing
            // with a truncated name and no Partner, plus a second row vanishing, both silently.
            // CsvHelper implements RFC 4180 properly, including multi-line quoted fields.
            using var reader = new StreamReader(csvStream, System.Text.Encoding.UTF8);
            using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = true,
                TrimOptions = TrimOptions.Trim,
                MissingFieldFound = null,   // short rows yield null fields rather than throwing
                HeaderValidated = null,
                BadDataFound = null
            });

            if (!await csv.ReadAsync() || !csv.ReadHeader())
            {
                throw new ArgumentException("CSV file is empty");
            }

            var headers = (csv.HeaderRecord ?? Array.Empty<string>())
                .Select(h => (h ?? string.Empty).Trim('"').Trim())
                .ToList();

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

            // DEF-419 (and DEF-342 parity): the client-side importer resolves the Partner column
            // by prefix because the shipped template heading reads "Partner (separate by comma)".
            // This path matched "Partner" exactly, so the same file imported through the API
            // dropped every partner tag silently. Resolve it the same way the browser does.
            var partnerHeader = headers.FirstOrDefault(h =>
                    h.Equals("Partner", StringComparison.OrdinalIgnoreCase) ||
                    h.StartsWith("Partner ", StringComparison.OrdinalIgnoreCase))
                ?? "Partner";

            var lineNumber = 1;
            while (await csv.ReadAsync())
            {
                lineNumber++;

                try
                {
                    var values = Enumerable.Range(0, headers.Count)
                        .Select(i => csv.TryGetField<string>(i, out var v) ? (v ?? string.Empty).Trim() : string.Empty)
                        .ToList();

                    // A record with nothing in it is padding, not data.
                    if (values.All(string.IsNullOrWhiteSpace)) continue;

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
                        Partner = GetValueByHeader(headers, values, partnerHeader),
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

            // Write data. Every field is escaped via CsvCell (previously the values were
            // interpolated raw — a quote or comma in ProductName/Description would corrupt the
            // row, and a value like the "+Vantage…" type would run as an Excel formula). The
            // Product ID value goes through CsvTextCell so identifier codes keep leading zeros
            // (a UPC "012345678905" would otherwise open as the number 12345678905). All use the
            // ="value" wrapper the import parser now strips (see product-id-manager.js
            // stripExcelTextFormat), so re-import stays lossless. See DEF-306.
            foreach (var productId in productIds)
            {
                var line = string.Join(",", new[]
                {
                    CsvCell(productId.ProductIdType),
                    CsvTextCell(productId.ProductIdValue),
                    CsvCell(productId.Status),
                    CsvCell(FormatPartnerList(productId.Partner)),
                    CsvCell(productId.ProductName),
                    CsvCell(productId.ProductSku),
                    CsvCell(productId.Description),
                    $"\"{productId.CreatedAt:yyyy-MM-dd}\"",
                    CsvCell(productId.CreatedByUser?.Username)
                });
                await writer.WriteLineAsync(line);
            }

            await writer.FlushAsync();
            return memoryStream.ToArray();
        }

        /// <summary>
        /// DEF-346: renders the stored partner list comma-delimited for export.
        ///
        /// <para><see cref="CertificateProductId.Partner"/> holds the denormalized list as the UI
        /// wrote it — pipe-delimited (<c>product-id-manager.js</c> saves
        /// <c>_partners.join('|')</c>, and the backend <c>SyncPartners</c> splits on both
        /// <c>'|'</c> and <c>','</c>). Exporting that raw meant a user was handed
        /// <c>Amazon|Novi</c> under a template heading that says
        /// <em>"Partner (separate by comma)"</em> — told one thing, shown another. That is the
        /// same "unclear when multiple" confusion behind QC #171, closed and reopened three
        /// times.</para>
        ///
        /// <para>Normalising on the way OUT rather than changing what is stored keeps this a
        /// display fix: no migration, no rewrite of live rows, and the save/import path is
        /// untouched. Round-trip stays lossless because the import parser splits on either
        /// delimiter, and <see cref="CsvCell"/> quotes the cell so an embedded comma cannot
        /// split the column.</para>
        /// </summary>
        private static string FormatPartnerList(string? stored)
        {
            if (string.IsNullOrWhiteSpace(stored)) return string.Empty;

            var names = stored
                .Split(new[] { '|', ',' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(n => n.Trim())
                .Where(n => n.Length > 0);

            return string.Join(", ", names);
        }

        // Characters that make Excel / Google Sheets treat a cell as a formula (CSV injection /
        // #NAME? on values like the "+Vantage Vinyl Verified" type). See DEF-306.
        private const string CsvFormulaTriggers = "=+-@\t\r";

        /// <summary>
        /// Builds a complete, Excel-safe CSV cell (surrounding quotes included). Normal values are
        /// RFC 4180 quoted; values that would otherwise be read as a formula (leading = + - @ or
        /// tab/CR) are emitted in Excel text format (="value"). Mirrors CertificateService.CsvCell.
        /// </summary>
        private static string CsvCell(string? field)
        {
            var value = field ?? string.Empty;
            var escaped = value.Replace("\"", "\"\"");
            if (value.Length > 0 && CsvFormulaTriggers.IndexOf(value[0]) >= 0)
            {
                return ExcelTextCell(value);
            }
            return $"\"{escaped}\"";
        }

        /// <summary>
        /// RFC 4180-valid CSV cell whose decoded content is Excel text format (="value").
        /// DEF-315 — see CertificateService.ExcelTextCell for the full rationale: the previous
        /// form left the '=' outside the quoting, producing an unquoted field that split on any
        /// embedded comma and shifted every following column. Encoding-only fix; the decoded
        /// value (and therefore the import round-trip) is unchanged.
        /// </summary>
        private static string ExcelTextCell(string value)
        {
            var excelEscaped = value.Replace("\"", "\"\"");
            var csvEscaped = excelEscaped.Replace("\"", "\"\"");
            return $"\"=\"\"{csvEscaped}\"\"\"";
        }

        /// <summary>
        /// Like <see cref="CsvCell"/> but ALWAYS emits Excel text format (="value") for non-empty
        /// values, so identifier codes (ASIN/UPC/GTIN/EAN) keep their leading zeros instead of being
        /// coerced to a number by Excel. Empty stays an empty quoted cell.
        /// </summary>
        private static string CsvTextCell(string? field)
        {
            var value = field ?? string.Empty;
            if (value.Length == 0)
            {
                return "\"\"";
            }
            // DEF-315: wrap only when Excel would actually corrupt the value (bare all-digit with a
            // leading zero, or >15 digits). Alphanumeric and multi-value cells are safe as plain
            // quoted fields and read cleanly in non-Excel tools.
            // See CertificateService.NeedsExcelTextProtection for the full rationale.
            return NeedsExcelTextProtection(value) ? ExcelTextCell(value) : $"\"{value.Replace("\"", "\"\"")}\"";
        }

        /// <summary>
        /// True when Excel would corrupt this value by parsing it as a number (bare all-digit value
        /// with a leading zero, or longer than 15 digits). Mirrors
        /// CertificateService.NeedsExcelTextProtection — keep the two in lock-step.
        /// </summary>
        private static bool NeedsExcelTextProtection(string value)
        {
            if (value.Length == 0) return false;
            foreach (var c in value)
            {
                if (c < '0' || c > '9') return false;
            }
            return value[0] == '0' || value.Length > 15;
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