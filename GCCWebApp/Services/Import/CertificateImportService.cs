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

        #region Column Name Mappings (supports both UPPERCASE_WITH_UNDERSCORES and PascalCase formats)

        private static readonly string[] CertificateNumberColumns = { "CERTIFICATE_NUMBER", "CertificateNumber", "certificate_number" };
        private static readonly string[] CompanyNameColumns = { "COMPANY_NAME", "CompanyName", "company_name" };
        private static readonly string[] CompanyLocationColumns = { "COMPANY_LOCATION", "CompanyLocation", "company_location" };
        private static readonly string[] CompanyCountryColumns = { "COMPANY_COUNTRY", "CompanyCountry", "company_country" };
        private static readonly string[] CompanyEmailColumns = { "COMPANY_EMAIL", "CompanyEmail", "company_email" };
        private static readonly string[] CompanyWebsiteColumns = { "COMPANY_WEBSITE", "CompanyWebsite", "company_website" };
        private static readonly string[] ProductNameColumns = { "PRODUCT_NAME", "ProductName", "product_name" };
        private static readonly string[] ProductTypeColumns = { "PRODUCT_TYPE", "ProductType", "product_type" };
        private static readonly string[] CertificationTypeColumns = { "CERTIFICATION_TYPE", "CertificationType", "certification_type" };
        private static readonly string[] StatusColumns = { "STATUS", "Status", "status" };
        private static readonly string[] EffectiveDateColumns = { "EFFECTIVE_DATE", "EffectiveDate", "effective_date" };
        private static readonly string[] ExpirationDateColumns = { "EXPIRATION_DATE", "ExpirationDate", "expiration_date" };
        private static readonly string[] CertificateLocationColumns = { "CERTIFICATE_LOCATION", "Location", "certificate_location", "location" };
        private static readonly string[] CertificateCountryColumns = { "CERTIFICATE_COUNTRY", "Country", "certificate_country", "country" };
        private static readonly string[] PartnerColumns = { "PARTNER", "Partner", "partner" };
        private static readonly string[] CertificationResultsColumns = { "CERTIFICATION_RESULTS", "CertificationResults", "certification_results" };
        private static readonly string[] TagsColumns = { "TAGS", "Tags", "tags" };
        private static readonly string[] AsinColumns = { "ASIN", "Asin", "asin" };
        private static readonly string[] UpcColumns = { "UPC", "Upc", "upc" };
        private static readonly string[] GtinColumns = { "GTIN", "Gtin", "gtin" };
        private static readonly string[] EanColumns = { "EAN", "Ean", "ean" };

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
        /// Validates email format
        /// </summary>
        private static bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

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

                // Process each row
                foreach (var csvRow in csvRows)
                {
                    // Skip empty rows (all required fields are empty)
                    if (string.IsNullOrWhiteSpace(csvRow.CertificateNumber) &&
                        string.IsNullOrWhiteSpace(csvRow.CompanyName) &&
                        string.IsNullOrWhiteSpace(csvRow.ProductName) &&
                        string.IsNullOrWhiteSpace(csvRow.CertificationType))
                    {
                        continue;
                    }

                    var importRow = await ProcessRowAsync(
                        csvRow,
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

                // Set entity creation summaries
                result.CompaniesToCreate = companiesToCreate.Count;
                result.ProductsToCreate = productsToCreate.Count;
                result.ProductTypesToCreate = productTypesToCreate.Count;
                result.TagsToCreate = tagsToCreate.Count;
                result.NewCompanyNames = companiesToCreate.ToList();
                result.NewProductNames = productsToCreate.ToList();
                result.NewProductTypeNames = productTypesToCreate.ToList();
                result.NewTagNames = tagsToCreate.ToList();

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

            using var reader = new StreamReader(csvFile.OpenReadStream(), Encoding.UTF8);
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

        #endregion
    }
}
