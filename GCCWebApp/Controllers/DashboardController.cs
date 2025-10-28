using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using GCCWebApp.Models;
using GCCWebApp.Repositories;
using GCCWebApp.Services.Performance;
using GCCWebApp.Services.Security;
using System.Text;
using System.Text.Json;

namespace GCCWebApp.Controllers
{
    [Authorize(Policy = "AnalystOrAdmin")]
    public class DashboardController : Controller
    {
        private readonly ILogger<DashboardController> _logger;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICertificateRepository _certificateRepository;
        private readonly IPerformanceMonitoringService _performanceMonitoring;
        private readonly IStructuredLoggingService _structuredLogger;

        public DashboardController(
            ILogger<DashboardController> logger,
            IUnitOfWork unitOfWork,
            ICertificateRepository certificateRepository,
            IPerformanceMonitoringService performanceMonitoring,
            IStructuredLoggingService structuredLogger)
        {
            _logger = logger;
            _unitOfWork = unitOfWork;
            _certificateRepository = certificateRepository;
            _performanceMonitoring = performanceMonitoring;
            _structuredLogger = structuredLogger;
        }

        public async Task<IActionResult> Index()
        {
            return await _performanceMonitoring.MonitorAsync("Dashboard.Index", async () =>
            {
                try
                {
                    var userRole = HttpContext.Session.GetString("UserRole") ?? "Public";
                    
                    await _structuredLogger.LogUserActionAsync("Dashboard.Index", User, new { UserRole = userRole });
                    
                    // Create dashboard based on user role
                    var model = new DashboardViewModel
                    {
                        UserRole = userRole,
                        UserName = HttpContext.Session.GetString("UserName") ?? "User",
                        UserEmail = HttpContext.Session.GetString("UserEmail") ?? "",
                        ErrorMessage = ""
                    };

                    if (userRole == "Admin" || userRole == "Analyst")
                    {
                        await LoadAdminDashboardData(model);
                    }
                    else
                    {
                        await LoadPublicDashboardData(model);
                    }

                    return View(model);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error loading dashboard");
                    await _structuredLogger.LogExceptionAsync(ex, "Dashboard.Index", User);
                    
                    return View(new DashboardViewModel 
                    { 
                        UserRole = "Public", 
                        UserName = "User", 
                        UserEmail = "",
                        HasError = true,
                        ErrorMessage = "Unable to load dashboard data"
                    });
                }
            });
        }

        private async Task LoadAdminDashboardData(DashboardViewModel model)
        {
            await _performanceMonitoring.MonitorAsync("Dashboard.LoadAdminData", async () =>
            {
                // Load comprehensive statistics for internal users - sequential to avoid DbContext concurrency issues
                model.Statistics = await _certificateRepository.GetCertificateStatisticsAsync();
                var recentCerts = await _certificateRepository.GetPagedCertificatesAsync(1, 5);
                model.RecentCertificates = recentCerts.certificates;
                model.ExpiringCertificates = await _certificateRepository.GetCertificatesExpiringInDaysAsync(30);
                model.TotalCompanies = await _unitOfWork.Companies.CountAsync();
                model.TotalProducts = await _unitOfWork.Products.CountAsync();
                model.TotalCertificationTypes = await _unitOfWork.CertificationTypes.CountAsync();
                model.CertificationTypeBreakdown = await _certificateRepository.GetCertificationTypeStatisticsAsync();
                model.TopCompaniesBreakdown = await _certificateRepository.GetTopCompaniesByActiveCertCountAsync(10);
                model.ProductTypeBreakdown = await _certificateRepository.GetProductTypeStatisticsAsync(10);
            });
        }

        private async Task LoadPublicDashboardData(DashboardViewModel model)
        {
            await _performanceMonitoring.MonitorAsync("Dashboard.LoadPublicData", async () =>
            {
                // Limited view for external users
                var activeCerts = await _certificateRepository.GetActiveCertificatesAsync();
                model.TotalActiveCertificates = activeCerts.Count();
                model.RecentCertificates = activeCerts.Take(5);
            });
        }

        [HttpGet]
        public async Task<IActionResult> GetChartData(string chartType = "monthly")
        {
            try
            {
                var userRole = HttpContext.Session.GetString("UserRole") ?? "Public";
                
                if (userRole != "Admin" && userRole != "Analyst")
                {
                    return Forbid();
                }

                switch (chartType.ToLower())
                {
                    case "monthly":
                        var monthlyData = await GetMonthlyCertificateData();
                        return Json(monthlyData);
                    
                    case "status":
                        var statusData = await GetCertificateStatusData();
                        return Json(statusData);
                    
                    case "company":
                        var companyData = await GetTopCompaniesData();
                        return Json(companyData);
                    
                    case "expiration":
                        var expirationData = await GetExpirationData();
                        return Json(expirationData);
                    
                    default:
                        return BadRequest("Invalid chart type");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting chart data for type: {ChartType}", chartType);
                return BadRequest("Error retrieving chart data");
            }
        }

        private async Task<object> GetMonthlyCertificateData()
        {
            var endDate = DateTime.UtcNow;
            var startDate = endDate.AddMonths(-11); // Last 12 months
            
            var certificates = await _certificateRepository.GetCertificatesByDateRangeAsync(startDate, endDate);
            
            var monthlyGroups = certificates
                .GroupBy(c => new { c.CreatedAt.Year, c.CreatedAt.Month })
                .Select(g => new
                {
                    Month = $"{g.Key.Year}-{g.Key.Month:D2}",
                    Count = g.Count()
                })
                .OrderBy(x => x.Month)
                .ToList();

            return new
            {
                labels = monthlyGroups.Select(x => x.Month).ToArray(),
                datasets = new[]
                {
                    new
                    {
                        label = "Certificates Created",
                        data = monthlyGroups.Select(x => x.Count).ToArray(),
                        backgroundColor = "rgba(54, 162, 235, 0.6)",
                        borderColor = "rgba(54, 162, 235, 1)",
                        borderWidth = 1
                    }
                }
            };
        }

        private async Task<object> GetCertificateStatusData()
        {
            var stats = await _certificateRepository.GetCertificateStatisticsAsync();
            
            return new
            {
                labels = new[] { "Active", "Expired", "Expiring Soon" },
                datasets = new[]
                {
                    new
                    {
                        data = new[] 
                        { 
                            stats.GetValueOrDefault("Active", 0), 
                            stats.GetValueOrDefault("Expired", 0), 
                            stats.GetValueOrDefault("ExpiringSoon", 0) 
                        },
                        backgroundColor = new[] 
                        { 
                            "rgba(75, 192, 192, 0.6)", 
                            "rgba(255, 99, 132, 0.6)", 
                            "rgba(255, 205, 86, 0.6)" 
                        }
                    }
                }
            };
        }

        private async Task<object> GetTopCompaniesData()
        {
            var companies = await _unitOfWork.Companies.GetAllAsync();
            var companyCertCounts = new List<(string Name, int Count)>();
            
            foreach (var company in companies.Take(10))
            {
                var certs = await _certificateRepository.GetCertificatesByCompanyAsync(company.Id);
                companyCertCounts.Add((company.Name, certs.Count()));
            }
            
            var topCompanies = companyCertCounts
                .OrderByDescending(x => x.Count)
                .Take(5)
                .ToList();

            return new
            {
                labels = topCompanies.Select(x => x.Name).ToArray(),
                datasets = new[]
                {
                    new
                    {
                        label = "Certificates",
                        data = topCompanies.Select(x => x.Count).ToArray(),
                        backgroundColor = "rgba(153, 102, 255, 0.6)",
                        borderColor = "rgba(153, 102, 255, 1)",
                        borderWidth = 1
                    }
                }
            };
        }

        private async Task<object> GetExpirationData()
        {
            var now = DateTime.UtcNow;
            var next30Days = await _certificateRepository.GetCertificatesExpiringInDaysAsync(30);
            var next60Days = await _certificateRepository.GetCertificatesExpiringInDaysAsync(60);
            var next90Days = await _certificateRepository.GetCertificatesExpiringInDaysAsync(90);

            return new
            {
                labels = new[] { "Next 30 Days", "Next 60 Days", "Next 90 Days" },
                datasets = new[]
                {
                    new
                    {
                        label = "Expiring Certificates",
                        data = new[] { next30Days.Count(), next60Days.Count(), next90Days.Count() },
                        backgroundColor = new[] 
                        { 
                            "rgba(255, 99, 132, 0.6)", 
                            "rgba(255, 159, 64, 0.6)", 
                            "rgba(255, 205, 86, 0.6)" 
                        }
                    }
                }
            };
        }

        [HttpGet]
        public async Task<IActionResult> DownloadStat(string type)
        {
            try
            {
                var userRole = HttpContext.Session.GetString("UserRole") ?? "Public";

                if (userRole != "Admin" && userRole != "Analyst")
                {
                    return Forbid();
                }

                var stats = await _certificateRepository.GetCertificateStatisticsAsync();
                var data = await GetStatData(type, stats);
                
                if (data == null)
                {
                    return BadRequest("Invalid stat type");
                }

                var csv = GenerateStatCsv(type, data);
                var fileName = $"{type}-report-{DateTime.UtcNow:yyyy-MM-dd}.csv";
                
                return File(Encoding.UTF8.GetBytes(csv), "text/csv", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading stat report for type: {StatType}", type);
                return BadRequest("Error generating report");
            }
        }

        [HttpGet]
        public async Task<IActionResult> DownloadTable(string type)
        {
            try
            {
                var userRole = HttpContext.Session.GetString("UserRole") ?? "Public";

                if (userRole != "Admin" && userRole != "Analyst")
                {
                    return Forbid();
                }

                var data = await GetTableData(type);
                
                if (data == null)
                {
                    return BadRequest("Invalid table type");
                }

                var csv = GenerateTableCsv(type, data);
                var fileName = $"{type}-table-{DateTime.UtcNow:yyyy-MM-dd}.csv";
                
                return File(Encoding.UTF8.GetBytes(csv), "text/csv", fileName);
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
            // Get all active certificates - we'll filter in memory based on table type
            var allCerts = await _certificateRepository.GetActiveCertificatesAsync();
            var certsList = allCerts.ToList();

            switch (type)
            {
                case "cert-types":
                    // Return all active certificates (they'll be grouped by cert type in the export)
                    return certsList;

                case "top-companies":
                    // Get top 10 companies and filter certificates
                    var topCompaniesStats = await _certificateRepository.GetTopCompaniesByActiveCertCountAsync(10);
                    var topCompanyIds = topCompaniesStats.Select(c => c.Name).ToList();
                    return certsList.Where(c => c.Company != null && topCompanyIds.Contains(c.Company.Name)).ToList();

                case "cert-status":
                    // Return all active certificates (status shown in CSV)
                    return certsList;

                case "product-types":
                    // Get top 10 product types and filter certificates
                    var productTypeStats = await _certificateRepository.GetProductTypeStatisticsAsync(10);
                    var topProductNames = productTypeStats.Select(p => p.Name).ToList();
                    return certsList.Where(c => c.Product != null && topProductNames.Contains(c.Product.Name)).ToList();

                case "product-categories":
                    // Get top 5 certification types and filter certificates
                    var certTypeStats = await _certificateRepository.GetCertificationTypeStatisticsAsync();
                    var top5CertTypes = certTypeStats.Take(5).Select(ct => ct.Name).ToList();
                    return certsList.Where(c => c.CertificationType != null && top5CertTypes.Contains(c.CertificationType.Name)).ToList();

                default:
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

            // Header row with full certificate details
            csv.AppendLine("Certificate Number,Company Name,Product Name,Certification Type,Effective Date,Expiration Date,Status,Certificate PDF URL");

            if (data != null)
            {
                IEnumerable<Certificate> certificates = data;

                foreach (var cert in certificates)
                {
                    var certificateNumber = EscapeCsvValue(cert.Number ?? "N/A");
                    var companyName = EscapeCsvValue(cert.Company?.Name ?? "N/A");
                    var productName = EscapeCsvValue(cert.Product?.Name ?? "N/A");
                    var certificationType = EscapeCsvValue(cert.CertificationType?.Name ?? "N/A");
                    var effectiveDate = cert.EffectiveDate.ToString("yyyy-MM-dd");
                    var expirationDate = cert.ExpirationDate.ToString("yyyy-MM-dd");
                    var status = EscapeCsvValue(cert.Status ?? "N/A");
                    var pdfUrl = !string.IsNullOrEmpty(cert.CertificateFile) ? $"/uploads/certificates/{cert.CertificateFile}" : "N/A";

                    csv.AppendLine($"{certificateNumber},{companyName},{productName},{certificationType},{effectiveDate},{expirationDate},{status},{pdfUrl}");
                }
            }

            return csv.ToString();
        }

        private string EscapeCsvValue(string value)
        {
            if (string.IsNullOrEmpty(value))
                return "\"\"";

            // Escape quotes and wrap in quotes if contains comma, quote, or newline
            if (value.Contains(",") || value.Contains("\"") || value.Contains("\n") || value.Contains("\r"))
            {
                return $"\"{value.Replace("\"", "\"\"")}\"";
            }

            return $"\"{value}\"";
        }
    }
}