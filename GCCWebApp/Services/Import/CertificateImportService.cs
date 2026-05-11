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
        private const string CACHE_KEY_PREFIX = "CertImport_";
        private const int CACHE_MINUTES = 30;

        public CertificateImportService(
            ApplicationDbContext context,
            IAuditService auditService,
            IMemoryCache cache,
            ILogger<CertificateImportService> logger,
            IWebHostEnvironment environment,
            IFileUploadService fileUploadService,
            IProductIdService productIdService)
        {
            _context = context;
            _auditService = auditService;
            _cache = cache;
            _logger = logger;
            _environment = environment;
            _fileUploadService = fileUploadService;
            _productIdService = productIdService;
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
        private static readonly string[] CertificationTypeColumns = { "CERTIFICATION_TYPE", "CertificationType", "certification_type", "Certification Type", "Cert Type" };
        private static readonly string[] StatusColumns = { "STATUS", "Status", "status" };
        private static readonly string[] EffectiveDateColumns = { "EFFECTIVE_DATE", "EffectiveDate", "effective_date", "Effective Date", "Eff Date" };
        private static readonly string[] ExpirationDateColumns = { "EXPIRATION_DATE", "ExpirationDate", "expiration_date", "Expiration Date", "Exp Date" };
        private static readonly string[] CertificateLocationColumns = { "CERTIFICATE_LOCATION", "Location", "certificate_location", "location" };
        private static readonly string[] CertificateCountryColumns = { "CERTIFICATE_COUNTRY", "Country", "certificate_country", "country" };
        private static readonly string[] PartnerColumns = { "PARTNER", "Partner", "partner" };
        private static readonly string[] CertificationResultsColumns = { "CERTIFICATION_RESULTS", "CertificationResults", "certification_results", "Certification Results", "Cert Results" };
        // External Tag preferred (matches export column 19); generic TAGS kept for legacy clients.
        // "Internal Tag" column from exports is silently ignored by the import data model (single Tags field).
        private static readonly string[] TagsColumns = { "External Tag", "ExternalTag", "EXTERNAL_TAG", "TAGS", "Tags", "tags" };
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
                var productTypes = await _context.ProductTypes.Where(pt => pt.IsActive).ToListAsync();
                var tags = await _context.Tags.Where(t => t.IsActive).ToListAsync();
                var categories = await _context.ProductCategories.Where(c => c.IsActive).ToListAsync();
                var existingCerts = await _context.Certificates
                    .Include(c => c.Company)
                    .Include(c => c.Product)
                    .Include(c => c.CertificationType)
                    .Include(c => c.Tags)
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

                // Group rows by certificate number to handle multi-ASIN rows
                var groupedRows = csvRows
                    .Where(r => !string.IsNullOrWhiteSpace(r.CertificateNumber) ||
                                !string.IsNullOrWhiteSpace(r.CompanyName) ||
                                !string.IsNullOrWhiteSpace(r.ProductName) ||
                                !string.IsNullOrWhiteSpace(r.CertificationType))
                    .GroupBy(r => r.CertificateNumber, StringComparer.OrdinalIgnoreCase)
                    .ToList();

                foreach (var group in groupedRows)
                {
                    var primaryRow = group.First();

                    var importRow = await ProcessRowAsync(
                        primaryRow,
                        companies,
                        products,
                        certTypes,
                        productTypes,
                        tags,
                        existingCerts,
                        companiesToCreate,
                        productsToCreate,
                        productTypesToCreate,
                        tagsToCreate
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

                // Set entity creation summaries
                result.CompaniesToCreate = companiesToCreate.Count;
                result.ProductsToCreate = productsToCreate.Count;
                result.ProductTypesToCreate = productTypesToCreate.Count;
                result.TagsToCreate = tagsToCreate.Count;
                result.NewCompanyNames = companiesToCreate.ToList();
                result.NewProductNames = productsToCreate.ToList();
                result.NewProductTypeNames = productTypesToCreate.ToList();
                result.NewTagNames = tagsToCreate.ToList();

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

            // Use execution strategy to handle SQL Server retries with transactions
            var strategy = _context.Database.CreateExecutionStrategy();

            await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync();

                try
                {
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

                    var product = new Product
                    {
                        Name = productName,
                        CategoryId = defaultCategory.Id,
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
                        if (!row.ProductId.HasValue && createdProducts.TryGetValue(row.ProductName, out var pId))
                            row.ProductId = pId;

                        // Resolve tag IDs
                        var allTagIds = new List<int>(row.TagIds);
                        foreach (var tagName in row.TagsToCreate)
                        {
                            if (createdTags.TryGetValue(tagName, out var tId))
                                allTagIds.Add(tId);
                        }

                        if (row.RowStatus == ImportRowStatus.New)
                        {
                            await CreateCertificateAsync(row, allTagIds, userId, username, reason);
                            result.CertificatesCreated++;
                        }
                        else if (row.RowStatus == ImportRowStatus.Modified)
                        {
                            await UpdateCertificateAsync(row, allTagIds, userId, username, reason);
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

            while (await csv.ReadAsync())
            {
                rowNumber++;
                var status = GetFieldWithMapping(csv, StatusColumns);
                var partner = GetFieldWithMapping(csv, PartnerColumns);

                rows.Add(new CertificateImportCsvRow
                {
                    RowNumber = rowNumber,
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
                    // Company fields
                    CompanyName = GetFieldWithMapping(csv, CompanyNameColumns),
                    CompanyLocation = GetFieldWithMapping(csv, CompanyLocationColumns),
                    CompanyCountry = GetFieldWithMapping(csv, CompanyCountryColumns),
                    CompanyEmail = GetFieldWithMapping(csv, CompanyEmailColumns),
                    CompanyWebsite = GetFieldWithMapping(csv, CompanyWebsiteColumns),
                    // Product fields
                    ProductName = GetFieldWithMapping(csv, ProductNameColumns),
                    ProductType = GetFieldWithMapping(csv, ProductTypeColumns),
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
            List<ProductType> productTypes,
            List<Tag> tags,
            List<Certificate> existingCerts,
            HashSet<string> companiesToCreate,
            HashSet<string> productsToCreate,
            HashSet<string> productTypesToCreate,
            HashSet<string> tagsToCreate)
        {
            var row = new CertificateImportRow
            {
                RowNumber = csvRow.RowNumber,
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
                CertificationResults = csvRow.CertificationResults
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

            // Validate CertificationResults length
            if (!string.IsNullOrWhiteSpace(row.CertificationResults) && row.CertificationResults.Length > 1000)
                row.ValidationMessages.Add("Certification Results exceeds maximum length of 1000 characters");

            // Validate company email format if provided
            if (!string.IsNullOrWhiteSpace(csvRow.CompanyEmail) && !IsValidEmail(csvRow.CompanyEmail))
                row.ValidationMessages.Add($"Invalid Company Email format: {csvRow.CompanyEmail}");

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

            // === CertificationType Lookup (must exist - no auto-create) ===
            var certType = certTypes.FirstOrDefault(ct =>
                ct.Name.Equals(row.CertificationType, StringComparison.OrdinalIgnoreCase));
            if (certType != null)
                row.CertificationTypeId = certType.Id;
            else
                row.ValidationMessages.Add($"Certification Type not found: '{row.CertificationType}'. Certification Types must exist before import.");

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

            // CertificationResults change detection
            if ((existing.CertificationResults ?? "") != (newData.CertificationResults ?? ""))
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
                CertificationResults = row.CertificationResults,
                CreatedBy = userId,
                UpdatedBy = userId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Certificates.Add(certificate);
            await _context.SaveChangesAsync();

            // Add tags
            if (tagIds.Any())
            {
                var tagsToAdd = await _context.Tags.Where(t => tagIds.Contains(t.Id)).ToListAsync();
                foreach (var tag in tagsToAdd)
                {
                    certificate.Tags.Add(tag);
                }
                await _context.SaveChangesAsync();
            }

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
                AuditChangeSource.Import
            );
        }

        private async Task UpdateCertificateAsync(
            CertificateImportRow row,
            List<int> tagIds,
            int userId,
            string username,
            string? reason)
        {
            var certificate = await _context.Certificates
                .Include(c => c.Company)
                .Include(c => c.Product)
                .Include(c => c.CertificationType)
                .Include(c => c.Tags)
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
                Partner = certificate.Partner,
                CertificationResults = certificate.CertificationResults
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
            certificate.CertificationResults = row.CertificationResults;
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
                AuditChangeSource.Import
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
                // Copy the file to the uploads directory
                var uploadsDir = Path.Combine(_environment.WebRootPath, "uploads", "certificates");
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
