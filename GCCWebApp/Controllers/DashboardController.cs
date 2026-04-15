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

                var (certificates, reportTitle) = await GetCertificatesForStat(type);

                if (reportTitle == null)
                {
                    return BadRequest("Invalid stat type");
                }

                var csv = GenerateCertificateCsv(reportTitle, certificates);
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

                var (certificates, reportTitle) = await GetCertificatesForTable(type);

                if (reportTitle == null)
                {
                    return BadRequest("Invalid table type");
                }

                var csv = GenerateCertificateCsv(reportTitle, certificates);
                var fileName = $"{type}-table-{DateTime.UtcNow:yyyy-MM-dd}.csv";

                return File(Encoding.UTF8.GetBytes(csv), "text/csv", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading table report for type: {TableType}", type);
                return BadRequest("Error generating report");
            }
        }

        private async Task<(IEnumerable<Certificate> certificates, string? reportTitle)> GetCertificatesForStat(string type)
        {
            var today = CertificateStatusHelper.BusinessToday;
            var startOfMonth = new DateTime(today.Year, today.Month, 1);
            var startOfLastMonth = startOfMonth.AddMonths(-1);
            var endOfLastMonth = startOfMonth.AddDays(-1);
            var startOfNextMonth = startOfMonth.AddMonths(1);
            var endOfNextMonth = startOfNextMonth.AddMonths(1).AddDays(-1);
            var startOfYear = new DateTime(today.Year, 1, 1);
            var startOfLastYear = new DateTime(today.Year - 1, 1, 1);
            var endOfLastYear = new DateTime(today.Year - 1, 12, 31);

            switch (type)
            {
                case "total-active-certs":
                    return (await _certificateRepository.GetActiveCertificatesAsync(), "Total Active Certificates");

                case "companies-with-active-certs":
                    return (await _certificateRepository.GetActiveCertificatesAsync(), "Companies with Active Certificates");

                case "product-categories-with-active-certs":
                    return (await _certificateRepository.GetActiveCertificatesAsync(), "Product Categories with Active Certificates");

                case "product-types-with-active-certs":
                    return (await _certificateRepository.GetActiveCertificatesAsync(), "Product Types with Active Certificates");

                case "product-subcategories-with-active-certs":
                    return (await _certificateRepository.GetActiveCertificatesAsync(), "Product Subcategories with Active Certificates");

                case "products-with-active-certs":
                    return (await _certificateRepository.GetActiveCertificatesAsync(), "Products with Active Certificates");

                case "certs-published-last-month":
                    return (await _certificateRepository.GetCertificatesByDateRangeAsync(startOfLastMonth, endOfLastMonth),
                        "Certificates Published Last Month");

                case "certs-expiring-this-month":
                    return (await _certificateRepository.GetExpiringCertificatesAsync(startOfNextMonth),
                        "Certificates Expiring This Month");

                case "certs-expired-last-month":
                    return (await _certificateRepository.GetCertificatesByDateRangeAsync(startOfLastMonth, endOfLastMonth),
                        "Certificates Expired Last Month");

                case "certs-expiring-next-month":
                    var expiringNext = await _certificateRepository.GetExpiringCertificatesAsync(endOfNextMonth);
                    var filtered = expiringNext.Where(c => c.ExpirationDate >= startOfNextMonth);
                    return (filtered, "Certificates Expiring Next Month");

                case "certs-published-this-year":
                    return (await _certificateRepository.GetCertificatesByDateRangeAsync(startOfYear, today),
                        "Certificates Published This Year");

                case "certs-published-last-year":
                    return (await _certificateRepository.GetCertificatesByDateRangeAsync(startOfLastYear, endOfLastYear),
                        "Certificates Published Last Year");

                default:
                    return (Enumerable.Empty<Certificate>(), null);
            }
        }

        private async Task<(IEnumerable<Certificate> certificates, string? reportTitle)> GetCertificatesForTable(string type)
        {
            switch (type)
            {
                case "cert-types":
                    return (await _certificateRepository.GetActiveCertificatesAsync(),
                        "Total Active Certs by Certification Type");

                case "top-companies":
                    return (await _certificateRepository.GetActiveCertificatesAsync(),
                        "Top 10 Companies by Active Cert Count");

                case "cert-status":
                    // All certificates with navigation properties
                    var allStatuses = new[] {
                        CertificateStatusHelper.StatusDraft,
                        CertificateStatusHelper.StatusClientApprovedNotPaid,
                        CertificateStatusHelper.StatusClientApprovedConfidential,
                        CertificateStatusHelper.StatusClientApprovedPaid,
                        CertificateStatusHelper.StatusHold,
                        CertificateStatusHelper.StatusExpired,
                        CertificateStatusHelper.StatusRepublished,
                        CertificateStatusHelper.StatusExpiredNoAnnualVerification
                    };
                    var statusCerts = new List<Certificate>();
                    foreach (var status in allStatuses)
                    {
                        var byCerts = await _certificateRepository.GetCertificatesByStatusAsync(status);
                        statusCerts.AddRange(byCerts);
                    }
                    return (statusCerts, "Certificates by Status");

                case "product-types":
                    return (await _certificateRepository.GetActiveCertificatesAsync(),
                        "Top 10 Product Types by Active Cert Count");

                case "product-categories":
                    return (await _certificateRepository.GetActiveCertificatesAsync(),
                        "Top 5 Product Categories by Active Cert Count");

                default:
                    return (Enumerable.Empty<Certificate>(), null);
            }
        }

        private string GenerateCertificateCsv(string reportTitle, IEnumerable<Certificate> certificates)
        {
            var certList = certificates.ToList();
            var csv = new StringBuilder();
            csv.AppendLine($"Report: {reportTitle}");
            csv.AppendLine($"Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
            csv.AppendLine($"Total Certificates: {certList.Count}");
            csv.AppendLine();
            csv.AppendLine("Certificate Number,Status,Company,Product,Certification Type,Effective Date,Expiration Date,Location,Country");

            foreach (var cert in certList)
            {
                csv.AppendLine($"{EscapeCsvValue(cert.Number)},{EscapeCsvValue(cert.Status)},{EscapeCsvValue(cert.Company?.Name ?? "")},{EscapeCsvValue(cert.Product?.Name ?? "")},{EscapeCsvValue(cert.CertificationType?.Name ?? "")},{cert.EffectiveDate:yyyy-MM-dd},{cert.ExpirationDate:yyyy-MM-dd},{EscapeCsvValue(cert.Location ?? "")},{EscapeCsvValue(cert.Country ?? "")}");
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