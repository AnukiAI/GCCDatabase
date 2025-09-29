using Microsoft.EntityFrameworkCore;
using GCCWebApp.Data;
using GCCWebApp.Models;
using System.Text.Json;

namespace GCCWebApp.Services.ETL
{
    public class DataExtractor : IDataExtractor
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<DataExtractor> _logger;
        private readonly HttpClient _httpClient;

        public DataExtractor(ApplicationDbContext context, ILogger<DataExtractor> logger, HttpClient httpClient)
        {
            _context = context;
            _logger = logger;
            _httpClient = httpClient;
        }

        public async Task<ETLExtractedData> ExtractCertificatesAsync(DateTime? lastExtractTime = null)
        {
            _logger.LogInformation("Starting certificate data extraction. Last extract time: {LastExtractTime}", lastExtractTime);

            var extractedData = new ETLExtractedData
            {
                DataSource = "Certificates",
                ExtractionTime = DateTime.UtcNow
            };

            try
            {
                var query = _context.Certificates
                    .Include(c => c.Company)
                    .Include(c => c.Product)
                        .ThenInclude(p => p.Category)
                    .Include(c => c.Product)
                        .ThenInclude(p => p.ProductType)
                    .Include(c => c.CertificationType)
                    .Include(c => c.Tags)
                    .Include(c => c.CustomFieldValues)
                        .ThenInclude(cfv => cfv.Field)
                    .AsQueryable();

                // Apply incremental filter if provided
                if (lastExtractTime.HasValue)
                {
                    query = query.Where(c => c.UpdatedAt > lastExtractTime.Value);
                }

                var certificates = await query.ToListAsync();

                // Transform to extraction format
                var certificateData = certificates.Select(c => new
                {
                    c.Id,
                    c.Number,
                    c.Status,
                    c.EffectiveDate,
                    c.ExpirationDate,
                    c.Location,
                    c.Country,
                    c.Asin,
                    c.Upc,
                    c.Gtin,
                    c.Ean,
                    c.CertificateFile,
                    c.CreatedAt,
                    c.UpdatedAt,
                    Company = new
                    {
                        c.Company.Id,
                        c.Company.Name,
                        c.Company.Location,
                        c.Company.Country
                    },
                    Product = new
                    {
                        c.Product.Id,
                        c.Product.Name,
                        c.Product.Description,
                        Category = c.Product.Category?.Name,
                        ProductType = c.Product.ProductType?.Name
                    },
                    CertificationType = new
                    {
                        c.CertificationType.Id,
                        c.CertificationType.Name,
                        c.CertificationType.Description
                    },
                    Tags = c.Tags.Select(t => t.Name).ToList(),
                    CustomFields = c.CustomFieldValues.Select(cfv => new
                    {
                        FieldName = cfv.Field.Name,
                        TextValue = cfv.TextValue,
                        NumberValue = cfv.NumberValue,
                        DateValue = cfv.DateValue,
                        BooleanValue = cfv.BooleanValue
                    }).ToList(),
                    // Calculated fields
                    IsExpired = c.ExpirationDate < DateTime.UtcNow,
                    DaysUntilExpiration = (c.ExpirationDate - DateTime.UtcNow).Days,
                    ValidityPeriod = (c.ExpirationDate - c.EffectiveDate).Days
                }).Cast<object>().ToList();

                extractedData.Data.Add("Certificates", certificateData);
                extractedData.TotalRecords = certificateData.Count();

                // Add metadata
                extractedData.Metadata.Add("ExtractionType", lastExtractTime.HasValue ? "Incremental" : "Full");
                extractedData.Metadata.Add("TotalCertificates", certificateData.Count());
                extractedData.Metadata.Add("ActiveCertificates", certificates.Count(c => c.Status == "active"));
                extractedData.Metadata.Add("ExpiredCertificates", certificates.Count(c => c.ExpirationDate < DateTime.UtcNow));

                _logger.LogInformation("Certificate extraction completed. Extracted {Count} records", certificateData.Count());
                return extractedData;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during certificate data extraction");
                throw;
            }
        }

        public async Task<ETLExtractedData> ExtractCompaniesAsync(DateTime? lastExtractTime = null)
        {
            _logger.LogInformation("Starting company data extraction. Last extract time: {LastExtractTime}", lastExtractTime);

            var extractedData = new ETLExtractedData
            {
                DataSource = "Companies",
                ExtractionTime = DateTime.UtcNow
            };

            try
            {
                var query = _context.Companies
                    .Include(c => c.Certificates)
                    .Include(c => c.Products)
                    .Include(c => c.Users)
                    .AsQueryable();

                if (lastExtractTime.HasValue)
                {
                    query = query.Where(c => c.UpdatedAt > lastExtractTime.Value);
                }

                var companies = await query.ToListAsync();

                var companyData = companies.Select(c => new
                {
                    c.Id,
                    c.Name,
                    c.ContactInfo,
                    c.Location,
                    c.Country,
                    c.IsActive,
                    c.CreatedAt,
                    c.UpdatedAt,
                    // Aggregated data
                    TotalCertificates = c.Certificates.Count,
                    ActiveCertificates = c.Certificates.Count(cert => cert.Status == "active"),
                    TotalProducts = c.Products.Count,
                    TotalUsers = c.Users.Count,
                    // Latest certificate info
                    LatestCertificateDate = c.Certificates.Max(cert => (DateTime?)cert.CreatedAt),
                    ExpiringCertificatesCount = c.Certificates.Count(cert => 
                        cert.ExpirationDate <= DateTime.UtcNow.AddDays(30) && cert.Status == "active")
                }).Cast<object>().ToList();

                extractedData.Data.Add("Companies", companyData);
                extractedData.TotalRecords = companyData.Count;

                extractedData.Metadata.Add("ExtractionType", lastExtractTime.HasValue ? "Incremental" : "Full");
                extractedData.Metadata.Add("TotalCompanies", companyData.Count);
                extractedData.Metadata.Add("ActiveCompanies", companies.Count(c => c.IsActive));

                _logger.LogInformation("Company extraction completed. Extracted {Count} records", companyData.Count);
                return extractedData;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during company data extraction");
                throw;
            }
        }

        public async Task<ETLExtractedData> ExtractProductsAsync(DateTime? lastExtractTime = null)
        {
            _logger.LogInformation("Starting product data extraction. Last extract time: {LastExtractTime}", lastExtractTime);

            var extractedData = new ETLExtractedData
            {
                DataSource = "Products",
                ExtractionTime = DateTime.UtcNow
            };

            try
            {
                var query = _context.Products
                    .Include(p => p.Company)
                    .Include(p => p.Category)
                    .Include(p => p.ProductType)
                    .Include(p => p.ProductSubcategory)
                    .Include(p => p.Certificates)
                    .Include(p => p.Tags)
                    .AsQueryable();

                if (lastExtractTime.HasValue)
                {
                    query = query.Where(p => p.UpdatedAt > lastExtractTime.Value);
                }

                var products = await query.ToListAsync();

                var productData = products.Select(p => new
                {
                    p.Id,
                    p.Name,
                    p.Description,
                    p.Model,
                    p.Sku,
                    p.Upc,
                    p.Asin,
                    p.IsActive,
                    p.CreatedAt,
                    p.UpdatedAt,
                    Company = p.Company != null ? new
                    {
                        p.Company.Id,
                        p.Company.Name
                    } : null,
                    Category = p.Category?.Name,
                    ProductType = p.ProductType?.Name,
                    ProductSubcategory = p.ProductSubcategory?.Name,
                    Tags = p.Tags.Select(t => t.Name).ToList(),
                    // Certificate statistics
                    TotalCertificates = p.Certificates.Count,
                    ActiveCertificates = p.Certificates.Count(c => c.Status == "active"),
                    ExpiredCertificates = p.Certificates.Count(c => c.ExpirationDate < DateTime.UtcNow),
                    LatestCertificateDate = p.Certificates.Max(c => (DateTime?)c.CreatedAt)
                }).Cast<object>().ToList();

                extractedData.Data.Add("Products", productData);
                extractedData.TotalRecords = productData.Count();

                extractedData.Metadata.Add("ExtractionType", lastExtractTime.HasValue ? "Incremental" : "Full");
                extractedData.Metadata.Add("TotalProducts", productData.Count());
                extractedData.Metadata.Add("ActiveProducts", products.Count(p => p.IsActive));

                _logger.LogInformation("Product extraction completed. Extracted {Count} records", productData.Count());
                return extractedData;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during product data extraction");
                throw;
            }
        }

        public async Task<ETLExtractedData> ExtractFromExternalSourceAsync(ExternalDataSource source)
        {
            _logger.LogInformation("Starting external data extraction from source: {SourceName}", source.Name);

            var extractedData = new ETLExtractedData
            {
                DataSource = source.Name,
                ExtractionTime = DateTime.UtcNow
            };

            try
            {
                switch (source.Type.ToLower())
                {
                    case "api":
                        return await ExtractFromApiAsync(source, extractedData);
                    case "csv":
                        return await ExtractFromCsvAsync(source, extractedData);
                    case "database":
                        return await ExtractFromDatabaseAsync(source, extractedData);
                    default:
                        throw new NotSupportedException($"External data source type '{source.Type}' is not supported");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during external data extraction from source: {SourceName}", source.Name);
                throw;
            }
        }

        private async Task<ETLExtractedData> ExtractFromApiAsync(ExternalDataSource source, ETLExtractedData extractedData)
        {
            try
            {
                // Add authentication headers if provided
                if (source.Authentication.ContainsKey("ApiKey"))
                {
                    _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {source.Authentication["ApiKey"]}");
                }

                var response = await _httpClient.GetStringAsync(source.ConnectionString);
                var data = JsonSerializer.Deserialize<List<Dictionary<string, object>>>(response);

                if (data != null)
                {
                    extractedData.Data.Add("ExternalApiData", data.Cast<object>().ToList());
                    extractedData.TotalRecords = data.Count;
                }

                _logger.LogInformation("API extraction completed. Extracted {Count} records from {Source}", 
                    extractedData.TotalRecords, source.Name);

                return extractedData;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting from API source: {Source}", source.Name);
                throw;
            }
        }

        private async Task<ETLExtractedData> ExtractFromCsvAsync(ExternalDataSource source, ETLExtractedData extractedData)
        {
            try
            {
                var lines = await File.ReadAllLinesAsync(source.ConnectionString);
                if (lines.Length == 0) return extractedData;

                var headers = lines[0].Split(',');
                var data = new List<object>();

                for (int i = 1; i < lines.Length; i++)
                {
                    var values = lines[i].Split(',');
                    var record = new Dictionary<string, object>();
                    
                    for (int j = 0; j < Math.Min(headers.Length, values.Length); j++)
                    {
                        record[headers[j].Trim()] = values[j].Trim();
                    }
                    
                    data.Add(record);
                }

                extractedData.Data.Add("CsvData", data);
                extractedData.TotalRecords = data.Count;

                _logger.LogInformation("CSV extraction completed. Extracted {Count} records from {Source}", 
                    extractedData.TotalRecords, source.Name);

                return extractedData;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting from CSV source: {Source}", source.Name);
                throw;
            }
        }

        private async Task<ETLExtractedData> ExtractFromDatabaseAsync(ExternalDataSource source, ETLExtractedData extractedData)
        {
            try
            {
                // This would require a more sophisticated implementation based on the database type
                // For now, we'll implement a basic SQL Server extraction
                _logger.LogWarning("Database extraction not fully implemented for source: {Source}", source.Name);

                // Placeholder for database extraction logic
                // Would use SqlConnection, etc. based on the database type
                await Task.CompletedTask; // Placeholder await to satisfy async signature

                return extractedData;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting from database source: {Source}", source.Name);
                throw;
            }
        }
    }
}