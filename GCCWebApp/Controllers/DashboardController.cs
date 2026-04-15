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
                // Use filtered counts from statistics (only companies/types/products with active certs)
                model.TotalCompanies = model.Statistics.GetValueOrDefault("CompaniesWithActiveCerts", 0);
                model.TotalProducts = model.Statistics.GetValueOrDefault("ActiveProducts", 0);
                model.TotalCertificationTypes = model.Statistics.GetValueOrDefault("CertTypesWithActiveCerts", 0);
                model.CertificationTypeBreakdown = await _certificateRepository.GetCertificationTypeStatisticsAsync();
                model.TopCompaniesBreakdown = await _certificateRepository.GetTopCompaniesByActiveCertCountAsync(10);
                model.ProductTypeBreakdown = await _certificateRepository.GetProductTypeStatisticsAsync(10);
                model.ProductCategoryBreakdown = await _certificateRepository.GetProductCategoryStatisticsAsync(5);
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
                labels = new[] { "Active", "Expired" },
                datasets = new[]
                {
                    new
                    {
                        data = new[]
                        {
                            stats.GetValueOrDefault("Active", 0),
                            stats.GetValueOrDefault("Expired", 0)
                        },
                        backgroundColor = new[]
                        {
                            "rgba(75, 192, 192, 0.6)",
                            "rgba(255, 99, 132, 0.6)"
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

                // Product category/type/subcategory stats get detailed breakdown exports
                var detailCsv = await GenerateDetailedStatCsvIfApplicable(type);
                if (detailCsv != null)
                {
                    var detailFileName = $"{type}-report-{DateTime.UtcNow:yyyy-MM-dd}.csv";
                    return File(Encoding.UTF8.GetBytes(detailCsv), "text/csv", detailFileName);
                }

                var stats = await _certificateRepository.GetCertificateStatisticsAsync();
                var data = GetStatData(type, stats);

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

        private object? GetStatData(string type, Dictionary<string, int> stats)
        {
            switch (type)
            {
                case "total-active-certs":
                    return new { Value = stats.GetValueOrDefault("Active", 0), Description = "Total Active Certificates" };

                case "companies-with-active-certs":
                    return new { Value = stats.GetValueOrDefault("CompaniesWithActiveCerts", 0), Description = "Companies with Active Certificates" };

                case "product-categories-with-active-certs":
                    return new { Value = stats.GetValueOrDefault("CertTypesWithActiveCerts", 0), Description = "Certification Types with Active Certificates" };

                case "product-types-with-active-certs":
                    return new { Value = stats.GetValueOrDefault("ProductTypesWithActiveCerts", 0), Description = "Product Types with Active Certificates" };

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
            switch (type)
            {
                case "cert-types":
                    var certTypes = await _certificateRepository.GetCertificationTypeStatisticsAsync();
                    return certTypes?.Select(ct => new { Name = ct.Name, Count = ct.Count }).ToList();

                case "top-companies":
                    var companies = await _certificateRepository.GetTopCompaniesByActiveCertCountAsync(10);
                    return companies?.Select(c => new { Name = c.Name, Count = c.Count }).ToList();

                case "cert-status":
                    // Return ALL statuses matching dashboard display
                    var stats = await _certificateRepository.GetCertificateStatisticsAsync();
                    var statusOrder = new[] {
                        ("Draft", "Status_Draft"),
                        ("Client Approved - Not Paid", "Status_Client Approved - Not Paid"),
                        ("Client Approved - Confidential", "Status_Client Approved - Confidential"),
                        ("Client Approved - Paid", "Status_Client Approved - Paid"),
                        ("Hold", "Status_Hold"),
                        ("Expired", "Status_Expired"),
                        ("Republished", "Status_Republished"),
                        ("Expired - No Annual Verification", "Status_Expired - No Annual Verification")
                    };
                    return statusOrder.Select(s => new { Name = s.Item1, Count = stats.GetValueOrDefault(s.Item2, 0) }).ToList();

                case "product-types":
                    var productTypes = await _certificateRepository.GetProductTypeStatisticsAsync(10);
                    return productTypes?.Select(pt => new { Name = pt.Name, Count = pt.Count }).ToList();

                case "product-categories":
                    var categories = await _certificateRepository.GetCertificationTypeStatisticsAsync();
                    return categories?.Take(5).Select(ct => new { Name = ct.Name, Count = ct.Count }).ToList();

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

        private async Task<string?> GenerateDetailedStatCsvIfApplicable(string type)
        {
            IEnumerable<(string Name, int Count)>? breakdown = null;
            string header = "";

            switch (type)
            {
                case "product-categories-with-active-certs":
                    breakdown = await _certificateRepository.GetProductCategoryStatisticsAsync(int.MaxValue);
                    header = "Product Category,Active Cert Count";
                    break;
                case "product-types-with-active-certs":
                    breakdown = await _certificateRepository.GetProductTypeStatisticsAsync(int.MaxValue);
                    header = "Product Type,Active Cert Count";
                    break;
                case "product-subcategories-with-active-certs":
                    breakdown = await _certificateRepository.GetProductSubcategoryStatisticsAsync(int.MaxValue);
                    header = "Product Subcategory,Active Cert Count";
                    break;
                default:
                    return null;
            }

            var csv = new StringBuilder();
            csv.AppendLine(header);
            foreach (var item in breakdown)
            {
                csv.AppendLine($"{EscapeCsvValue(item.Name)},{item.Count}");
            }
            csv.AppendLine();
            csv.AppendLine($"Generated,{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
            return csv.ToString();
        }

        private string GenerateTableCsv(string type, dynamic data)
        {
            var csv = new StringBuilder();

            // Determine header based on table type
            switch (type)
            {
                case "cert-types":
                    csv.AppendLine("Certification Type,# of Active Certificates");
                    break;
                case "top-companies":
                    csv.AppendLine("Company,# of Active Certificates");
                    break;
                case "cert-status":
                    csv.AppendLine("Status,# of Certificates");
                    break;
                case "product-types":
                    csv.AppendLine("Product Type,# of Active Certificates");
                    break;
                case "product-categories":
                    csv.AppendLine("Product Category,# of Active Certificates");
                    break;
                default:
                    csv.AppendLine("Name,Count");
                    break;
            }

            if (data != null)
            {
                foreach (var item in data)
                {
                    csv.AppendLine($"{EscapeCsvValue(item.Name)},{item.Count}");
                }
            }

            csv.AppendLine();
            csv.AppendLine($"Generated,{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
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