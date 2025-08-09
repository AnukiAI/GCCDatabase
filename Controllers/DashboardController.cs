using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using GCCWebApp.Models;
using GCCWebApp.Repositories;

namespace GCCWebApp.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly ILogger<DashboardController> _logger;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICertificateRepository _certificateRepository;

        public DashboardController(
            ILogger<DashboardController> logger,
            IUnitOfWork unitOfWork,
            ICertificateRepository certificateRepository)
        {
            _logger = logger;
            _unitOfWork = unitOfWork;
            _certificateRepository = certificateRepository;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                var userRole = HttpContext.Session.GetString("UserRole") ?? "Public";
                
                // Create dashboard based on user role
                var model = new DashboardViewModel
                {
                    UserRole = userRole,
                    UserName = HttpContext.Session.GetString("UserName") ?? "User",
                    UserEmail = HttpContext.Session.GetString("UserEmail") ?? ""
                };

                if (userRole == "Admin" || userRole == "Analyst")
                {
                    // Load comprehensive statistics for internal users
                    var stats = await _certificateRepository.GetCertificateStatisticsAsync();
                    model.Statistics = stats;
                    
                    // Recent certificates
                    var recentCerts = await _certificateRepository.GetPagedCertificatesAsync(1, 5);
                    model.RecentCertificates = recentCerts.certificates;
                    
                    // Expiring certificates
                    model.ExpiringCertificates = await _certificateRepository.GetCertificatesExpiringInDaysAsync(30);
                    
                    // Company and product counts
                    model.TotalCompanies = await _unitOfWork.Companies.CountAsync();
                    model.TotalProducts = await _unitOfWork.Products.CountAsync();
                    model.TotalCertificationTypes = await _unitOfWork.CertificationTypes.CountAsync();
                }
                else
                {
                    // Limited view for external users
                    var activeCerts = await _certificateRepository.GetActiveCertificatesAsync();
                    model.TotalActiveCertificates = activeCerts.Count();
                    model.RecentCertificates = activeCerts.Take(5);
                }

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading dashboard");
                return View(new DashboardViewModel 
                { 
                    UserRole = "Public", 
                    UserName = "User", 
                    UserEmail = "",
                    HasError = true,
                    ErrorMessage = "Unable to load dashboard data"
                });
            }
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
    }
}