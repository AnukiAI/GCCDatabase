using CsvHelper;
using CsvHelper.Configuration;
using GCCWebApp.Data;
using GCCWebApp.Models;
using GCCWebApp.Models.Import;
using GCCWebApp.Repositories;
using GCCWebApp.Services;
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
using System.Text;
using System.Threading.Tasks;

namespace GCCWebApp.Services.Import
{
    public class CertificateImportService : ICertificateImportService
    {
        private readonly ApplicationDbContext _context;
        private readonly IAuditService _auditService;
        private readonly IMemoryCache _cache;
        private readonly ILogger<CertificateImportService> _logger;
        private readonly IWebHostEnvironment _environment;
        private readonly IFileUploadService _fileUploadService;
        private readonly IProductIdService _productIdService;
        private readonly IUploadPathResolver _uploadPaths;
        private readonly ICertificateService _certificateService;
        private readonly ICertificateRepository _certificateRepository;
        private const string CACHE_KEY_PREFIX = "CertImport_";
        private const int CACHE_MINUTES = 30;
        // Deactivated placeholder type for migrated certs whose certification type isn't in the system
        // (client directive 2026-06-24): map to this instead of rejecting the row. CertificationTypeId
        // is a required FK, so this placeholder is how a cert imports with an effectively "blank" type.
        private const string UnspecifiedCertTypeName = "Unspecified";

        public CertificateImportService(
            ApplicationDbContext context,
            IAuditService auditService,
            IMemoryCache cache,
            ILogger<CertificateImportService> logger,
            IWebHostEnvironment environment,
            IFileUploadService fileUploadService,
            IProductIdService productIdService,
            IUploadPathResolver uploadPaths,
            ICertificateService certificateService,
            // Injected purely to reuse GenerateNextCertificateNumberAsync for blank Certificate #
            // cells. Deliberately not reimplemented here: the rule carries the DEF-322 fix (only
            // sequences inside the standard 4-digit space count, so one out-of-band "26-10095"
            // cannot push the whole year's numbering out) and the year-rollover behaviour the client
            // asked us to prove (QC #218). A second copy would drift from both.
            ICertificateRepository certificateRepository)
        {
            _context = context;
            _auditService = auditService;
            _cache = cache;
            _logger = logger;
            _environment = environment;
            _fileUploadService = fileUploadService;
            _productIdService = productIdService;
            _uploadPaths = uploadPaths;
            _certificateService = certificateService;
            _certificateRepository = certificateRepository;
        }

        #region Column Name Mappings
        // Each array supports three header conventions so the same import service can ingest:
        //   1. Legacy SCREAMING_SNAKE templates (e.g. CERTIFICATE_NUMBER) — older client CSVs
        //   2. PascalCase property names (e.g. CertificateNumber) — programmatic exports
        //   3. Friendly human labels matching the cert tile table + export CSV (e.g. "Certificate #")
        //      — so /Certificate/ExportCertificates → edit in Excel → /CertificateImport round-trips.

        private static readonly string[] CertificateNumberColumns = { "CERTIFICATE_NUMBER", "CertificateNumber", "certificate_number", "Certificate #", "Cert #" };
        private static readonly string[] CompanyNameColumns = { "COMPANY_NAME", "CompanyName", "company_name", "Company" };
        private static readonly string[] CompanyLocationColumns = { "COMPANY_LOCATION", "CompanyLocation", "company_location" };
        private static readonly string[] CompanyCountryColumns = { "COMPANY_COUNTRY", "CompanyCountry", "company_country" };
        private static readonly string[] CompanyEmailColumns = { "COMPANY_EMAIL", "CompanyEmail", "company_email", "Company Email", "Company Contact Email/URL" };
        private static readonly string[] CompanyWebsiteColumns = { "COMPANY_WEBSITE", "CompanyWebsite", "company_website", "Company URL", "Company Website", "Contact URL" };
        private static readonly string[] ProductNameColumns = { "PRODUCT_NAME", "ProductName", "product_name", "Product Name", "Product" };
        private static readonly string[] ProductTypeColumns = { "PRODUCT_TYPE", "ProductType", "product_type", "Product Type", "Prod Type" };
        // QC #189 / DEF-278: Product Category column. Resolved against existing categories
        // (case-insensitive); no auto-create — an unmatched value rejects the row.
        private static readonly string[] ProductCategoryColumns = { "PRODUCT_CATEGORY", "ProductCategory", "product_category", "Product Category", "Category" };
        private static readonly string[] CertificationTypeColumns = { "CERTIFICATION_TYPE", "CertificationType", "certification_type", "Certification Type", "Cert Type" };
        // DEF-202 (QC #148): the downloaded template header reads
        // "Status (Draft / Client Approved - Not Paid / Client Approved - Confidential / Client Approved - Paid / Hold / Expired / Republished / Expired - No Annual Verification — defaults to Draft if blank)"
        // so the analyst sees allowed values inline. Parser tries exact short forms first;
        // if missing, GetFieldWithMapping falls back to a "starts with 'Status '" prefix
        // match so the longer hint header still resolves.
        private static readonly string[] StatusColumns = { "STATUS", "Status", "status" };
        private static readonly string[] EffectiveDateColumns = { "EFFECTIVE_DATE", "EffectiveDate", "effective_date", "Effective Date", "Eff Date" };
        private static readonly string[] ExpirationDateColumns = { "EXPIRATION_DATE", "ExpirationDate", "expiration_date", "Expiration Date", "Exp Date" };
        // DEF-347: bare uppercase "LOCATION"/"COUNTRY" were missing here while every other mapping
        // array carries the uppercase form (see StatusColumns). CsvHelper header matching is
        // case-SENSITIVE — no PrepareHeaderForMatch is configured (:1070) — so a file headed
        // "COUNTRY" silently imported a blank country and no error was raised. That is exactly the
        // silence this defect is about, and the migration's own source files use LOCATION/COUNTRY.
        private static readonly string[] CertificateLocationColumns = { "CERTIFICATE_LOCATION", "LOCATION", "Location", "certificate_location", "location" };
        private static readonly string[] CertificateCountryColumns = { "CERTIFICATE_COUNTRY", "COUNTRY", "Country", "certificate_country", "country" };

        // DEF-347: matches Certificate.Country's nvarchar(100) (Models/Certificate.cs:25).
        private const int CountryMaxLength = 100;

        private static string Truncate(string value, int max) =>
            value.Length <= max ? value : value[..max] + "…";
        private static readonly string[] PartnerColumns = { "PARTNER", "Partner", "partner" };
        private static readonly string[] CertificationResultsColumns = { "CERTIFICATION_RESULTS", "CertificationResults", "certification_results", "Certification Results", "Cert Results" };
        // Pathway (optional; SEP/Amazon CPF). The "Pathway (SEP Amazon only)" template header is
        // resolved by the GetFieldWithMapping prefix fallback ("Pathway " + ...).
        private static readonly string[] PathwayColumns = { "PATHWAY", "Pathway", "pathway" };
        // External Tag preferred (matches export column 19); generic TAGS kept for legacy clients.
        private static readonly string[] TagsColumns = { "External Tag", "ExternalTag", "EXTERNAL_TAG", "TAGS", "Tags", "tags" };
        // Internal Tag column — ingested as TagType="internal" (parallel to external tags).
        private static readonly string[] InternalTagsColumns = { "Internal Tag", "Internal Tags", "InternalTag", "INTERNAL_TAG" };
        private static readonly string[] AsinColumns = { "ASIN", "Asin", "asin" };
        private static readonly string[] UpcColumns = { "UPC", "Upc", "upc" };
        private static readonly string[] GtinColumns = { "GTIN", "Gtin", "gtin" };
        private static readonly string[] EanColumns = { "EAN", "Ean", "ean" };
        private static readonly string[] ProductIdTypeColumns = { "PRODUCT_ID_TYPE", "ProductIdType", "product_id_type", "type" };
        private static readonly string[] ProductIdValueColumns = { "PRODUCT_ID_VALUE", "ProductIdValue", "product_id_value", "value" };
        private static readonly string[] ProductSkuColumns = { "PRODUCT_SKU", "ProductSku", "product_sku", "productSku" };
        private static readonly string[] AmazonCodeColumns = { "AMAZON_CODE", "AmazonCode", "amazon_code", "amazonCode" };
        private static readonly string[] PdfFilePathColumns = { "PDF_FILE_PATH", "PdfFilePath", "pdf_file_path", "pdfDownloadUrl" };

        /// <summary>
        /// Gets a field value by trying multiple column name formats
        /// </summary>
        // === DEF-355 support: which columns did the FILE actually carry? ===
        // A blank value and an absent column are different intents, and the importer could not
        // tell them apart — so a file without (say) a Partner column read as "clear Partner".
        // These two helpers answer "was this column present in the header?", mirroring
        // GetFieldWithMapping's matching rules EXACTLY so presence and readability agree:
        // an exact, case-SENSITIVE match (CsvHelper's GetField, no PrepareHeaderForMatch — the
        // DEF-347 trap), plus the DEF-202 case-insensitive "Name " / "Name(" prefix fallback.
        private static bool HasColumn(string[]? header, string[] possibleNames)
        {
            if (header == null) return false;
            foreach (var name in possibleNames)
            {
                if (header.Any(h => h != null && h.Equals(name, StringComparison.Ordinal)))
                    return true;
                if (header.Any(h => h != null &&
                        (h.StartsWith(name + " ", StringComparison.OrdinalIgnoreCase) ||
                         h.StartsWith(name + "(", StringComparison.OrdinalIgnoreCase))))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// DEF-355: the set of logical fields whose column appeared in the uploaded file.
        /// Only these may be overwritten on an UPDATE; anything absent is left untouched.
        /// </summary>
        private static HashSet<string> BuildProvidedColumns(string[]? header)
        {
            var provided = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            void Check(string key, string[] names) { if (HasColumn(header, names)) provided.Add(key); }

            Check(nameof(Certificate.Status), StatusColumns);
            Check(nameof(Certificate.Location), CertificateLocationColumns);
            Check(nameof(Certificate.Country), CertificateCountryColumns);
            Check(nameof(Certificate.ProductCategoryId), ProductCategoryColumns);
            Check(nameof(Certificate.CertificationResults), CertificationResultsColumns);
            Check(nameof(Certificate.Partner), PartnerColumns);
            Check(nameof(Certificate.Asin), AsinColumns);
            Check(nameof(Certificate.Upc), UpcColumns);
            Check(nameof(Certificate.Gtin), GtinColumns);
            Check(nameof(Certificate.Ean), EanColumns);
            return provided;
        }

        private static string GetFieldWithMapping(CsvReader csv, string[] possibleNames)
        {
            foreach (var name in possibleNames)
            {
                try
                {
                    var value = csv.GetField(name);
                    if (value != null)
                        return StripExcelTextFormat(value.Trim());
                }
                catch
                {
                    // Column not found, try next
                }
            }

            // DEF-202 (QC #148): templates now ship enum-typed headers with inline value
            // hints, e.g. "Status (Draft / Client Approved - Paid / ...)". Exact match
            // above won't find those, so as a fallback walk the header row and accept any
            // header that starts with one of the short names followed by a space or '(' —
            // tight enough that "Status Update" or "Status Date" wouldn't false-positive.
            try
            {
                var header = csv.HeaderRecord;
                if (header != null)
                {
                    foreach (var name in possibleNames)
                    {
                        var match = header.FirstOrDefault(h =>
                            h != null &&
                            (h.StartsWith(name + " ", StringComparison.OrdinalIgnoreCase) ||
                             h.StartsWith(name + "(", StringComparison.OrdinalIgnoreCase)));
                        if (match != null)
                        {
                            var value = csv.GetField(match);
                            if (value != null)
                                return StripExcelTextFormat(value.Trim());
                        }
                    }
                }
            }
            catch
            {
                // Header introspection unavailable for this CsvReader state — fall through
            }
            return string.Empty;
        }

        /// <summary>
        /// Strips Excel text format (="value") used to preserve leading zeros
        /// Handles: ="12345" -> 12345, "12345" -> 12345, 12345 -> 12345
        /// </summary>
        private static string StripExcelTextFormat(string value)
        {
            if (string.IsNullOrEmpty(value))
                return value;

            // Handle ="value" format (Excel text format for preserving leading zeros)
            if (value.StartsWith("=\"") && value.EndsWith("\""))
                return value.Substring(2, value.Length - 3);

            // Handle ="value format (missing closing quote)
            if (value.StartsWith("=\""))
                return value.Substring(2).TrimEnd('"');

            return value;
        }

        /// <summary>
        /// Validates email format. QC #181 alignment: same strict regex as the ViewModel
        /// annotations (CertificateEditViewModel, CompanyViewModel, AdminModels,
        /// AccountViewModels) and the JS validators (admin-core, userManagement,
        /// companyManagement). Previous implementation used System.Net.Mail.MailAddress,
        /// which is permissive enough to accept "a@b" — and CSV imports were silently
        /// landing those values in Companies.Email rows.
        /// </summary>
        private static readonly System.Text.RegularExpressions.Regex EmailFormatRegex =
            new(@"^[a-zA-Z0-9._%+\-]+@[a-zA-Z0-9.\-]+\.[a-zA-Z]{2,}$",
                System.Text.RegularExpressions.RegexOptions.Compiled);

        private static bool IsValidEmail(string email) =>
            !string.IsNullOrWhiteSpace(email) && EmailFormatRegex.IsMatch(email);

        #endregion

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
                var companies = await _context.Companies.Where(c => !c.IsDeleted).ToListAsync();
                var products = await _context.Products.Where(p => !p.IsDeleted).ToListAsync();
                var certTypes = await _context.CertificationTypes.ToListAsync();

                // Ensure the deactivated "Unspecified" placeholder exists so certificates whose type
                // isn't in the system import (mapped to it) instead of being rejected (client directive
                // 2026-06-24). One-time idempotent system row; created deactivated so it's never an
                // option for new certificates.
                var unspecifiedType = certTypes.FirstOrDefault(ct =>
                    ct.Name.Equals(UnspecifiedCertTypeName, StringComparison.OrdinalIgnoreCase) && ct.ParentId == null);
                if (unspecifiedType == null)
                {
                    unspecifiedType = new CertificationType
                    {
                        Name = UnspecifiedCertTypeName,
                        IsActive = false,
                        Level = 1,
                        ParentId = null,
                        SortOrder = 9999,
                        Description = "Placeholder for migrated certificates whose original certification type is not in the system. Deactivated — not selectable for new certificates."
                    };
                    _context.CertificationTypes.Add(unspecifiedType);
                    await _context.SaveChangesAsync();
                    certTypes.Add(unspecifiedType);
                    _logger.LogInformation("Created deactivated '{Name}' certification-type placeholder (ID {Id}).",
                        UnspecifiedCertTypeName, unspecifiedType.Id);
                }

                var pathways = await _context.Pathways.Where(p => p.IsActive && !p.IsDeleted).ToListAsync();
                var productTypes = await _context.ProductTypes.Where(pt => pt.IsActive).ToListAsync();
                var tags = await _context.Tags.Where(t => t.IsActive).ToListAsync();
                var categories = await _context.ProductCategories.Where(c => c.IsActive).ToListAsync();
                // DEF-350: this preloads EVERY certificate with five Includes, one of which
                // (Tags) is a collection — so a single SQL statement carries the cartesian
                // product of certs x tags. Harmless at the 228 rows this was written against;
                // at the 5,405-row go-live volume it exceeds the 30s command timeout and the
                // preview fails with "Execution Timeout Expired", which reads as a flaky import.
                //
                // AsSplitQuery is the established remedy here (DEF-166 / DEF-234) — keep the
                // Includes, split the statement. See [[feedback_ef_includes_with_projection]].
                var existingCerts = await _context.Certificates
                    .Include(c => c.Company)
                    .Include(c => c.Product)
                    .Include(c => c.CertificationType)
                    .Include(c => c.Pathway)
                    .Include(c => c.Tags)
                    .AsSplitQuery()
                    // Preview never mutates these — DetectChanges only reads them, and
                    // UpdateCertificateAsync re-queries the certificate by Id before writing.
                    // Tracking 5,405 certificates plus their graphs is pure overhead here, and
                    // with AsSplitQuery alone the process died outright on the go-live volume.
                    .AsNoTracking()
                    .ToListAsync();

                // Get default category for auto-created products
                var defaultCategory = categories.FirstOrDefault();
                if (defaultCategory == null)
                {
                    result.HasCriticalErrors = true;
                    result.ErrorRows.Add(new CertificateImportError
                    {
                        RowNumber = 0,
                        ErrorType = "System",
                        ErrorMessages = new List<string> { "No product categories exist. Please create at least one category before importing." }
                    });
                    return result;
                }

                // Track entities to be created across all rows
                var companiesToCreate = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var productsToCreate = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var productTypesToCreate = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var tagsToCreate = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var internalTagsToCreate = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                // Group rows by certificate number to handle multi-ASIN rows: several CSV rows that
                // share one certificate number are ONE certificate carrying several product IDs.
                //
                // A blank certificate number carries no identity, so it must NOT group. Keyed on the
                // number alone, every auto-numbered row in a file collapsed into a single group and
                // only group.First() survived — two blank rows imported as ONE certificate.
                //
                // The key is a tuple rather than a sentinel string: AutoNumberIndex is the row's
                // position for a blank row and -1 for a numbered one, so blank rows can never share
                // a key with each other or with a real number, whatever the file contains. (A magic
                // string prefix would in principle be forgeable by a CSV cell; a tuple cannot be.)
                // Number is upper-cased to preserve the case-insensitive matching the previous
                // StringComparer.OrdinalIgnoreCase gave, which a tuple key does not carry.
                //
                // Consequence, and it is the right trade: a row that leaves Certificate # blank
                // cannot take part in multi-ASIN grouping, because there is no shared key to group
                // on. To attach several product IDs to one imported certificate, give it a number in
                // the file.
                var groupedRows = csvRows
                    .Where(r => !string.IsNullOrWhiteSpace(r.CertificateNumber) ||
                                !string.IsNullOrWhiteSpace(r.CompanyName) ||
                                !string.IsNullOrWhiteSpace(r.ProductName) ||
                                !string.IsNullOrWhiteSpace(r.CertificationType))
                    .Select((r, index) => (Row: r, Index: index))
                    .GroupBy(x => (
                        Number: string.IsNullOrWhiteSpace(x.Row.CertificateNumber)
                            ? string.Empty
                            : x.Row.CertificateNumber.ToUpperInvariant(),
                        AutoNumberIndex: string.IsNullOrWhiteSpace(x.Row.CertificateNumber) ? x.Index : -1))
                    .Select(g => g.Select(x => x.Row).ToList())
                    .ToList();

                foreach (var group in groupedRows)
                {
                    var primaryRow = group.First();

                    var importRow = await ProcessRowAsync(
                        primaryRow,
                        companies,
                        products,
                        certTypes,
                        pathways,
                        productTypes,
                        categories,
                        tags,
                        existingCerts,
                        companiesToCreate,
                        productsToCreate,
                        productTypesToCreate,
                        tagsToCreate,
                        internalTagsToCreate
                    );

                    // Collect product ID entries from ALL rows in the group
                    foreach (var csvRow in group)
                    {
                        // Check for explicit ProductIdType/ProductIdValue columns first
                        if (!string.IsNullOrWhiteSpace(csvRow.ProductIdType) &&
                            !string.IsNullOrWhiteSpace(csvRow.ProductIdValue))
                        {
                            importRow.ProductIdEntries.Add(new ProductIdEntry
                            {
                                RowNumber = csvRow.RowNumber,
                                ProductIdType = csvRow.ProductIdType.ToUpperInvariant(),
                                ProductIdValue = csvRow.ProductIdValue,
                                ProductName = !string.IsNullOrWhiteSpace(csvRow.ProductName) ? csvRow.ProductName : null,
                                ProductSku = !string.IsNullOrWhiteSpace(csvRow.ProductSku) ? csvRow.ProductSku : null,
                                AmazonCode = !string.IsNullOrWhiteSpace(csvRow.AmazonCode) ? csvRow.AmazonCode : null,
                                Partner = !string.IsNullOrWhiteSpace(csvRow.Partner) && csvRow.Partner != "None" ? csvRow.Partner : null
                            });
                        }
                        // Fall back to legacy ASIN/UPC/GTIN/EAN columns
                        else
                        {
                            if (!string.IsNullOrWhiteSpace(csvRow.Asin))
                            {
                                importRow.ProductIdEntries.Add(new ProductIdEntry
                                {
                                    RowNumber = csvRow.RowNumber,
                                    ProductIdType = "ASIN",
                                    ProductIdValue = csvRow.Asin,
                                    ProductName = !string.IsNullOrWhiteSpace(csvRow.ProductName) ? csvRow.ProductName : null,
                                    ProductSku = !string.IsNullOrWhiteSpace(csvRow.ProductSku) ? csvRow.ProductSku : null,
                                    AmazonCode = !string.IsNullOrWhiteSpace(csvRow.AmazonCode) ? csvRow.AmazonCode : null,
                                    Partner = !string.IsNullOrWhiteSpace(csvRow.Partner) && csvRow.Partner != "None" ? csvRow.Partner : null
                                });
                            }
                            if (!string.IsNullOrWhiteSpace(csvRow.Upc))
                            {
                                importRow.ProductIdEntries.Add(new ProductIdEntry
                                {
                                    RowNumber = csvRow.RowNumber,
                                    ProductIdType = "UPC",
                                    ProductIdValue = csvRow.Upc,
                                    ProductName = !string.IsNullOrWhiteSpace(csvRow.ProductName) ? csvRow.ProductName : null
                                });
                            }
                            if (!string.IsNullOrWhiteSpace(csvRow.Gtin))
                            {
                                importRow.ProductIdEntries.Add(new ProductIdEntry
                                {
                                    RowNumber = csvRow.RowNumber,
                                    ProductIdType = "GTIN",
                                    ProductIdValue = csvRow.Gtin,
                                    ProductName = !string.IsNullOrWhiteSpace(csvRow.ProductName) ? csvRow.ProductName : null
                                });
                            }
                            if (!string.IsNullOrWhiteSpace(csvRow.Ean))
                            {
                                importRow.ProductIdEntries.Add(new ProductIdEntry
                                {
                                    RowNumber = csvRow.RowNumber,
                                    ProductIdType = "EAN",
                                    ProductIdValue = csvRow.Ean,
                                    ProductName = !string.IsNullOrWhiteSpace(csvRow.ProductName) ? csvRow.ProductName : null
                                });
                            }
                        }
                    }

                    // Set PDF file path from the primary row (or first row that has one)
                    var pdfRow = group.FirstOrDefault(r => !string.IsNullOrWhiteSpace(r.PdfFilePath));
                    if (pdfRow != null)
                    {
                        importRow.PdfFilePath = pdfRow.PdfFilePath;
                    }

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
                                RawData = primaryRow.RawData
                            });
                            result.InvalidRows++;
                            break;
                    }
                }

                // Check for critical errors
                result.HasCriticalErrors = result.TotalRows == result.InvalidRows;

                // Tag names are globally unique: if a brand-new name appears in BOTH the external
                // and internal columns, create it once (external wins) and let internal attach reuse
                // it via the createdTags fallback below — avoids an IX_Tags_Name duplicate-key insert.
                internalTagsToCreate.ExceptWith(tagsToCreate);

                // Set entity creation summaries
                result.CompaniesToCreate = companiesToCreate.Count;
                result.ProductsToCreate = productsToCreate.Count;
                result.ProductTypesToCreate = productTypesToCreate.Count;
                result.TagsToCreate = tagsToCreate.Count;
                result.InternalTagsToCreate = internalTagsToCreate.Count;
                result.NewCompanyNames = companiesToCreate.ToList();
                result.NewProductNames = productsToCreate.ToList();
                result.NewProductTypeNames = productTypesToCreate.ToList();
                result.NewTagNames = tagsToCreate.ToList();
                result.NewInternalTagNames = internalTagsToCreate.ToList();

                // Multi-ASIN and PDF summaries
                result.ProductIdsToCreate = result.NewRows.Concat(result.ModifiedRows)
                    .Sum(r => r.ProductIdEntries.Count);
                result.PdfFilesToUpload = result.NewRows.Concat(result.ModifiedRows)
                    .Count(r => !string.IsNullOrWhiteSpace(r.PdfFilePath));

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

            // === Pre-import auto-backup (CR-023 R5) ===
            // Every Modified row overwrites a live certificate. A mis-matched file (e.g. a wrong
            // cert-number column) could overwrite many real certs at once. Before mutating anything,
            // snapshot the CURRENT state of every cert that would be overwritten to a re-importable
            // CSV. Fail-safe: if the snapshot can't be written, abort the whole import rather than
            // overwrite live data with no recovery path. (New-only imports skip this — nothing to back up.)
            try
            {
                var overwriteIds = previewResult.ModifiedRows
                    .Where(r => r.ExistingCertificateId.HasValue)
                    .Select(r => r.ExistingCertificateId!.Value.ToString())
                    .Distinct()
                    .ToList();

                if (overwriteIds.Any())
                {
                    // includeInternalTags: true → full round-trip fidelity (the restore CSV carries
                    // both External Tag and Internal Tag columns, matching the import template).
                    var csvBytes = await _certificateService.ExportCertificatesByIdsAsync(overwriteIds, includeInternalTags: true);
                    if (csvBytes == null || csvBytes.Length == 0)
                        throw new InvalidOperationException(
                            "Snapshot of the certificates that would be overwritten could not be generated.");

                    var backupsDir = _uploadPaths.ImportBackupsDir;
                    Directory.CreateDirectory(backupsDir);
                    var fileName = $"cert-import-backup_{DateTime.UtcNow:yyyyMMdd_HHmmss}_{previewResult.ImportId.Substring(0, 8)}.csv";
                    var backupPath = Path.Combine(backupsDir, fileName);
                    await File.WriteAllBytesAsync(backupPath, csvBytes);

                    result.BackupFilePath = backupPath;
                    result.BackupFileName = fileName;
                    result.BackupCertificateCount = overwriteIds.Count;

                    _logger.LogInformation(
                        "Pre-import backup written for {ImportId}: {Count} certificate(s) snapshotted to {Path} before overwrite.",
                        previewResult.ImportId, overwriteIds.Count, backupPath);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Pre-import backup failed for {ImportId}; aborting before any changes were made.", previewResult.ImportId);
                result.Success = false;
                result.Message = "Import aborted: the safety backup of the certificates that would be " +
                    $"overwritten could not be created ({ex.Message}). No changes were made.";
                result.Errors.Add(ex.Message);
                return result;
            }

            // === CR-023: one ImportBatch per apply, so the whole operation can later be undone ===
            // The batch id is stamped on every audit row written below (created + overwritten certs),
            // which is what UndoImportBatchAsync reads to reverse the import. The row is created inside
            // the transaction (Phase 0) so a rolled-back import leaves no batch behind, and finalized
            // with counts + COMPLETED status just before commit.
            var importBatch = new ImportBatch
            {
                EntityType = AuditEntityType.Certificate,
                FileName = previewResult.FileName,
                TotalRows = previewResult.TotalRows,
                ImportedBy = userId,
                ImportedByUsername = username,
                Status = ImportBatchStatus.Processing
            };
            var importBatchId = importBatch.Id;
            result.ImportBatchId = importBatchId;

            // Use execution strategy to handle SQL Server retries with transactions
            var strategy = _context.Database.CreateExecutionStrategy();

            await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync();

                try
                {
                // === Phase 0: persist the batch row (CR-023) ===
                _context.ImportBatches.Add(importBatch);
                await _context.SaveChangesAsync();

                // === Phase 1: Create all new entities first ===

                // 1a. Create new Companies
                var createdCompanies = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                foreach (var companyName in previewResult.NewCompanyNames)
                {
                    // Get company details from the first row that references this company
                    var rowWithCompanyDetails = previewResult.NewRows
                        .Concat(previewResult.ModifiedRows)
                        .FirstOrDefault(r => r.CompanyName.Equals(companyName, StringComparison.OrdinalIgnoreCase));

                    var company = new Company
                    {
                        Name = companyName,
                        Location = rowWithCompanyDetails?.CompanyLocation,
                        Country = rowWithCompanyDetails?.CompanyCountry,
                        Email = rowWithCompanyDetails?.CompanyEmail,
                        Website = rowWithCompanyDetails?.CompanyWebsite,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow,
                        IsActive = true,
                        IsDeleted = false
                    };
                    _context.Companies.Add(company);
                    await _context.SaveChangesAsync();
                    createdCompanies[companyName] = company.Id;

                    _logger.LogInformation("Auto-created company: {CompanyName} (ID: {CompanyId})", companyName, company.Id);

                    await _auditService.LogEntityChangeAsync(
                        company.Id.ToString(),
                        company.Name,
                        AuditAction.Create,
                        null,
                        company,
                        userId,
                        username,
                        "Auto-created via certificate import",
                        AuditChangeSource.Import
                    );
                }

                // 1b. Get default category for auto-created products
                var defaultCategory = await _context.ProductCategories.FirstOrDefaultAsync(c => c.IsActive);
                if (defaultCategory == null)
                {
                    throw new InvalidOperationException("No product categories exist. Please create at least one category before importing.");
                }

                // 1c. Create new ProductTypes
                var createdProductTypes = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                foreach (var productTypeName in previewResult.NewProductTypeNames)
                {
                    var productType = new ProductType
                    {
                        Name = productTypeName,
                        CategoryId = defaultCategory.Id,
                        Level = 0,
                        SortOrder = 0,
                        IsActive = true
                    };
                    _context.ProductTypes.Add(productType);
                    await _context.SaveChangesAsync();
                    createdProductTypes[productTypeName] = productType.Id;

                    _logger.LogInformation("Auto-created product type: {ProductTypeName} (ID: {ProductTypeId})", productTypeName, productType.Id);

                    await _auditService.LogEntityChangeAsync(
                        productType.Id.ToString(),
                        productType.Name,
                        AuditAction.Create,
                        null,
                        productType,
                        userId,
                        username,
                        "Auto-created via certificate import",
                        AuditChangeSource.Import
                    );
                }

                // 1d. Create new Tags (as "external" type)
                var createdTags = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                foreach (var tagName in previewResult.NewTagNames)
                {
                    var tag = new Tag
                    {
                        Name = tagName,
                        TagType = "external",
                        Description = $"Auto-created via certificate import on {DateTime.UtcNow:yyyy-MM-dd}",
                        IsActive = true
                    };
                    _context.Tags.Add(tag);
                    await _context.SaveChangesAsync();
                    createdTags[tagName] = tag.Id;

                    _logger.LogInformation("Auto-created tag: {TagName} (ID: {TagId})", tagName, tag.Id);
                }

                // 1d-internal. Create new Internal Tags (as "internal" type)
                var createdInternalTags = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                foreach (var tagName in previewResult.NewInternalTagNames)
                {
                    var tag = new Tag
                    {
                        Name = tagName,
                        TagType = "internal",
                        Description = $"Auto-created via certificate import on {DateTime.UtcNow:yyyy-MM-dd}",
                        IsActive = true
                    };
                    _context.Tags.Add(tag);
                    await _context.SaveChangesAsync();
                    createdInternalTags[tagName] = tag.Id;

                    _logger.LogInformation("Auto-created internal tag: {TagName} (ID: {TagId})", tagName, tag.Id);
                }

                // 1e. Create new Products
                var createdProducts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                foreach (var productName in previewResult.NewProductNames)
                {
                    // Get product details from the first row that references this product
                    var rowWithProductDetails = previewResult.NewRows
                        .Concat(previewResult.ModifiedRows)
                        .FirstOrDefault(r => r.ProductName.Equals(productName, StringComparison.OrdinalIgnoreCase));

                    // Determine CompanyId
                    int? companyId = null;
                    if (rowWithProductDetails?.CompanyId.HasValue == true)
                    {
                        companyId = rowWithProductDetails.CompanyId;
                    }
                    else if (rowWithProductDetails != null &&
                             createdCompanies.TryGetValue(rowWithProductDetails.CompanyName, out var newCompanyId))
                    {
                        companyId = newCompanyId;
                    }

                    // Determine ProductTypeId
                    int? productTypeId = null;
                    if (rowWithProductDetails?.ProductTypeId.HasValue == true)
                    {
                        productTypeId = rowWithProductDetails.ProductTypeId;
                    }
                    else if (!string.IsNullOrWhiteSpace(rowWithProductDetails?.ProductTypeName) &&
                             createdProductTypes.TryGetValue(rowWithProductDetails.ProductTypeName, out var newPtId))
                    {
                        productTypeId = newPtId;
                    }

                    // QC #189 / DEF-278: use the row's resolved Product Category for the new product
                    // (Product.CategoryId is required), falling back to the import default only when blank.
                    int categoryIdForNewProduct = rowWithProductDetails?.ProductCategoryId ?? defaultCategory.Id;

                    var product = new Product
                    {
                        Name = productName,
                        CategoryId = categoryIdForNewProduct,
                        ProductTypeId = productTypeId,
                        CompanyId = companyId,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow,
                        IsActive = true,
                        IsDeleted = false
                    };
                    _context.Products.Add(product);
                    await _context.SaveChangesAsync();
                    createdProducts[productName] = product.Id;

                    _logger.LogInformation("Auto-created product: {ProductName} (ID: {ProductId})", productName, product.Id);

                    await _auditService.LogEntityChangeAsync(
                        product.Id.ToString(),
                        product.Name,
                        AuditAction.Create,
                        null,
                        product,
                        userId,
                        username,
                        "Auto-created via certificate import",
                        AuditChangeSource.Import
                    );
                }

                // === Phase 2: Create/Update Certificates ===
                var rowsToProcess = previewResult.NewRows.Concat(previewResult.ModifiedRows).ToList();

                foreach (var row in rowsToProcess)
                {
                    try
                    {
                        // Resolve IDs from created entities if needed
                        if (!row.CompanyId.HasValue && createdCompanies.TryGetValue(row.CompanyName, out var cId))
                            row.CompanyId = cId;
                        // DEF-354: back-fill a product type created by THIS import onto the row.
                        // createdProductTypes was previously consulted only when building the
                        // Product record (:711), so a type auto-created here never reached the
                        // row — and therefore could never reach the certificate below.
                        if (!row.ProductTypeId.HasValue
                            && !string.IsNullOrWhiteSpace(row.ProductTypeName)
                            && createdProductTypes.TryGetValue(row.ProductTypeName, out var newRowPtId))
                            row.ProductTypeId = newRowPtId;
                        if (!row.ProductId.HasValue && createdProducts.TryGetValue(row.ProductName, out var pId))
                            row.ProductId = pId;

                        // CR-186 SPLIT: re-resolve to the product RECORD matching the full identity tuple
                        // (Company, Name, Category, Type, Subcategory) so same-name/different-type rows land
                        // on SEPARATE records — consistent with the cert form's find-or-create. The name-keyed
                        // step above can only ever return one record per name; this overrides it. QC #189 /
                        // DEF-278: the template's Product Category is now honored (row.ProductCategoryId) and
                        // passed through; Subcategory still resolves as null (no column yet) and Category falls
                        // back to the base product's only when the row left it blank.
                        if (row.ProductId.HasValue)
                            row.ProductId = await ResolveProductForImportAsync(row.ProductId.Value, row.ProductTypeId, null, row.ProductCategoryId);

                        // Resolve tag IDs (external + internal both land in the same Tags M2M)
                        var allTagIds = new List<int>(row.TagIds);
                        foreach (var tagName in row.TagsToCreate)
                        {
                            if (createdTags.TryGetValue(tagName, out var tId))
                                allTagIds.Add(tId);
                        }
                        allTagIds.AddRange(row.InternalTagIds);
                        foreach (var tagName in row.InternalTagsToCreate)
                        {
                            // Prefer the internally-created tag; fall back to an external creation
                            // when the same new name was reconciled to the external set (unique names).
                            if (createdInternalTags.TryGetValue(tagName, out var tId))
                                allTagIds.Add(tId);
                            else if (createdTags.TryGetValue(tagName, out var tId2))
                                allTagIds.Add(tId2);
                        }

                        if (row.RowStatus == ImportRowStatus.New)
                        {
                            await CreateCertificateAsync(row, allTagIds, userId, username, reason, importBatchId);
                            result.CertificatesCreated++;
                        }
                        else if (row.RowStatus == ImportRowStatus.Modified)
                        {
                            await UpdateCertificateAsync(row, allTagIds, userId, username, reason, importBatchId);
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

                // === Phase 3: Attach Product IDs and PDFs to unchanged (existing) certificates ===
                foreach (var row in previewResult.UnchangedRows)
                {
                    try
                    {
                        if (row.ExistingCertificateId.HasValue && row.ProductIdEntries.Any())
                        {
                            await CreateProductIdEntriesAsync(row.ExistingCertificateId.Value, row.ProductIdEntries, userId);
                        }

                        if (row.ExistingCertificateId.HasValue && !string.IsNullOrWhiteSpace(row.PdfFilePath))
                        {
                            var cert = await _context.Certificates.FindAsync(row.ExistingCertificateId.Value);
                            if (cert != null)
                            {
                                await UploadPdfFromPathAsync(cert, row.PdfFilePath);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to attach product IDs/PDF to existing certificate {CertNumber}",
                            row.CertificateNumber);
                    }
                }

                // === CR-023: finalize the batch row with outcome counts before commit ===
                importBatch.NewCount = result.CertificatesCreated;
                importBatch.ModifiedCount = result.CertificatesUpdated;
                importBatch.UnchangedCount = previewResult.UnchangedRows.Count;
                importBatch.ErrorCount = result.CertificatesFailed;
                importBatch.CompletedAt = DateTime.UtcNow;
                importBatch.Status = result.CertificatesFailed == 0
                    ? ImportBatchStatus.Completed
                    : ImportBatchStatus.PartialSuccess;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                result.Success = result.CertificatesFailed == 0;
                result.Message = result.Success
                    ? $"Import completed: {result.CertificatesCreated} certificates created, {result.CertificatesUpdated} updated"
                    : $"Import completed with errors: {result.CertificatesFailed} failed";

                _logger.LogInformation(
                    "Import applied: {ImportId}, {Created} created, {Updated} updated, {Failed} failed, {Companies} companies, {Products} products, {Tags} tags auto-created",
                    previewResult.ImportId, result.CertificatesCreated, result.CertificatesUpdated, result.CertificatesFailed,
                    createdCompanies.Count, createdProducts.Count, createdTags.Count
                );

                    // Remove from cache
                    _cache.Remove(CACHE_KEY_PREFIX + previewResult.ImportId);
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    var innerMessage = ex.InnerException?.Message ?? ex.Message;
                    _logger.LogError(ex, "Failed to apply import: {ImportId}. Inner: {InnerMessage}",
                        previewResult.ImportId, innerMessage);
                    result.Success = false;
                    result.Message = $"Import failed: {innerMessage}";
                    result.Errors.Add(innerMessage);
                    if (ex.InnerException?.InnerException != null)
                    {
                        result.Errors.Add(ex.InnerException.InnerException.Message);
                    }
                }
            }); // End of ExecuteAsync

            return result;
        }

        public async Task<CertificateImportPreviewResult?> GetCachedPreviewAsync(string importId)
        {
            return await Task.FromResult(_cache.Get<CertificateImportPreviewResult>(CACHE_KEY_PREFIX + importId));
        }

        // === CR-023: Import History + Undo ===========================================================

        public async Task<List<ImportBatch>> GetImportHistoryAsync(int limit = 100)
        {
            return await _context.ImportBatches
                .Where(b => b.EntityType == AuditEntityType.Certificate)
                .OrderByDescending(b => b.CreatedAt)
                .Take(limit > 0 ? limit : 100)
                .ToListAsync();
        }

        /// <summary>
        /// CR-023: roll back a certificate import. Restores every certificate the batch overwrote to its
        /// pre-import field values (from the per-field audit rows stamped with this batch id) and removes
        /// every certificate the batch created. Idempotent — a batch already UNDONE is refused.
        ///
        /// Limitation (documented): the per-field audit captures scalar certificate fields only, so a
        /// restore reverts Company/Status/dates/identifiers etc. but NOT tag/partner/product-id collection
        /// changes that an overwrite may have made. The CR-023 R5 pre-import backup CSV remains the
        /// full-fidelity manual fallback for those rare cases. Created certs are removed whole (children
        /// included), so their collections need no special handling.
        /// </summary>
        public async Task<UndoImportResult> UndoImportBatchAsync(Guid batchId, int userId, string username)
        {
            var result = new UndoImportResult();

            var batch = await _context.ImportBatches.FirstOrDefaultAsync(b => b.Id == batchId);
            if (batch == null)
            {
                result.Success = false;
                result.Message = "Import batch not found. It may be from before import history was tracked.";
                return result;
            }
            if (batch.Status == ImportBatchStatus.Undone)
            {
                result.Success = false;
                result.Message = "This import has already been undone.";
                return result;
            }
            if (batch.Status != ImportBatchStatus.Completed && batch.Status != ImportBatchStatus.PartialSuccess)
            {
                result.Success = false;
                result.Message = $"Only completed imports can be undone (this batch is '{batch.Status}').";
                return result;
            }

            // Pull this batch's certificate audit rows: UPDATE rows (per-field, to restore) and CREATE
            // rows (whole-cert, to delete). Both are stamped with the batch id by ApplyImportAsync.
            var auditRows = await _context.AuditHistory
                .Where(a => a.ImportBatchId == batchId && a.EntityType == AuditEntityType.Certificate)
                .ToListAsync();

            var updateRowsByCert = auditRows
                .Where(a => a.Action == AuditAction.Update)
                .GroupBy(a => a.EntityId)
                .ToList();

            var createdCertIds = auditRows
                .Where(a => a.Action == AuditAction.Create)
                .Select(a => a.EntityId)
                .Distinct()
                .ToList();

            var strategy = _context.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    var undoReason = $"Undo of import batch {batchId} ({batch.FileName ?? "import"})";

                    // --- Restore overwritten certificates to their pre-import field values ---
                    foreach (var group in updateRowsByCert)
                    {
                        if (!int.TryParse(group.Key, out var certId)) continue;
                        var cert = await _context.Certificates.FirstOrDefaultAsync(c => c.Id == certId);
                        if (cert == null)
                        {
                            result.Errors.Add($"Certificate {group.Key} no longer exists; skipped restore.");
                            continue;
                        }

                        foreach (var change in group)
                        {
                            if (!string.IsNullOrEmpty(change.FieldName))
                                ApplyOldValueToCertificate(cert, change.FieldName!, change.OldValue);
                        }
                        cert.UpdatedBy = userId;
                        cert.UpdatedAt = DateTime.UtcNow;
                        await _context.SaveChangesAsync();

                        // CR-015: re-derive product-ID statuses from the restored cert state.
                        await _productIdService.SyncProductIdStatusesForCertificateAsync(cert.Id);

                        await _auditService.LogEntityChangeAsync(
                            cert.Id.ToString(), cert.Number, AuditAction.Restore,
                            null, cert, userId, username, undoReason, AuditChangeSource.System);
                        result.CertificatesRestored++;
                    }

                    // --- Remove certificates the import created ---
                    foreach (var idStr in createdCertIds)
                    {
                        if (!int.TryParse(idStr, out var certId)) continue;
                        var cert = await _context.Certificates
                            .Include(c => c.Tags)
                            .Include(c => c.Partners)
                            .FirstOrDefaultAsync(c => c.Id == certId);
                        if (cert == null) continue; // already gone

                        // Remove dependent rows explicitly (FKs are mostly Restrict) before the cert.
                        var productIds = await _context.CertificateProductIds.Where(p => p.CertificateId == certId).ToListAsync();
                        if (productIds.Count > 0) _context.CertificateProductIds.RemoveRange(productIds);
                        var statusHistory = await _context.CertificateStatusHistory.Where(h => h.CertificateId == certId).ToListAsync();
                        if (statusHistory.Count > 0) _context.CertificateStatusHistory.RemoveRange(statusHistory);
                        var documents = await _context.CertificateDocuments.Where(d => d.CertificateId == certId).ToListAsync();
                        if (documents.Count > 0) _context.CertificateDocuments.RemoveRange(documents);
                        var customValues = await _context.CustomFieldValues.Where(v => v.CertificateId == certId).ToListAsync();
                        if (customValues.Count > 0) _context.CustomFieldValues.RemoveRange(customValues);
                        cert.Tags.Clear();
                        cert.Partners.Clear();

                        var snapshot = cert.Number;
                        _context.Certificates.Remove(cert);
                        await _context.SaveChangesAsync();

                        await _auditService.LogEntityChangeAsync(
                            certId.ToString(), snapshot, AuditAction.Delete,
                            cert, null, userId, username, undoReason, AuditChangeSource.System);
                        result.CertificatesDeleted++;
                    }

                    batch.Status = ImportBatchStatus.Undone;
                    batch.ErrorMessage = $"Undone by {username} on {DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC " +
                        $"({result.CertificatesRestored} restored, {result.CertificatesDeleted} removed).";
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    result.Success = true;
                    result.Message = $"Import undone: {result.CertificatesRestored} certificate(s) restored, " +
                        $"{result.CertificatesDeleted} created certificate(s) removed.";
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    var inner = ex.InnerException?.Message ?? ex.Message;
                    _logger.LogError(ex, "Failed to undo import batch {BatchId}. Inner: {Inner}", batchId, inner);
                    result.Success = false;
                    result.Message = $"Undo failed: {inner}";
                    result.Errors.Add(inner);
                }
            });

            return result;
        }

        // CR-023: restores a single scalar certificate field from its audit-captured OldValue.
        // FieldName is the CLR property name (AuditService.DetectChanges uses prop.Name) and OldValue
        // is the serialized prior value (strings verbatim, dates "yyyy-MM-dd HH:mm:ss", numbers ToString).
        // Only the fields the import path can mutate are handled; anything else is ignored.
        private static void ApplyOldValueToCertificate(Certificate cert, string fieldName, string? oldValue)
        {
            switch (fieldName)
            {
                case nameof(Certificate.CompanyId): cert.CompanyId = int.Parse(oldValue!, CultureInfo.InvariantCulture); break;
                case nameof(Certificate.ProductId): cert.ProductId = int.Parse(oldValue!, CultureInfo.InvariantCulture); break;
                case nameof(Certificate.CertificationTypeId): cert.CertificationTypeId = int.Parse(oldValue!, CultureInfo.InvariantCulture); break;
                case nameof(Certificate.ProductCategoryId): cert.ProductCategoryId = string.IsNullOrEmpty(oldValue) ? (int?)null : int.Parse(oldValue, CultureInfo.InvariantCulture); break;
                case nameof(Certificate.PathwayId): cert.PathwayId = string.IsNullOrEmpty(oldValue) ? (int?)null : int.Parse(oldValue, CultureInfo.InvariantCulture); break;
                case nameof(Certificate.Status): cert.Status = oldValue ?? "Draft"; break;
                case nameof(Certificate.EffectiveDate): cert.EffectiveDate = DateTime.Parse(oldValue!, CultureInfo.InvariantCulture); break;
                case nameof(Certificate.ExpirationDate): cert.ExpirationDate = DateTime.Parse(oldValue!, CultureInfo.InvariantCulture); break;
                case nameof(Certificate.Location): cert.Location = oldValue; break;
                case nameof(Certificate.Country): cert.Country = oldValue; break;
                case nameof(Certificate.Asin): cert.Asin = oldValue; break;
                case nameof(Certificate.Upc): cert.Upc = oldValue; break;
                case nameof(Certificate.Gtin): cert.Gtin = oldValue; break;
                case nameof(Certificate.Ean): cert.Ean = oldValue; break;
                case nameof(Certificate.Partner): cert.Partner = oldValue; break;
                case nameof(Certificate.CertificationResults): cert.CertificationResults = oldValue; break;
                // Unhandled fields (e.g. computed Normalized* or collection-derived) are intentionally skipped.
                default: break;
            }
        }

        #region Private Helper Methods

        private async Task<List<CertificateImportCsvRow>> ParseCsvFileAsync(IFormFile csvFile)
        {
            var rows = new List<CertificateImportCsvRow>();
            int rowNumber = 1; // Start at 1 (header is row 0)

            using var reader = new StreamReader(csvFile.OpenReadStream(), Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
            using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HeaderValidated = null,
                MissingFieldFound = null,
                BadDataFound = null, // Ignore "bad data" like Excel text format ="value"
                TrimOptions = TrimOptions.Trim
            });

            await csv.ReadAsync();
            csv.ReadHeader();

            // DEF-355: computed ONCE from the header, then stamped on every row so it survives
            // the preview cache and reaches UpdateCertificateAsync at apply time.
            var providedColumns = BuildProvidedColumns(csv.HeaderRecord);

            while (await csv.ReadAsync())
            {
                rowNumber++;
                var status = GetFieldWithMapping(csv, StatusColumns);
                var partner = GetFieldWithMapping(csv, PartnerColumns);

                rows.Add(new CertificateImportCsvRow
                {
                    RowNumber = rowNumber,
                    ProvidedColumns = providedColumns,
                    // Certificate fields
                    CertificateNumber = GetFieldWithMapping(csv, CertificateNumberColumns),
                    CertificationType = GetFieldWithMapping(csv, CertificationTypeColumns),
                    Status = string.IsNullOrWhiteSpace(status) ? CertificateStatusHelper.StatusDraft : status,
                    EffectiveDate = GetFieldWithMapping(csv, EffectiveDateColumns),
                    ExpirationDate = GetFieldWithMapping(csv, ExpirationDateColumns),
                    Location = GetFieldWithMapping(csv, CertificateLocationColumns),
                    Country = GetFieldWithMapping(csv, CertificateCountryColumns),
                    CertificationResults = GetFieldWithMapping(csv, CertificationResultsColumns),
                    Tags = GetFieldWithMapping(csv, TagsColumns),
                    InternalTags = GetFieldWithMapping(csv, InternalTagsColumns),
                    // Company fields
                    CompanyName = GetFieldWithMapping(csv, CompanyNameColumns),
                    CompanyLocation = GetFieldWithMapping(csv, CompanyLocationColumns),
                    CompanyCountry = GetFieldWithMapping(csv, CompanyCountryColumns),
                    CompanyEmail = GetFieldWithMapping(csv, CompanyEmailColumns),
                    CompanyWebsite = GetFieldWithMapping(csv, CompanyWebsiteColumns),
                    // Product fields
                    ProductName = GetFieldWithMapping(csv, ProductNameColumns),
                    ProductType = GetFieldWithMapping(csv, ProductTypeColumns),
                    ProductCategory = GetFieldWithMapping(csv, ProductCategoryColumns),
                    // Product identifiers
                    Asin = GetFieldWithMapping(csv, AsinColumns),
                    Upc = GetFieldWithMapping(csv, UpcColumns),
                    Gtin = GetFieldWithMapping(csv, GtinColumns),
                    Ean = GetFieldWithMapping(csv, EanColumns),
                    // Product ID fields (for multi-ASIN support)
                    ProductIdType = GetFieldWithMapping(csv, ProductIdTypeColumns),
                    ProductIdValue = GetFieldWithMapping(csv, ProductIdValueColumns),
                    ProductSku = GetFieldWithMapping(csv, ProductSkuColumns),
                    AmazonCode = GetFieldWithMapping(csv, AmazonCodeColumns),
                    // PDF file path
                    PdfFilePath = GetFieldWithMapping(csv, PdfFilePathColumns),
                    // Partner
                    Partner = string.IsNullOrWhiteSpace(partner) ? "None" : partner,
                    // Pathway (optional)
                    Pathway = GetFieldWithMapping(csv, PathwayColumns),
                    RawData = csv.Context.Parser.RawRecord
                });
            }

            return rows;
        }

        /// <summary>
        /// CR-186 SPLIT: resolve an imported certificate to the product RECORD matching its full
        /// identity (Company, Name, Category, ProductType, Subcategory), creating it if absent — the
        /// import equivalent of CertificateController.ResolveProductForCertAsync. Keeps same-name/
        /// different-type imported certs on separate product line items. Category falls back to the base
        /// product's; null Type/Subcategory matched in-memory (null == null is true) to dodge EF's
        /// column-to-nullable-parameter equality pitfalls.
        /// </summary>
        private async Task<int> ResolveProductForImportAsync(int basePickedProductId, int? typeId, int? subcatId, int? categoryId = null)
        {
            var baseProduct = await _context.Products.AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == basePickedProductId && !p.IsDeleted);
            if (baseProduct == null) return basePickedProductId;

            int categoryIdForProduct = categoryId ?? baseProduct.CategoryId;

            var candidates = await _context.Products
                .Where(p => !p.IsDeleted && p.Name == baseProduct.Name && p.CategoryId == categoryIdForProduct)
                .ToListAsync();
            var existing = candidates.FirstOrDefault(p =>
                p.CompanyId == baseProduct.CompanyId &&
                p.ProductTypeId == typeId &&
                p.ProductSubcategoryId == subcatId);
            if (existing != null) return existing.Id;

            var created = new Product
            {
                Name = baseProduct.Name,
                Description = baseProduct.Description,
                Model = baseProduct.Model,
                Sku = baseProduct.Sku,
                CompanyId = baseProduct.CompanyId,
                CategoryId = categoryIdForProduct,
                ProductTypeId = typeId,
                ProductSubcategoryId = subcatId,
                IsActive = baseProduct.IsActive,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _context.Products.Add(created);
            await _context.SaveChangesAsync();
            _logger.LogInformation(
                "CR-186 import: resolved certificate to product record #{Id} (Name={Name}, CategoryId={Cat}, ProductTypeId={Type}, ProductSubcategoryId={Sub})",
                created.Id, created.Name, categoryIdForProduct, typeId, subcatId);
            return created.Id;
        }

        private async Task<CertificateImportRow> ProcessRowAsync(
            CertificateImportCsvRow csvRow,
            List<Company> companies,
            List<Product> products,
            List<CertificationType> certTypes,
            List<Pathway> pathways,
            List<ProductType> productTypes,
            List<ProductCategory> categories,
            List<Tag> tags,
            List<Certificate> existingCerts,
            HashSet<string> companiesToCreate,
            HashSet<string> productsToCreate,
            HashSet<string> productTypesToCreate,
            HashSet<string> tagsToCreate,
            HashSet<string> internalTagsToCreate)
        {
            var row = new CertificateImportRow
            {
                RowNumber = csvRow.RowNumber,
                ProvidedColumns = csvRow.ProvidedColumns, // DEF-355: carry header presence to apply time
                CertificateNumber = csvRow.CertificateNumber,
                CompanyName = csvRow.CompanyName,
                ProductName = csvRow.ProductName,
                CertificationType = csvRow.CertificationType,
                Status = string.IsNullOrWhiteSpace(csvRow.Status) ? CertificateStatusHelper.StatusDraft : csvRow.Status,
                Location = csvRow.Location,
                Country = csvRow.Country,
                Asin = csvRow.Asin,
                Upc = csvRow.Upc,
                Gtin = csvRow.Gtin,
                Ean = csvRow.Ean,
                Partner = csvRow.Partner,
                // New fields
                CompanyLocation = csvRow.CompanyLocation,
                CompanyCountry = csvRow.CompanyCountry,
                CompanyEmail = csvRow.CompanyEmail,
                CompanyWebsite = csvRow.CompanyWebsite,
                ProductTypeName = csvRow.ProductType,
                ProductCategoryName = csvRow.ProductCategory,
                CertificationResults = csvRow.CertificationResults,
                Pathway = csvRow.Pathway
            };

            // Validate required fields
            //
            // Certificate # is deliberately NOT required. A blank one means "mint the next number",
            // exactly as /Certificate/Create does when the analyst leaves Number blank — auto
            // numbering is an original requirement, and the two write paths disagreeing about it
            // meant an analyst could create a certificate through the form but not through an
            // import of the same data. The template annotates Status with "defaults to Draft if
            // blank" and said nothing here, so the old rejection was also undiscoverable until the
            // import was refused.
            //
            // Supplying a number still works and still wins — which is what the initial data
            // migration does, since it carries the client's existing certificate numbers.
            if (string.IsNullOrWhiteSpace(row.CertificateNumber))
                row.WillAutoAssignNumber = true;

            if (string.IsNullOrWhiteSpace(row.CompanyName))
                row.ValidationMessages.Add("Company Name is required");

            if (string.IsNullOrWhiteSpace(row.ProductName))
                row.ValidationMessages.Add("Product Name is required");

            if (string.IsNullOrWhiteSpace(row.CertificationType))
                row.ValidationMessages.Add("Certification Type is required");

            // Validate CertificationResults length
            if (!string.IsNullOrWhiteSpace(row.CertificationResults) && row.CertificationResults.Length > 1000)
                row.ValidationMessages.Add("Certification Results exceeds maximum length of 1000 characters");

            // === Status validation / normalization (QC #194 / DEF-276) ===
            // Import previously stored the raw CSV status text, so a near-miss like
            // "Client Approved Paid" (missing the dash) persisted as invalid text — the cert
            // then showed a blank status and the Edit dropdown couldn't match it, forcing a
            // delete-and-redo. Resolve tolerantly (case/space/punctuation) to the canonical
            // 8-state value; if it matches no known status, reject the row in preview rather
            // than persisting garbage. (Blank was already defaulted to Draft above.)
            if (CertificateStatusHelper.TryResolveStatus(row.Status, out var canonicalStatus))
                row.Status = canonicalStatus;
            else
                row.ValidationMessages.Add(
                    $"Invalid Status: '{row.Status}'. Must be one of: {string.Join(", ", CertificateStatusHelper.GetAllStatuses().Keys)}.");

            // Validate company email format if provided
            if (!string.IsNullOrWhiteSpace(csvRow.CompanyEmail) && !IsValidEmail(csvRow.CompanyEmail))
                row.ValidationMessages.Add($"Invalid Company Email format: {csvRow.CompanyEmail}");

            // === Country validation / normalization (DEF-347) ===
            // Import wrote Country as a raw string with no check against CountryList, and the UI
            // <select> can't produce a bad value — so import was the only way in, and "Atlantis"
            // got in that way. Resolve tolerantly like DEF-276 does for Status, so migration
            // spellings ("USA", " united states ") normalize instead of being flagged.
            //
            // Deliberately a WARNING, not a ValidationMessage: an entry there marks the row Error
            // and drops the certificate from the import (:1445). For the go-live load, dropping a
            // certificate over a country typo is worse than loading it with the typo visible. The
            // value is kept as-supplied so the analyst can see what to correct.
            if (!string.IsNullOrWhiteSpace(row.Country))
            {
                // CR-037: the cell may now carry several countries, pipe- or comma-separated. The
                // single-value paths below are unchanged: one resolvable value yields a one-item
                // list whose first entry is the canonical spelling, and a cell where NOTHING
                // resolves still falls through to the tolerant raw-value branch.
                var resolvedCountries = CountryList.ParseMany(row.Country, out var unresolvedCountries);

                if (resolvedCountries.Count > 0)
                {
                    row.Countries = resolvedCountries;
                    row.Country = resolvedCountries[0]; // primary = first listed

                    if (unresolvedCountries.Count > 0)
                    {
                        // Partially resolved: take what we understood, name what we did not. Still a
                        // warning, not an error — dropping a certificate over one bad fragment is
                        // worse than loading it with the good countries attached.
                        row.Warnings.Add(
                            $"Country: kept {string.Join(", ", resolvedCountries)}; " +
                            $"could not match {string.Join(", ", unresolvedCountries.Select(u => $"'{Truncate(u, 40)}'"))} " +
                            "against the standard country list. Correct it on the certificate afterwards, " +
                            "or fix the source file and re-upload.");
                    }
                }
                else
                {
                    var supplied = row.Country;

                    // Certificate.Country is nvarchar(100). An unresolved value passes through raw,
                    // and a bad Location->Country split (plan section 87) can be far longer than a
                    // country name — SQL Server would then throw on SaveChanges and take the WHOLE
                    // batch down mid-load. Truncate and say so rather than fail 5,556 rows.
                    if (supplied.Length > CountryMaxLength)
                        row.Country = supplied[..CountryMaxLength];

                    row.Warnings.Add(
                        $"Unrecognised Country '{Truncate(supplied, 60)}' — it is not in the standard " +
                        "country list. The certificate will import with this value" +
                        (supplied.Length > CountryMaxLength
                            ? $", shortened to {CountryMaxLength} characters"
                            : " as-is") +
                        "; correct it on the certificate afterwards, or fix the source file and re-upload.");
                }
            }

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

            // === Company Lookup/Auto-Creation Logic ===
            var company = companies.FirstOrDefault(c =>
                c.Name.Equals(row.CompanyName, StringComparison.OrdinalIgnoreCase));
            if (company != null)
            {
                row.CompanyId = company.Id;
                row.CompanyWillBeCreated = false;
            }
            else
            {
                // Company will be auto-created
                row.CompanyWillBeCreated = true;
                companiesToCreate.Add(row.CompanyName);
            }

            // === ProductType Lookup/Auto-Creation Logic ===
            if (!string.IsNullOrWhiteSpace(csvRow.ProductType))
            {
                var productType = productTypes.FirstOrDefault(pt =>
                    pt.Name.Equals(csvRow.ProductType, StringComparison.OrdinalIgnoreCase));
                if (productType != null)
                {
                    row.ProductTypeId = productType.Id;
                    row.ProductTypeWillBeCreated = false;
                }
                else
                {
                    row.ProductTypeWillBeCreated = true;
                    productTypesToCreate.Add(csvRow.ProductType);
                }
            }

            // === Product Lookup/Auto-Creation Logic ===
            var product = products.FirstOrDefault(p =>
                p.Name.Equals(row.ProductName, StringComparison.OrdinalIgnoreCase));
            if (product != null)
            {
                row.ProductId = product.Id;
                row.ProductWillBeCreated = false;
            }
            else
            {
                // Product will be auto-created
                row.ProductWillBeCreated = true;
                productsToCreate.Add(row.ProductName);
            }

            // === CertificationType Lookup ===
            // Must exist; but if the value isn't in the system we DON'T reject the row — we map it to
            // the deactivated "Unspecified" placeholder (client directive 2026-06-24) so the certificate
            // still imports. CertificationTypeId is a required FK, so the placeholder represents "blank".
            var certType = certTypes.FirstOrDefault(ct =>
                ct.Name.Equals(row.CertificationType, StringComparison.OrdinalIgnoreCase));
            if (certType != null)
            {
                row.CertificationTypeId = certType.Id;
            }
            else
            {
                var placeholder = certTypes.FirstOrDefault(ct =>
                    ct.Name.Equals(UnspecifiedCertTypeName, StringComparison.OrdinalIgnoreCase) && ct.ParentId == null);
                if (placeholder != null)
                {
                    row.CertificationTypeId = placeholder.Id;
                    _logger.LogWarning(
                        "Import row {RowNumber} (cert {CertNumber}): Certification Type '{Type}' not found; mapped to the '{Placeholder}' placeholder.",
                        row.RowNumber, row.CertificateNumber, row.CertificationType, UnspecifiedCertTypeName);
                }
                else
                {
                    // Defensive: placeholder couldn't be resolved (shouldn't happen — ensured in preview).
                    row.ValidationMessages.Add($"Certification Type not found: '{row.CertificationType}'. Certification Types must exist before import.");
                }
            }

            // === Product Category Lookup (QC #189 / DEF-278; optional, must exist if provided) ===
            // Previously the template's Product Category was ignored entirely and every imported
            // product/cert fell back to the first category ("Chemical and Raw Materials"). Now the
            // value is honored: matched (case-insensitive) → drives the cert's ProductCategoryId
            // (CR-186) and the auto-created product's category; provided-but-unmatched → reject the
            // row (categories are a controlled hierarchy, so no auto-create — a typo shouldn't spawn
            // a new category); blank → left null (cert shows no category; product falls back to the
            // import default since Product.CategoryId is required).
            if (!string.IsNullOrWhiteSpace(csvRow.ProductCategory))
            {
                var category = categories.FirstOrDefault(c =>
                    c.Name.Equals(csvRow.ProductCategory, StringComparison.OrdinalIgnoreCase));
                if (category != null)
                    row.ProductCategoryId = category.Id;
                else
                    row.ValidationMessages.Add($"Product Category not found: '{csvRow.ProductCategory}'. Categories must exist before import.");
            }

            // === Pathway Lookup (optional; must exist if provided - no auto-create) ===
            // Unmatched values are non-blocking: log a warning and leave PathwayId null so an
            // unexpected pathway string never fails the whole certificate row.
            if (!string.IsNullOrWhiteSpace(csvRow.Pathway))
            {
                var pathway = pathways.FirstOrDefault(p =>
                    p.Name.Equals(row.Pathway, StringComparison.OrdinalIgnoreCase));
                if (pathway != null)
                    row.PathwayId = pathway.Id;
                else
                    _logger.LogWarning(
                        "Import row {RowNumber} (cert {CertNumber}): Pathway '{Pathway}' not found in the Pathways list; leaving unset.",
                        row.RowNumber, row.CertificateNumber, row.Pathway);
            }

            // === Tags Processing ===
            if (!string.IsNullOrWhiteSpace(csvRow.Tags))
            {
                var tagNames = csvRow.Tags.Split(',')
                    .Select(t => t.Trim())
                    .Where(t => !string.IsNullOrWhiteSpace(t))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                foreach (var tagName in tagNames)
                {
                    row.TagNames.Add(tagName);
                    var existingTag = tags.FirstOrDefault(t =>
                        t.Name.Equals(tagName, StringComparison.OrdinalIgnoreCase));

                    if (existingTag != null)
                    {
                        row.TagIds.Add(existingTag.Id);
                    }
                    else
                    {
                        row.TagsToCreate.Add(tagName);
                        tagsToCreate.Add(tagName);
                    }
                }
            }

            // === Internal Tags Processing (TagType="internal") ===
            // Mirrors external tags but matches/creates against internal-typed tags so a same-named
            // external tag is never reused for an internal value. Attached to the same Tags M2M.
            if (!string.IsNullOrWhiteSpace(csvRow.InternalTags))
            {
                var internalTagNames = csvRow.InternalTags.Split(',')
                    .Select(t => t.Trim())
                    .Where(t => !string.IsNullOrWhiteSpace(t))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                foreach (var tagName in internalTagNames)
                {
                    row.InternalTagNames.Add(tagName);
                    // Tag names are globally unique (IX_Tags_Name) — a name maps to exactly one
                    // tag regardless of type, so match by name across all types (same as external).
                    var existingTag = tags.FirstOrDefault(t =>
                        t.Name.Equals(tagName, StringComparison.OrdinalIgnoreCase));

                    if (existingTag != null)
                    {
                        row.InternalTagIds.Add(existingTag.Id);
                        if (!existingTag.TagType.Equals("internal", StringComparison.OrdinalIgnoreCase))
                            _logger.LogWarning(
                                "Import row {RowNumber} (cert {CertNumber}): internal tag '{Tag}' already exists as a '{Type}' tag; reusing the existing tag (tag names are globally unique).",
                                row.RowNumber, row.CertificateNumber, tagName, existingTag.TagType);
                    }
                    else
                    {
                        row.InternalTagsToCreate.Add(tagName);
                        internalTagsToCreate.Add(tagName);
                    }
                }
            }

            // If validation failed, mark as error
            if (row.ValidationMessages.Count > 0)
            {
                row.RowStatus = ImportRowStatus.Error;
                return row;
            }

            // Check if certificate exists. A row with no number cannot match one — it is new by
            // definition — and is short-circuited rather than left to rely on Equals(null) being
            // false, so several blank rows in one file stay independent New rows instead of
            // collapsing onto each other or onto a stored certificate.
            var existing = row.WillAutoAssignNumber
                ? null
                : existingCerts.FirstOrDefault(c =>
                    c.Number.Equals(row.CertificateNumber, StringComparison.OrdinalIgnoreCase));

            if (existing == null)
            {
                // === DEF-353: a NEW certificate may not be created without dates ===
                // Certificate.EffectiveDate/ExpirationDate are NOT NULL, so a blank cell used to
                // be replaced with DateTime.UtcNow / +1 year in CreateCertificateAsync. That is
                // fabrication, not a default: on the go-live rehearsal it gave 15 certificates
                // invented dates, including an Expired 2013 cert with a 2027 expiry and two certs
                // whose effective date landed AFTER their expiration date. An unparseable date is
                // already rejected above (:1306/:1314); a blank one now gets the same treatment,
                // consistent with the other "is required" checks.
                //
                // Scoped to NEW rows on purpose. On an UPDATE a blank date legitimately means
                // "leave the stored value alone" (UpdateCertificateAsync only assigns when
                // HasValue), so partial-update files keep working exactly as before.
                var missingDates = new List<string>();
                if (!row.EffectiveDate.HasValue) missingDates.Add("Effective Date");
                if (!row.ExpirationDate.HasValue) missingDates.Add("Expiration Date");

                if (missingDates.Count > 0)
                {
                    row.ValidationMessages.Add(
                        $"{string.Join(" and ", missingDates)} " +
                        $"{(missingDates.Count == 1 ? "is" : "are")} required for a new certificate. " +
                        "Blank dates are no longer filled in automatically, because the substituted " +
                        "value is indistinguishable from a real one.");
                    row.RowStatus = ImportRowStatus.Error;
                    return row;
                }

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

            // DEF-355: a column the file never carried is not a change. Without this the preview
            // would advertise edits ("Status: Client Approved - Paid -> Draft") that
            // UpdateCertificateAsync now correctly refuses to apply — preview and apply must agree.
            // Pathway already worked this way; the rest now match it.
            bool Provided(string field) => newData.ProvidedColumns.Contains(field);

            // Compare each field
            if (existing.CompanyId != newData.CompanyId)
                changes.Add(new FieldChange
                {
                    FieldName = FieldChange.CompanyFieldName,
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

            // Pathway: only flag a change when the CSV supplied a pathway (matches the
            // conditional apply in UpdateCertificateAsync — a blank column is a no-op, not a clear).
            if (!string.IsNullOrWhiteSpace(newData.Pathway) && existing.PathwayId != newData.PathwayId)
                changes.Add(new FieldChange
                {
                    FieldName = "Pathway",
                    OldValue = existing.Pathway?.Name,
                    NewValue = newData.Pathway
                });

            if (Provided(nameof(Certificate.Status)) && existing.Status != newData.Status)
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

            if (Provided(nameof(Certificate.Location)) && (existing.Location ?? "") != (newData.Location ?? ""))
                changes.Add(new FieldChange
                {
                    FieldName = "Location",
                    OldValue = existing.Location,
                    NewValue = newData.Location
                });

            if (Provided(nameof(Certificate.Country)) && (existing.Country ?? "") != (newData.Country ?? ""))
                changes.Add(new FieldChange
                {
                    FieldName = "Country",
                    OldValue = existing.Country,
                    NewValue = newData.Country
                });

            if (Provided(nameof(Certificate.Asin)) && (existing.Asin ?? "") != (newData.Asin ?? ""))
                changes.Add(new FieldChange
                {
                    FieldName = "Asin",
                    OldValue = existing.Asin,
                    NewValue = newData.Asin
                });

            if (Provided(nameof(Certificate.Upc)) && (existing.Upc ?? "") != (newData.Upc ?? ""))
                changes.Add(new FieldChange
                {
                    FieldName = "Upc",
                    OldValue = existing.Upc,
                    NewValue = newData.Upc
                });

            if (Provided(nameof(Certificate.Gtin)) && (existing.Gtin ?? "") != (newData.Gtin ?? ""))
                changes.Add(new FieldChange
                {
                    FieldName = "Gtin",
                    OldValue = existing.Gtin,
                    NewValue = newData.Gtin
                });

            if (Provided(nameof(Certificate.Ean)) && (existing.Ean ?? "") != (newData.Ean ?? ""))
                changes.Add(new FieldChange
                {
                    FieldName = "Ean",
                    OldValue = existing.Ean,
                    NewValue = newData.Ean
                });

            if (Provided(nameof(Certificate.Partner)) && (existing.Partner ?? "None") != (newData.Partner ?? "None"))
                changes.Add(new FieldChange
                {
                    FieldName = "Partner",
                    OldValue = existing.Partner,
                    NewValue = newData.Partner
                });

            // CertificationResults change detection
            if (Provided(nameof(Certificate.CertificationResults)) && (existing.CertificationResults ?? "") != (newData.CertificationResults ?? ""))
                changes.Add(new FieldChange
                {
                    FieldName = "CertificationResults",
                    OldValue = existing.CertificationResults,
                    NewValue = newData.CertificationResults
                });

            // Tags change detection
            var existingTagNames = existing.Tags?.Select(t => t.Name).OrderBy(n => n).ToList() ?? new List<string>();
            var newTagNames = newData.TagNames.OrderBy(n => n).ToList();

            if (!existingTagNames.SequenceEqual(newTagNames, StringComparer.OrdinalIgnoreCase))
            {
                changes.Add(new FieldChange
                {
                    FieldName = "Tags",
                    OldValue = string.Join(", ", existingTagNames),
                    NewValue = string.Join(", ", newTagNames)
                });
            }

            return changes;
        }

        private async Task CreateCertificateAsync(
            CertificateImportRow row,
            List<int> tagIds,
            int userId,
            string username,
            string? reason,
            Guid? importBatchId = null)
        {
            // Blank Certificate # in the file means "mint the next one" (see WillAutoAssignNumber).
            // Done here, at apply time, rather than in preview: each Create below ends in its own
            // SaveChangesAsync, so a file with several blank rows sees the previous row committed
            // and the sequence advances 26-0483, 26-0484, … instead of every blank row colliding on
            // the same number. Written back onto the row so the error paths, the audit trail and the
            // result summary all report the number that was actually issued.
            if (string.IsNullOrWhiteSpace(row.CertificateNumber))
            {
                row.CertificateNumber = await _certificateRepository.GenerateNextCertificateNumberAsync();
                _logger.LogInformation(
                    "Import row {RowNumber} had no Certificate #; assigned {CertNumber}",
                    row.RowNumber, row.CertificateNumber);
            }

            var certificate = new Certificate
            {
                Number = row.CertificateNumber,
                CompanyId = row.CompanyId!.Value,
                ProductId = row.ProductId!.Value,
                CertificationTypeId = row.CertificationTypeId!.Value,
                ProductCategoryId = row.ProductCategoryId, // QC #189 / DEF-278: honor template category
                // DEF-354: the template's Product Type now reaches the certificate too. DEF-278
                // added Category here and stopped, so ProductTypeId stayed NULL on every
                // imported certificate (0 of 5,405 on the go-live rehearsal) even though the
                // value was resolved and written to the Product record. It is read back by the
                // Edit view model (CertificateController.cs:844) and used for ProductType
                // delete-protection (AdminService.cs:1426), so a NULL here is not cosmetic.
                ProductTypeId = row.ProductTypeId,
                Status = row.Status,
                // DEF-353: these were `?? DateTime.UtcNow` / `?? .AddYears(1)`, which silently
                // invented dates for blank source cells — on the go-live rehearsal that gave an
                // Expired 2013 certificate a 2027 expiry, and gave two certificates an effective
                // date AFTER their expiration date. New rows without both dates are now rejected
                // in preview (see ProcessRowAsync), so these are unreachable by contract; throw
                // rather than fabricate if that ever stops being true. The per-row try/catch in
                // Phase 2 turns this into a row-level error, not a failed batch.
                EffectiveDate = row.EffectiveDate ?? throw new InvalidOperationException(
                    $"Effective Date missing for new certificate {row.CertificateNumber}; it must be rejected in preview, never defaulted."),
                ExpirationDate = row.ExpirationDate ?? throw new InvalidOperationException(
                    $"Expiration Date missing for new certificate {row.CertificateNumber}; it must be rejected in preview, never defaulted."),
                Location = row.Location,
                Country = row.Country,
                Asin = row.Asin,
                Upc = row.Upc,
                Gtin = row.Gtin,
                Ean = row.Ean,
                Partner = row.Partner ?? "None",
                CertificationResults = row.CertificationResults,
                PathwayId = row.PathwayId,
                CreatedBy = userId,
                UpdatedBy = userId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Certificates.Add(certificate);
            await _context.SaveChangesAsync();

            // CR-037: populate the cert-level country join rows from the resolved Country cell.
            SyncCertificateCountries(certificate, row.Countries);

            // CR-171: populate the cert-level Partners (M2M) from the (possibly pipe/comma-delimited)
            // Partner cell, and normalize the denormalized primary to the first matched partner.
            await SyncCertificatePartnersAsync(certificate, row.Partner);

            // Add tags
            if (tagIds.Any())
            {
                var tagsToAdd = await _context.Tags.Where(t => tagIds.Contains(t.Id)).ToListAsync();
                foreach (var tag in tagsToAdd)
                {
                    certificate.Tags.Add(tag);
                }
            }
            await _context.SaveChangesAsync();

            // Create CertificateProductId entries (multi-ASIN support)
            await CreateProductIdEntriesAsync(certificate.Id, row.ProductIdEntries, userId);

            // Upload PDF if provided
            await UploadPdfFromPathAsync(certificate, row.PdfFilePath);

            // Log to audit
            await _auditService.LogEntityChangeAsync(
                certificate.Id.ToString(),
                certificate.Number,
                AuditAction.Create,
                null,
                certificate,
                userId,
                username,
                reason ?? "Created via CSV import",
                AuditChangeSource.Import,
                importBatchId
            );
        }

        // CR-171: resolve a (possibly pipe/comma-delimited) Partner cell into the cert-level Partners
        // (M2M) collection and normalize the denormalized primary to the first matched name.
        // "None"/blank → no partner tags. Requires cert.Partners loaded for the update path.
        /// <summary>
        /// CR-037: replaces a certificate's country join rows from the resolved import list and
        /// re-derives the denormalized primary. Mirrors SyncCertificatePartnersAsync below.
        ///
        /// Runs on create AND update — an import that refreshed only the primary would leave the
        /// join rows describing the previous load, and the country filter would then return stale
        /// matches for every re-imported certificate.
        /// </summary>
        private void SyncCertificateCountries(Certificate cert, List<string> countries)
        {
            cert.Countries.Clear();

            if (countries == null || countries.Count == 0)
            {
                // Nothing resolved. Leave cert.Country alone — validation deliberately keeps an
                // unrecognised value visible on the certificate so an analyst can correct it.
                return;
            }

            foreach (var name in countries)
            {
                cert.Countries.Add(new CertificateCountry
                {
                    CertificateId = cert.Id,
                    Country = name
                });
            }

            cert.Country = countries[0];
        }

        private async Task SyncCertificatePartnersAsync(Certificate cert, string? partnerCsv)
        {
            var names = (partnerCsv ?? string.Empty)
                .Split(new[] { ',', '|' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(n => n.Trim())
                .Where(n => n.Length > 0 && !n.Equals("None", StringComparison.OrdinalIgnoreCase))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            cert.Partners.Clear();
            if (names.Any())
            {
                var matched = await _context.Partners
                    .Where(p => !p.IsDeleted && names.Contains(p.Name))
                    .ToListAsync();
                foreach (var p in matched) cert.Partners.Add(p);
                cert.Partner = matched.FirstOrDefault()?.Name ?? "None";
            }
            else
            {
                cert.Partner = "None";
            }
        }

        private async Task UpdateCertificateAsync(
            CertificateImportRow row,
            List<int> tagIds,
            int userId,
            string username,
            string? reason,
            Guid? importBatchId = null)
        {
            var certificate = await _context.Certificates
                .Include(c => c.Company)
                .Include(c => c.Product)
                .Include(c => c.CertificationType)
                .Include(c => c.Tags)
                .Include(c => c.Partners) // CR-171: re-sync cert-level partners from the import
                // CR-037: required, not cosmetic. SyncCertificateCountries clears this collection
                // before re-adding; without the Include it arrives empty, the old rows survive, and
                // the re-inserts collide with the unique (CertificateId, Country) index — so a
                // re-import would fail to update countries at all.
                .Include(c => c.Countries)
                .FirstOrDefaultAsync(c => c.Id == row.ExistingCertificateId!.Value);

            if (certificate == null)
            {
                throw new InvalidOperationException($"Certificate not found: {row.CertificateNumber}");
            }

            // Capture old state for audit.
            //
            // DEF-391: this was a hand-written copy of 19 named fields. Two failure modes, and the
            // register already carries one of each:
            //   • Forget a field and its change is invisible — DEF-354 was exactly that, patching
            //     ProductTypeId in after the fact. Every field added to Certificate from now on
            //     would need remembering here, and nothing enforces it.
            //   • Every field NOT listed defaults to null/0 on the copy, so the diff reads it as a
            //     change from blank on every single import row — CertificateFile, CreatedAt,
            //     CreatedBy, CsfAttributes and the persisted Normalized* columns all produced
            //     phantom audit rows that nothing had touched.
            //
            // A detached AsNoTracking read of the same row solves both: it is the real prior state
            // of every scalar, so nothing is missed and nothing is invented. AsNoTracking is what
            // makes it a snapshot — a tracked read would return the same instance the assignments
            // below mutate, and old would equal new on every field.
            //
            // CR-023 undo is unaffected: ApplyOldValueToCertificate switches on the scalar field
            // names it can restore and ignores anything else, so extra fidelity here can only help
            // it, never confuse it.
            var oldCertificate = await _context.Certificates
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == certificate.Id);

            // Apply updates
            if (row.CompanyId.HasValue) certificate.CompanyId = row.CompanyId.Value;
            if (row.ProductId.HasValue) certificate.ProductId = row.ProductId.Value;
            if (row.CertificationTypeId.HasValue) certificate.CertificationTypeId = row.CertificationTypeId.Value;
            // === DEF-355: a column the file never carried must NOT be overwritten ===
            // These assignments used to be unconditional, so a re-import that omitted a column
            // read as "set this to empty" and silently destroyed data on every row it touched.
            // Proven on 23-0753 during the DEF-353 regression test: Cert Results 0.03 -> blank,
            // Partner Amazon -> blank (M2M link dropped too), Category -> blank.
            //
            // The rule now matches the one already documented for Pathway below: only write what
            // the file actually supplied. A column that IS present but blank still clears the
            // value — that is a deliberate edit, and remains supported.
            //
            // Status is the most dangerous of these: a blank/absent Status defaults to "Draft"
            // at parse time (:1286), so before this fix a file without a Status column reset
            // every certificate it touched to Draft.
            bool Provided(string field) => row.ProvidedColumns.Contains(field);

            if (Provided(nameof(Certificate.ProductCategoryId)))
                certificate.ProductCategoryId = row.ProductCategoryId; // QC #189 / DEF-278: honor template category
            // DEF-354: Product Type follows the same rule.
            if (row.ProductTypeId.HasValue) certificate.ProductTypeId = row.ProductTypeId.Value;
            if (Provided(nameof(Certificate.Status)))
                certificate.Status = row.Status;
            if (row.EffectiveDate.HasValue) certificate.EffectiveDate = row.EffectiveDate.Value;
            if (row.ExpirationDate.HasValue) certificate.ExpirationDate = row.ExpirationDate.Value;
            if (Provided(nameof(Certificate.Location))) certificate.Location = row.Location;
            if (Provided(nameof(Certificate.Country))) certificate.Country = row.Country;
            if (Provided(nameof(Certificate.Asin))) certificate.Asin = row.Asin;
            if (Provided(nameof(Certificate.Upc))) certificate.Upc = row.Upc;
            if (Provided(nameof(Certificate.Gtin))) certificate.Gtin = row.Gtin;
            if (Provided(nameof(Certificate.Ean))) certificate.Ean = row.Ean;
            if (Provided(nameof(Certificate.Country)))
                SyncCertificateCountries(certificate, row.Countries); // CR-037 (sets denorm + join rows)
            if (Provided(nameof(Certificate.Partner)))
                await SyncCertificatePartnersAsync(certificate, row.Partner); // CR-171 (sets denorm + M2M)
            if (Provided(nameof(Certificate.CertificationResults)))
                certificate.CertificationResults = row.CertificationResults;
            // Pathway: only set when the CSV actually provided one, so a file without a Pathway
            // column (e.g. the current export round-trip) never clears an existing pathway.
            if (row.PathwayId.HasValue) certificate.PathwayId = row.PathwayId.Value;
            certificate.UpdatedBy = userId;
            certificate.UpdatedAt = DateTime.UtcNow;

            // Update tags - clear and re-add
            certificate.Tags.Clear();
            if (tagIds.Any())
            {
                var tagsToAdd = await _context.Tags.Where(t => tagIds.Contains(t.Id)).ToListAsync();
                foreach (var tag in tagsToAdd)
                {
                    certificate.Tags.Add(tag);
                }
            }

            await _context.SaveChangesAsync();

            // CR-015: cascade product-ID statuses on cert update from import.
            await _productIdService.SyncProductIdStatusesForCertificateAsync(certificate.Id);

            // Create CertificateProductId entries (multi-ASIN support)
            await CreateProductIdEntriesAsync(certificate.Id, row.ProductIdEntries, userId);

            // Upload PDF if provided
            await UploadPdfFromPathAsync(certificate, row.PdfFilePath);

            // Log to audit
            await _auditService.LogEntityChangeAsync(
                certificate.Id.ToString(),
                certificate.Number,
                AuditAction.Update,
                oldCertificate,
                certificate,
                userId,
                username,
                reason ?? "Updated via CSV import",
                AuditChangeSource.Import,
                importBatchId
            );
        }

        /// <summary>
        /// Creates CertificateProductId entries for a certificate from import data
        /// Checks for duplicates per-certificate (same ASIN can exist on different certs)
        /// </summary>
        private async Task CreateProductIdEntriesAsync(int certificateId, List<ProductIdEntry> entries, int userId)
        {
            if (entries == null || !entries.Any())
                return;

            // Get existing product IDs for THIS certificate to avoid duplicates
            var existingProductIds = await _context.Set<CertificateProductId>()
                .Where(p => p.CertificateId == certificateId)
                .ToListAsync();

            int displayOrder = existingProductIds.Count;

            foreach (var entry in entries)
            {
                // Skip if this type+value already exists on THIS certificate
                var exists = existingProductIds.Any(p =>
                    p.ProductIdType.Equals(entry.ProductIdType, StringComparison.OrdinalIgnoreCase) &&
                    p.ProductIdValue.Equals(entry.ProductIdValue, StringComparison.OrdinalIgnoreCase));

                if (exists)
                {
                    _logger.LogInformation("Skipping duplicate product ID {Type}:{Value} for certificate {CertId}",
                        entry.ProductIdType, entry.ProductIdValue, certificateId);
                    continue;
                }

                var productId = new CertificateProductId
                {
                    CertificateId = certificateId,
                    ProductIdType = entry.ProductIdType,
                    ProductIdValue = entry.ProductIdValue,
                    ProductName = entry.ProductName,
                    ProductSku = entry.ProductSku,
                    Description = entry.AmazonCode,
                    Partner = entry.Partner,
                    // CR-015: Status will be auto-set by SyncProductIdStatusesForCertificateAsync below.
                    Status = "Inactive",
                    IsActive = true,
                    IsPublic = true,
                    DisplayOrder = displayOrder++,
                    CreatedBy = userId,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedBy = userId,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.Set<CertificateProductId>().Add(productId);
                _logger.LogInformation("Created product ID {Type}:{Value} for certificate {CertId}",
                    entry.ProductIdType, entry.ProductIdValue, certificateId);
            }

            await _context.SaveChangesAsync();

            // CR-015: After insert, sync per-row status to the parent cert's derived status.
            await _productIdService.SyncProductIdStatusesForCertificateAsync(certificateId);
        }

        /// <summary>
        /// Uploads a PDF file from a local file path and associates it with the certificate
        /// </summary>
        private async Task UploadPdfFromPathAsync(Certificate certificate, string? pdfFilePath)
        {
            if (string.IsNullOrWhiteSpace(pdfFilePath))
                return;

            // Resolve the file path
            var fullPath = pdfFilePath;
            if (!Path.IsPathRooted(fullPath))
            {
                fullPath = Path.GetFullPath(fullPath);
            }

            if (!File.Exists(fullPath))
            {
                _logger.LogWarning("PDF file not found at path: {PdfPath} for certificate {CertNumber}",
                    fullPath, certificate.Number);
                return;
            }

            if (!fullPath.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("File is not a PDF: {PdfPath} for certificate {CertNumber}",
                    fullPath, certificate.Number);
                return;
            }

            try
            {
                // Copy the file to the uploads directory.
                // DEF-286: write through the shared IUploadPathResolver so imported PDFs land in the
                // same configured root as direct uploads (DEF-262 fixed imports writing to wwwroot
                // while the rest of the app read from App_Data, orphaning the files).
                var uploadsDir = _uploadPaths.CertificatesDir;
                Directory.CreateDirectory(uploadsDir);

                var fileName = $"{certificate.Number}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.pdf";
                var destPath = Path.Combine(uploadsDir, fileName);

                File.Copy(fullPath, destPath, overwrite: true);

                certificate.CertificateFile = Path.Combine("uploads", "certificates", fileName).Replace("\\", "/");
                await _context.SaveChangesAsync();

                _logger.LogInformation("PDF uploaded from {SourcePath} for certificate {CertNumber}",
                    fullPath, certificate.Number);

                // Try to generate thumbnail
                try
                {
                    var thumbnailPath = await _fileUploadService.GeneratePdfThumbnailAsync(destPath, certificate.Number);
                    if (!string.IsNullOrEmpty(thumbnailPath))
                    {
                        certificate.CertificateImagePath = thumbnailPath;
                        await _context.SaveChangesAsync();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to generate PDF thumbnail for certificate {CertNumber}", certificate.Number);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to upload PDF from path {PdfPath} for certificate {CertNumber}",
                    fullPath, certificate.Number);
            }
        }

        #endregion
    }
}
