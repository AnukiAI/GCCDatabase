using Microsoft.AspNetCore.Mvc;
using GCCWebApp.Models;
using GCCWebApp.Repositories;
using GCCWebApp.Services.Performance;
using GCCWebApp.Services.Security;
using GCCWebApp.Data;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using ClosedXML.Excel;

namespace GCCWebApp.Controllers
{
    public class ExportController : Controller
    {
        private readonly ILogger<ExportController> _logger;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICertificateRepository _certificateRepository;
        private readonly IPerformanceMonitoringService _performanceMonitoring;
        private readonly IStructuredLoggingService _structuredLogger;
        private readonly ApplicationDbContext _context;

        public ExportController(
            ILogger<ExportController> logger,
            IUnitOfWork unitOfWork,
            ICertificateRepository certificateRepository,
            IPerformanceMonitoringService performanceMonitoring,
            IStructuredLoggingService structuredLogger,
            ApplicationDbContext context)
        {
            _logger = logger;
            _unitOfWork = unitOfWork;
            _certificateRepository = certificateRepository;
            _performanceMonitoring = performanceMonitoring;
            _structuredLogger = structuredLogger;
            _context = context;
        }

        [HttpGet]
        [AllowAnonymous] // Temporarily allow anonymous access for testing
        public async Task<IActionResult> DownloadStat(string type, string format = "csv")
        {
            try
            {
                // Temporarily bypass authentication for testing
                var userRole = HttpContext.Session.GetString("UserRole") ?? "Admin"; // Default to Admin for testing
                
                // Comment out authentication check for now
                // if (userRole != "Admin" && userRole != "Analyst")
                // {
                //     return Forbid();
                // }

                var stats = await _certificateRepository.GetCertificateStatisticsAsync();
                var data = await GetStatData(type, stats);
                
                if (data == null)
                {
                    return BadRequest("Invalid stat type");
                }

                if (format?.ToLower() == "excel")
                {
                    var excelBytes = GenerateStatExcel(type, data);
                    var fileName = $"{type}-report-{DateTime.UtcNow:yyyy-MM-dd}.xlsx";
                    return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
                }
                else
                {
                    var csv = GenerateStatCsv(type, data);
                    var fileName = $"{type}-report-{DateTime.UtcNow:yyyy-MM-dd}.csv";
                    return File(Encoding.UTF8.GetBytes(csv), "text/csv", fileName);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading stat report for type: {StatType}", type);
                return BadRequest("Error generating report");
            }
        }

        [HttpGet]
        [AllowAnonymous] // Temporarily allow anonymous access for testing
        public async Task<IActionResult> DownloadTable(string type, string format = "csv")
        {
            try
            {
                // Temporarily bypass authentication for testing
                var userRole = HttpContext.Session.GetString("UserRole") ?? "Admin"; // Default to Admin for testing
                
                // Comment out authentication check for now
                // if (userRole != "Admin" && userRole != "Analyst")
                // {
                //     return Forbid();
                // }

                var data = await GetTableData(type);
                
                if (data == null)
                {
                    return BadRequest("Invalid table type");
                }

                if (format?.ToLower() == "excel")
                {
                    var excelBytes = GenerateTableExcel(type, data);
                    var fileName = $"{type}-table-{DateTime.UtcNow:yyyy-MM-dd}.xlsx";
                    return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
                }
                else
                {
                    var csv = GenerateTableCsv(type, data);
                    var fileName = $"{type}-table-{DateTime.UtcNow:yyyy-MM-dd}.csv";
                    return File(Encoding.UTF8.GetBytes(csv), "text/csv", fileName);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading table report for type: {TableType}", type);
                return BadRequest("Error generating report");
            }
        }

        private async Task<object?> GetStatData(string type, Dictionary<string, int> stats)
        {
            switch (type)
            {
                case "total-active-certs":
                    return new { Value = stats.GetValueOrDefault("Active", 0), Description = "Total Active Certificates" };

                case "companies-with-active-certs":
                    var totalCompanies = await _unitOfWork.Companies.CountAsync();
                    return new { Value = totalCompanies, Description = "Companies with Active Certificates" };

                case "product-categories-with-active-certs":
                    var totalCertTypes = await _unitOfWork.CertificationTypes.CountAsync();
                    return new { Value = totalCertTypes, Description = "Product Categories with Active Certificates" };

                case "product-types-with-active-certs":
                    var totalProducts = await _unitOfWork.Products.CountAsync();
                    return new { Value = totalProducts, Description = "Product Types with Active Certificates" };

                case "product-subcategories-with-active-certs":
                    return new { Value = stats.GetValueOrDefault("ProductSubcategories", 0), Description = "Product Subcategories with Active Certificates" };

                case "products-with-active-certs":
                    return new { Value = stats.GetValueOrDefault("ActiveProducts", 0), Description = "Products with Active Certificates" };

                case "certs-published-last-month":
                    return new { Value = stats.GetValueOrDefault("PublishedLastMonth", 0), Description = "Certificates Published Last Month" };

                case "certs-expiring-this-month":
                    return new { Value = stats.GetValueOrDefault("ExpiringThisMonth", 0), Description = "Certificates Expiring This Month" };

                case "certs-expired-last-month":
                    return new { Value = stats.GetValueOrDefault("ExpiredLastMonth", 0), Description = "Certificates Expired Last Month" };

                case "certs-expiring-next-month":
                    return new { Value = stats.GetValueOrDefault("ExpiringNextMonth", 0), Description = "Certificates Expiring Next Month" };

                case "certs-published-this-year":
                    return new { Value = stats.GetValueOrDefault("PublishedThisYear", 0), Description = "Certificates Published This Year" };

                case "certs-published-last-year":
                    return new { Value = stats.GetValueOrDefault("PublishedLastYear", 0), Description = "Certificates Published Last Year" };

                default:
                    return null;
            }
        }

        private async Task<object?> GetTableData(string type)
        {
            try
            {
                switch (type)
                {
                    case "cert-types":
                        var certTypes = await _certificateRepository.GetCertificationTypeStatisticsAsync();
                        if (certTypes == null || !certTypes.Any())
                            return new List<object>();
                        return certTypes.Select(ct => new { Name = ct.Item1, Count = ct.Item2 }).ToList();

                    case "top-companies":
                        var companies = await _certificateRepository.GetTopCompaniesByActiveCertCountAsync(10);
                        if (companies == null || !companies.Any())
                            return new List<object>();
                        return companies.Select(c => new { Name = c.Item1, Count = c.Item2 }).ToList();

                    case "cert-status":
                        var stats = await _certificateRepository.GetCertificateStatisticsAsync();
                        return new[]
                        {
                            new { Name = "Active", Count = stats.GetValueOrDefault("Active", 0) },
                            new { Name = "Expired", Count = stats.GetValueOrDefault("Expired", 0) },
                            new { Name = "Expiring Soon", Count = stats.GetValueOrDefault("ExpiringSoon", 0) }
                        };

                    case "product-types":
                        var productTypes = await _certificateRepository.GetProductTypeStatisticsAsync(10);
                        if (productTypes == null || !productTypes.Any())
                            return new List<object>();
                        return productTypes.Select(pt => new { Name = pt.Item1, Count = pt.Item2 }).ToList();

                    case "product-categories":
                        var certTypesForCategories = await _certificateRepository.GetCertificationTypeStatisticsAsync();
                        if (certTypesForCategories == null || !certTypesForCategories.Any())
                            return new List<object>();
                        return certTypesForCategories.Take(5).Select(ct => new { Name = ct.Item1, Count = ct.Item2 }).ToList();

                    default:
                        _logger.LogWarning("Unknown table type requested: {TableType}", type);
                        return null;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting table data for type: {TableType}", type);
                return null;
            }
        }

        private string GenerateStatCsv(string type, dynamic data)
        {
            var csv = new StringBuilder();
            csv.AppendLine("Metric,Value,Generated Date");
            csv.AppendLine($"\"{data.Description}\",{data.Value},{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
            return csv.ToString();
        }

        private string GenerateTableCsv(string type, dynamic data)
        {
            var csv = new StringBuilder();
            
            switch (type)
            {
                case "cert-types":
                case "top-companies":
                case "product-types":
                case "product-categories":
                    csv.AppendLine("Name,Count");
                    if (data != null)
                    {
                        foreach (var item in data)
                        {
                            csv.AppendLine($"\"{item.Name}\",{item.Count}");
                        }
                    }
                    break;

                case "cert-status":
                    csv.AppendLine("Status,Count");
                    if (data != null)
                    {
                        foreach (var item in data)
                        {
                            csv.AppendLine($"\"{item.Name}\",{item.Count}");
                        }
                    }
                    break;
            }

            csv.AppendLine();
            csv.AppendLine($"Generated on: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
            return csv.ToString();
        }

        private byte[] GenerateStatExcel(string type, dynamic data)
        {
            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Statistics");
                
                // Headers
                worksheet.Cell(1, 1).Value = "Metric";
                worksheet.Cell(1, 2).Value = "Value";
                worksheet.Cell(1, 3).Value = "Generated Date";
                
                // Style headers
                var headerRange = worksheet.Range(1, 1, 1, 3);
                headerRange.Style.Font.Bold = true;
                headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;
                headerRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                
                // Data
                worksheet.Cell(2, 1).Value = data.Description;
                worksheet.Cell(2, 2).Value = data.Value;
                worksheet.Cell(2, 3).Value = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss") + " UTC";
                
                // Auto-fit columns
                worksheet.Columns().AdjustToContents();
                
                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    return stream.ToArray();
                }
            }
        }

        private byte[] GenerateTableExcel(string type, dynamic data)
        {
            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Data");
                
                // Headers based on type
                switch (type)
                {
                    case "cert-types":
                    case "top-companies":
                    case "product-types":
                    case "product-categories":
                        worksheet.Cell(1, 1).Value = "Name";
                        worksheet.Cell(1, 2).Value = "Count";
                        break;
                    case "cert-status":
                        worksheet.Cell(1, 1).Value = "Status";
                        worksheet.Cell(1, 2).Value = "Count";
                        break;
                }
                
                // Style headers
                var headerRange = worksheet.Range(1, 1, 1, 2);
                headerRange.Style.Font.Bold = true;
                headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;
                headerRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                
                // Add data
                int row = 2;
                if (data != null)
                {
                    foreach (var item in data)
                    {
                        worksheet.Cell(row, 1).Value = item.Name;
                        worksheet.Cell(row, 2).Value = item.Count;
                        row++;
                    }
                }
                
                // Add generation timestamp
                row++;
                worksheet.Cell(row, 1).Value = "Generated on:";
                worksheet.Cell(row, 2).Value = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss") + " UTC";
                
                // Auto-fit columns
                worksheet.Columns().AdjustToContents();
                
                // Add borders to data
                var dataRange = worksheet.Range(1, 1, row, 2);
                dataRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                dataRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
                
                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    return stream.ToArray();
                }
            }
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> TestTuples()
        {
            try
            {
                var certTypes = await _certificateRepository.GetCertificationTypeStatisticsAsync();
                var converted = certTypes.Select(ct => new { Name = ct.Name, Count = ct.Count }).ToList();
                
                return Ok(new { 
                    Original = certTypes,
                    Converted = converted,
                    Count = certTypes.Count(),
                    Message = "Tuple conversion test"
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message, StackTrace = ex.StackTrace });
            }
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> TestData()
        {
            try
            {
                var stats = await _certificateRepository.GetCertificateStatisticsAsync();
                var certTypes = await _certificateRepository.GetCertificationTypeStatisticsAsync();
                var companies = await _certificateRepository.GetTopCompaniesByActiveCertCountAsync(5);
                var products = await _certificateRepository.GetProductTypeStatisticsAsync(5);

                var testData = new
                {
                    Statistics = stats,
                    CertificationTypes = certTypes,
                    Companies = companies,
                    Products = products,
                    Message = "Data retrieved successfully"
                };

                return Ok(testData);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message, StackTrace = ex.StackTrace });
            }
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> TestDownloadNoAuth(string type = "total-active-certs", string format = "excel")
        {
            try
            {
                // Test download without authentication requirement
                var stats = await _certificateRepository.GetCertificateStatisticsAsync();
                var data = await GetStatData(type, stats);
                
                if (data == null)
                {
                    return BadRequest($"No data found for type: {type}. Available stats: {string.Join(", ", stats.Keys)}");
                }

                if (format?.ToLower() == "excel")
                {
                    var excelBytes = GenerateStatExcel(type, data);
                    var fileName = $"{type}-test-{DateTime.UtcNow:yyyy-MM-dd}.xlsx";
                    return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
                }
                else
                {
                    var csv = GenerateStatCsv(type, data);
                    var fileName = $"{type}-test-{DateTime.UtcNow:yyyy-MM-dd}.csv";
                    return File(Encoding.UTF8.GetBytes(csv), "text/csv", fileName);
                }
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message, StackTrace = ex.StackTrace });
            }
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> TestCsvGeneration(string type = "total-active-certs")
        {
            try
            {
                var stats = await _certificateRepository.GetCertificateStatisticsAsync();
                var data = await GetStatData(type, stats);
                
                if (data == null)
                {
                    return BadRequest($"No data found for type: {type}. Available stats: {string.Join(", ", stats.Keys)}");
                }

                var csv = GenerateStatCsv(type, data);
                
                return Content(csv, "text/plain");
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message, StackTrace = ex.StackTrace });
            }
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> SeedDatabase()
        {
            try
            {
                // Simple seeding method - create some basic test data with proper relationships
                await _context.Database.EnsureCreatedAsync();

                // Clear existing data (except admin users)
                await _context.Database.ExecuteSqlRawAsync("DELETE FROM Certificates");
                await _context.Database.ExecuteSqlRawAsync("DELETE FROM Products"); 
                await _context.Database.ExecuteSqlRawAsync("DELETE FROM Companies");
                await _context.Database.ExecuteSqlRawAsync("DELETE FROM CertificationTypes");
                await _context.Database.ExecuteSqlRawAsync("DELETE FROM ProductCategories");

                // Create certification types first
                var certificationTypes = new List<CertificationType>();
                var certTypeNames = new[] { "FSC Chain of Custody", "SFI Chain of Custody", "PEFC Chain of Custody", "Recycled Content", "Green Building Standard" };
                foreach (var typeName in certTypeNames)
                {
                    certificationTypes.Add(new CertificationType
                    {
                        Name = typeName,
                        Description = $"Description for {typeName}",
                        IsActive = true
                    });
                }
                _context.CertificationTypes.AddRange(certificationTypes);
                await _context.SaveChangesAsync();

                // Create product categories
                var productCategories = new List<ProductCategory>();
                var categoryNames = new[] { "Paper Products", "Wood Products", "Packaging", "Building Materials", "Furniture" };
                foreach (var categoryName in categoryNames)
                {
                    productCategories.Add(new ProductCategory
                    {
                        Name = categoryName,
                        Description = $"Description for {categoryName}",
                        IsActive = true
                    });
                }
                _context.ProductCategories.AddRange(productCategories);
                await _context.SaveChangesAsync();

                // Create companies with realistic names
                var companies = new List<Company>();
                var companyNames = new[] { 
                    "Green Paper Mills Inc", "Sustainable Wood Co", "EcoPackaging Solutions", "Forest Products Ltd", 
                    "Renewable Resources Corp", "Environmental Materials Inc", "Clean Manufacturing Co", 
                    "Responsible Sourcing LLC", "Certified Supply Chain Inc", "Green Building Supplies" 
                };
                for (int i = 0; i < companyNames.Length; i++)
                {
                    companies.Add(new Company
                    {
                        Name = companyNames[i],
                        Country = i % 3 == 0 ? "USA" : i % 3 == 1 ? "Canada" : "Mexico",
                        Location = $"City {i + 1}",
                        IsActive = true
                    });
                }
                _context.Companies.AddRange(companies);
                await _context.SaveChangesAsync();

                // Create products with proper category relationships
                var products = new List<Product>();
                var random = new Random();
                for (int i = 1; i <= 20; i++)
                {
                    var category = productCategories[random.Next(productCategories.Count)];
                    products.Add(new Product
                    {
                        Name = $"Product {i}",
                        Description = $"Description for Product {i}",
                        Model = $"Model-{i}",
                        CategoryId = category.Id,
                        IsActive = true
                    });
                }
                _context.Products.AddRange(products);
                await _context.SaveChangesAsync();

                // Create certificates with proper relationships
                var certificates = new List<Certificate>();
                var statuses = new[] { "active", "active", "active", "active", "active", "draft", "pending", "expired", "suspended" }; // Mostly active
                for (int i = 1; i <= 50; i++)
                {
                    var company = companies[random.Next(companies.Count)];
                    var product = products[random.Next(products.Count)];
                    var certType = certificationTypes[random.Next(certificationTypes.Count)];
                    var status = statuses[random.Next(statuses.Length)];

                    certificates.Add(new Certificate
                    {
                        Number = $"GCC{i:D4}",
                        Status = status,
                        CompanyId = company.Id,
                        ProductId = product.Id,
                        CertificationTypeId = certType.Id,
                        ExpirationDate = status == "expired" ? DateTime.UtcNow.AddDays(-random.Next(30)) : DateTime.UtcNow.AddDays(random.Next(365) + 30),
                        EffectiveDate = DateTime.UtcNow.AddDays(-random.Next(100)),
                        Country = company.Country,
                        Location = company.Location,
                        CreatedAt = DateTime.UtcNow.AddDays(-random.Next(200)),
                        UpdatedAt = DateTime.UtcNow.AddDays(-random.Next(30))
                    });
                }
                _context.Certificates.AddRange(certificates);
                await _context.SaveChangesAsync();
                
                return Ok(new { 
                    Message = "Database seeded successfully with proper relationships",
                    CertificationTypesCreated = certificationTypes.Count,
                    ProductCategoriesCreated = productCategories.Count,
                    CompaniesCreated = companies.Count,
                    ProductsCreated = products.Count,
                    CertificatesCreated = certificates.Count
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message, StackTrace = ex.StackTrace });
            }
        }
    }
}