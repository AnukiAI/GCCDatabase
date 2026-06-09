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
        private readonly IWebHostEnvironment _environment;

        public ExportController(
            ILogger<ExportController> logger,
            IUnitOfWork unitOfWork,
            ICertificateRepository certificateRepository,
            IPerformanceMonitoringService performanceMonitoring,
            IStructuredLoggingService structuredLogger,
            ApplicationDbContext context,
            IWebHostEnvironment environment)
        {
            _logger = logger;
            _unitOfWork = unitOfWork;
            _certificateRepository = certificateRepository;
            _performanceMonitoring = performanceMonitoring;
            _structuredLogger = structuredLogger;
            _context = context;
            _environment = environment;
        }

        [HttpGet]
        [Authorize(Policy = "AnalystOrAdmin")]
        public async Task<IActionResult> DownloadStat(string type, string format = "csv")
        {
            try
            {
                _logger.LogInformation("DownloadStat called - Type: {StatType}, Format: {Format}", type, format);

                // Get authenticated user role
                var userRole = HttpContext.Session.GetString("UserRole");

                // Enforce authentication check
                if (string.IsNullOrEmpty(userRole) || (userRole != "Admin" && userRole != "Analyst"))
                {
                    _logger.LogWarning("Unauthorized access attempt to DownloadStat by role: {UserRole}", userRole ?? "None");
                    return Forbid();
                }

                var stats = await _certificateRepository.GetCertificateStatisticsAsync();
                var data = await GetStatData(type, stats);

                if (data == null)
                {
                    _logger.LogWarning("GetStatData returned null for type: {StatType}", type);
                    return BadRequest("Invalid stat type");
                }

                // Extract certificate count for logging using reflection
                var dataType = data.GetType();
                var certificatesProperty = dataType.GetProperty("Certificates");
                var descriptionProperty = dataType.GetProperty("Description");

                if (certificatesProperty != null && descriptionProperty != null)
                {
                    var certificates = certificatesProperty.GetValue(data) as List<Certificate>;
                    var description = descriptionProperty.GetValue(data) as string;
                    if (certificates != null && description != null)
                    {
                        _logger.LogInformation("Exporting {Count} certificates for stat type '{StatType}' ({Description})",
                            certificates.Count, type, description);
                    }
                }

                if (format?.ToLower() == "excel")
                {
                    var excelBytes = GenerateStatExcel(type, data);
                    var fileName = $"{type}-report-{DateTime.UtcNow:yyyy-MM-dd}.xlsx";
                    _logger.LogInformation("Generated Excel file: {FileName}", fileName);
                    return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
                }
                else
                {
                    var csv = GenerateStatCsv(type, data);
                    var fileName = $"{type}-report-{DateTime.UtcNow:yyyy-MM-dd}.csv";
                    _logger.LogInformation("Generated CSV file: {FileName}", fileName);
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
        [Authorize(Policy = "AnalystOrAdmin")]
        public async Task<IActionResult> DownloadReport(string type, string format = "excel")
        {
            try
            {
                // Get authenticated user role
                var userRole = HttpContext.Session.GetString("UserRole");

                // Enforce authentication check
                if (string.IsNullOrEmpty(userRole) || (userRole != "Admin" && userRole != "Analyst"))
                {
                    _logger.LogWarning("Unauthorized access attempt to DownloadReport by role: {UserRole}", userRole ?? "None");
                    return Forbid();
                }

                List<Certificate> certificates;
                string reportTitle;

                switch (type)
                {
                    case "active-certificates":
                        certificates = await _context.Certificates
                            .Include(c => c.Company)
                            .Include(c => c.Product)
                            .Include(c => c.CertificationType)
                            .Where(c => CertificateStatusHelper.PublicStatuses.Contains(c.Status) && (c.ExpirationDate == null || c.ExpirationDate >= CertificateStatusHelper.BusinessToday))
                            .OrderByDescending(c => c.EffectiveDate)
                            .ToListAsync();
                        reportTitle = "Current Active Certificates";
                        break;

                    case "expiring-30-days":
                        var thirtyDaysFromNow = CertificateStatusHelper.BusinessToday.AddDays(30);
                        certificates = await _context.Certificates
                            .Include(c => c.Company)
                            .Include(c => c.Product)
                            .Include(c => c.CertificationType)
                            .Where(c => CertificateStatusHelper.PublicStatuses.Contains(c.Status) &&
                                       (c.ExpirationDate == null || c.ExpirationDate >= CertificateStatusHelper.BusinessToday) &&
                                       c.ExpirationDate <= thirtyDaysFromNow)
                            .OrderBy(c => c.ExpirationDate)
                            .ToListAsync();
                        reportTitle = "Certificates Expiring in Next 30 Days";
                        break;

                    case "expiring-3-months":
                        var threeMonthsFromNow = CertificateStatusHelper.BusinessToday.AddMonths(3);
                        certificates = await _context.Certificates
                            .Include(c => c.Company)
                            .Include(c => c.Product)
                            .Include(c => c.CertificationType)
                            .Where(c => CertificateStatusHelper.PublicStatuses.Contains(c.Status) &&
                                       (c.ExpirationDate == null || c.ExpirationDate >= CertificateStatusHelper.BusinessToday) &&
                                       c.ExpirationDate <= threeMonthsFromNow)
                            .OrderBy(c => c.ExpirationDate)
                            .ToListAsync();
                        reportTitle = "Certificates Expiring in Next 3 Months";
                        break;

                    default:
                        return BadRequest("Invalid report type");
                }

                if (format?.ToLower() == "excel")
                {
                    var excelBytes = GenerateCertificateReportExcel(reportTitle, certificates);
                    var fileName = $"{type}-{DateTime.UtcNow:yyyy-MM-dd}.xlsx";
                    return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
                }
                else
                {
                    var csv = GenerateCertificateReportCsv(reportTitle, certificates);
                    var fileName = $"{type}-{DateTime.UtcNow:yyyy-MM-dd}.csv";
                    return File(Encoding.UTF8.GetBytes(csv), "text/csv", fileName);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading certificate report for type: {ReportType}", type);
                return BadRequest("Error generating report");
            }
        }

        [HttpGet]
        [Authorize(Policy = "AnalystOrAdmin")]
        public async Task<IActionResult> DownloadTable(string type, string format = "csv")
        {
            try
            {
                // Get authenticated user role
                var userRole = HttpContext.Session.GetString("UserRole");

                // Enforce authentication check
                if (string.IsNullOrEmpty(userRole) || (userRole != "Admin" && userRole != "Analyst"))
                {
                    _logger.LogWarning("Unauthorized access attempt to DownloadTable by role: {UserRole}", userRole ?? "None");
                    return Forbid();
                }

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

        /// <summary>
        /// Aggregate export of all ACTIVE product identifiers across every certificate — the
        /// "all active product identifiers across certs" report from QC #184 / CR-184.
        /// "Active" = parent cert is publicly live (PublicStatuses: Client Approved - Paid or
        /// Republished), which is exactly CR-015's definition of an active product identifier.
        /// The per-certificate export (ProductIdService.ExportToCsvAsync) only covers one cert at a
        /// time; this is the single cross-cert file an analyst needs, with the certificate context
        /// (number, company) plus the product-level fields (name, SKU, partner) that the
        /// parent-Product export omits. CSV only, Analyst+/Admin only.
        /// </summary>
        [HttpGet]
        [Authorize(Policy = "AnalystOrAdmin")]
        public async Task<IActionResult> DownloadActiveProductIds()
        {
            try
            {
                var userRole = HttpContext.Session.GetString("UserRole");
                if (string.IsNullOrEmpty(userRole) || (userRole != "Admin" && userRole != "Analyst"))
                {
                    _logger.LogWarning("Unauthorized access attempt to DownloadActiveProductIds by role: {UserRole}", userRole ?? "None");
                    return Forbid();
                }

                // Active product identifiers only: non-deleted rows whose parent cert is publicly
                // live (PublicStatuses = Paid or Republished) — the client's "all active product
                // identifiers across certs". AsSplitQuery + tie-breaker ordering per the
                // many-Includes guidance (DEF-166/234).
                var rows = await _context.CertificateProductIds
                    .Where(p => p.IsActive && CertificateStatusHelper.PublicStatuses.Contains(p.Certificate.Status))
                    .Include(p => p.Certificate)
                        .ThenInclude(c => c.Company)
                    .Include(p => p.CreatedByUser)
                    .OrderBy(p => p.Certificate.Number)
                    .ThenBy(p => p.ProductIdType)
                    .ThenBy(p => p.Id)
                    .AsSplitQuery()
                    .ToListAsync();

                _logger.LogInformation("DownloadActiveProductIds exporting {Count} active product identifiers across all certificates", rows.Count);

                var csv = new StringBuilder();
                csv.AppendLine("Report: Active Product Identifiers (All Certificates)");
                csv.AppendLine($"Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
                csv.AppendLine($"Total Active Product Identifiers: {rows.Count}");
                csv.AppendLine();
                // No status columns: the export is filtered to active identifiers (parent cert
                // Paid/Republished), so every row is active by construction. Both the derived
                // Identifier Status (CR-015) and the Certificate Status would be non-informative.
                csv.AppendLine("Certificate Number,Company,Identifier Type,Id Value,Partner,Product Name,SKU,Description,Effective Date,Expiration Date,Created Date,Created By");

                foreach (var p in rows)
                {
                    var c = p.Certificate;
                    csv.AppendLine(string.Join(",", new[]
                    {
                        CsvField(c?.Number),
                        CsvField(c?.Company?.Name),
                        CsvField(p.ProductIdType),
                        CsvField(p.ProductIdValue),
                        CsvField(p.Partner),
                        CsvField(p.ProductName),
                        CsvField(p.ProductSku),
                        CsvField(p.Description),
                        CsvField(c?.EffectiveDate.ToString("yyyy-MM-dd")),
                        CsvField(c?.ExpirationDate.ToString("yyyy-MM-dd")),
                        CsvField(p.CreatedAt.ToString("yyyy-MM-dd")),
                        CsvField(p.CreatedByUser?.Username)
                    }));
                }

                var fileName = $"active-product-identifiers-{DateTime.UtcNow:yyyy-MM-dd}.csv";
                return File(Encoding.UTF8.GetBytes(csv.ToString()), "text/csv", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating active-product-identifiers export");
                return BadRequest("Error generating report");
            }
        }

        /// <summary>
        /// Wraps a CSV field in quotes and escapes embedded quotes (RFC 4180 doubling),
        /// so values containing commas, quotes, or newlines (e.g. Product Name / Description)
        /// don't break the column layout.
        /// </summary>
        private static string CsvField(string? value)
        {
            return "\"" + (value ?? string.Empty).Replace("\"", "\"\"") + "\"";
        }

        private async Task<object?> GetStatData(string type, Dictionary<string, int> stats)
        {
            _logger.LogInformation("GetStatData called for type: {StatType}", type);

            // Return actual certificate lists instead of just counts
            var now = DateTime.UtcNow;
            var startOfMonth = new DateTime(now.Year, now.Month, 1);
            var endOfMonth = startOfMonth.AddMonths(1).AddDays(-1);
            var startOfLastMonth = startOfMonth.AddMonths(-1);
            var endOfLastMonth = startOfMonth.AddDays(-1);
            var startOfYear = new DateTime(now.Year, 1, 1);
            var startOfLastYear = new DateTime(now.Year - 1, 1, 1);
            var endOfLastYear = new DateTime(now.Year - 1, 12, 31);

            _logger.LogDebug("Date ranges - Now: {Now}, StartOfMonth: {StartOfMonth}, EndOfMonth: {EndOfMonth}, " +
                "StartOfLastMonth: {StartOfLastMonth}, EndOfLastMonth: {EndOfLastMonth}",
                now, startOfMonth, endOfMonth, startOfLastMonth, endOfLastMonth);

            switch (type)
            {
                // DEF-200 (QC #180): all dashboard "Active …" exports now use DashboardStatuses
                // to match the tile counts in CertificateRepository.GetCertificateStatisticsAsync.
                // The previous PublicStatuses filter dropped Confidential certs from the export
                // while the tile included them, so tile and export disagreed whenever any
                // Confidential cert was in scope.
                case "total-active-certs":
                    _logger.LogInformation("Querying total-active-certs: DashboardStatuses, ExpirationDate >= {Now}", now);
                    var activeCerts = await _context.Certificates
                        .Include(c => c.Company)
                        .Include(c => c.Product)
                        .Include(c => c.CertificationType)
                        .Where(c => CertificateStatusHelper.DashboardStatuses.Contains(c.Status) && (c.ExpirationDate == null || c.ExpirationDate >= now))
                        .OrderBy(c => c.Number)
                        .ToListAsync();
                    _logger.LogInformation("total-active-certs query returned {Count} certificates", activeCerts.Count);
                    return new { Certificates = activeCerts, Description = "Total Active Certificates" };

                case "companies-with-active-certs":
                    var companyCerts = await _context.Certificates
                        .Include(c => c.Company)
                        .Include(c => c.Product)
                        .Include(c => c.CertificationType)
                        .Where(c => CertificateStatusHelper.DashboardStatuses.Contains(c.Status) && (c.ExpirationDate == null || c.ExpirationDate >= now) && c.Company != null)
                        .OrderBy(c => c.Company!.Name)
                        .ThenBy(c => c.Number)
                        .ToListAsync();
                    return new { Certificates = companyCerts, Description = "Companies with Active Certificates" };

                case "product-categories-with-active-certs":
                    var categoryCerts = await _context.Certificates
                        .Include(c => c.Company)
                        .Include(c => c.Product)
                        .Include(c => c.CertificationType)
                        .Where(c => CertificateStatusHelper.DashboardStatuses.Contains(c.Status) && (c.ExpirationDate == null || c.ExpirationDate >= now) && c.CertificationType != null)
                        .OrderBy(c => c.CertificationType!.Name)
                        .ThenBy(c => c.Number)
                        .ToListAsync();
                    return new { Certificates = categoryCerts, Description = "Product Categories with Active Certificates" };

                case "product-types-with-active-certs":
                    var productTypeCerts = await _context.Certificates
                        .Include(c => c.Company)
                        .Include(c => c.Product)
                        .Include(c => c.CertificationType)
                        .Where(c => CertificateStatusHelper.DashboardStatuses.Contains(c.Status) && (c.ExpirationDate == null || c.ExpirationDate >= now) && c.Product != null)
                        .OrderBy(c => c.Product!.Name)
                        .ThenBy(c => c.Number)
                        .ToListAsync();
                    return new { Certificates = productTypeCerts, Description = "Product Types with Active Certificates" };

                case "product-subcategories-with-active-certs":
                    var subcategoryCerts = await _context.Certificates
                        .Include(c => c.Company)
                        .Include(c => c.Product)
                        .ThenInclude(p => p!.Category)
                        .Include(c => c.CertificationType)
                        .Where(c => CertificateStatusHelper.DashboardStatuses.Contains(c.Status) && (c.ExpirationDate == null || c.ExpirationDate >= now) && c.Product != null && c.Product.Category != null)
                        .OrderBy(c => c.Product!.Category!.Name)
                        .ThenBy(c => c.Number)
                        .ToListAsync();
                    return new { Certificates = subcategoryCerts, Description = "Product Subcategories with Active Certificates" };

                case "products-with-active-certs":
                    var productCerts = await _context.Certificates
                        .Include(c => c.Company)
                        .Include(c => c.Product)
                        .Include(c => c.CertificationType)
                        .Where(c => CertificateStatusHelper.DashboardStatuses.Contains(c.Status) && (c.ExpirationDate == null || c.ExpirationDate >= now) && c.Product != null)
                        .OrderBy(c => c.Product!.Name)
                        .ThenBy(c => c.Number)
                        .ToListAsync();
                    return new { Certificates = productCerts, Description = "Products with Active Certificates" };

                case "certs-published-last-month":
                    _logger.LogInformation("Querying certs-published-last-month: EffectiveDate between {Start} and {End}",
                        startOfLastMonth, endOfLastMonth);
                    var publishedLastMonth = await _context.Certificates
                        .Include(c => c.Company)
                        .Include(c => c.Product)
                        .Include(c => c.CertificationType)
                        .Where(c => CertificateStatusHelper.DashboardStatuses.Contains(c.Status) &&
                                   c.EffectiveDate >= startOfLastMonth && c.EffectiveDate <= endOfLastMonth)
                        .OrderByDescending(c => c.EffectiveDate)
                        .ThenBy(c => c.Number)
                        .ToListAsync();
                    _logger.LogInformation("certs-published-last-month query returned {Count} certificates", publishedLastMonth.Count);
                    return new { Certificates = publishedLastMonth, Description = "Certificates Published Last Month" };

                case "certs-expiring-this-month":
                    _logger.LogInformation("Querying certs-expiring-this-month: DashboardStatuses, ExpirationDate between {Start} and {End}",
                        startOfMonth, endOfMonth);
                    var expiringThisMonth = await _context.Certificates
                        .Include(c => c.Company)
                        .Include(c => c.Product)
                        .Include(c => c.CertificationType)
                        .Where(c => CertificateStatusHelper.DashboardStatuses.Contains(c.Status) && c.ExpirationDate >= startOfMonth && c.ExpirationDate <= endOfMonth)
                        .OrderBy(c => c.ExpirationDate)
                        .ThenBy(c => c.Number)
                        .ToListAsync();
                    _logger.LogInformation("certs-expiring-this-month query returned {Count} certificates", expiringThisMonth.Count);
                    return new { Certificates = expiringThisMonth, Description = "Certificates Expiring This Month" };

                case "certs-expired-last-month":
                    // DEF-200 + DEF-220 (QC #180): mirror the tile's CurrentlyExpiredStatuses
                    // filter (Expired + ExpiredNoAnnualVerification only). EverPublishedStatuses
                    // was too wide and leaked Republished/Paid/Confidential certs into the
                    // expired export, contradicting the card title.
                    var expiredLastMonth = await _context.Certificates
                        .Include(c => c.Company)
                        .Include(c => c.Product)
                        .Include(c => c.CertificationType)
                        .Where(c => CertificateStatusHelper.CurrentlyExpiredStatuses.Contains(c.Status) &&
                                   c.ExpirationDate >= startOfLastMonth && c.ExpirationDate <= endOfLastMonth)
                        .OrderBy(c => c.ExpirationDate)
                        .ThenBy(c => c.Number)
                        .ToListAsync();
                    return new { Certificates = expiredLastMonth, Description = "Certificates Expired Last Month" };

                case "certs-expiring-next-month":
                    var nextMonthStart = startOfMonth.AddMonths(1);
                    var nextMonthEnd = nextMonthStart.AddMonths(1).AddDays(-1);
                    var expiringNextMonth = await _context.Certificates
                        .Include(c => c.Company)
                        .Include(c => c.Product)
                        .Include(c => c.CertificationType)
                        .Where(c => CertificateStatusHelper.DashboardStatuses.Contains(c.Status) && c.ExpirationDate >= nextMonthStart && c.ExpirationDate <= nextMonthEnd)
                        .OrderBy(c => c.ExpirationDate)
                        .ThenBy(c => c.Number)
                        .ToListAsync();
                    return new { Certificates = expiringNextMonth, Description = "Certificates Expiring Next Month" };

                case "certs-published-this-year":
                    var publishedThisYear = await _context.Certificates
                        .Include(c => c.Company)
                        .Include(c => c.Product)
                        .Include(c => c.CertificationType)
                        .Where(c => CertificateStatusHelper.DashboardStatuses.Contains(c.Status) &&
                                   c.EffectiveDate >= startOfYear)
                        .OrderByDescending(c => c.EffectiveDate)
                        .ThenBy(c => c.Number)
                        .ToListAsync();
                    return new { Certificates = publishedThisYear, Description = "Certificates Published This Year" };

                case "certs-published-last-year":
                    var publishedLastYear = await _context.Certificates
                        .Include(c => c.Company)
                        .Include(c => c.Product)
                        .Include(c => c.CertificationType)
                        .Where(c => CertificateStatusHelper.DashboardStatuses.Contains(c.Status) &&
                                   c.EffectiveDate >= startOfLastYear && c.EffectiveDate <= endOfLastYear)
                        .OrderByDescending(c => c.EffectiveDate)
                        .ThenBy(c => c.Number)
                        .ToListAsync();
                    return new { Certificates = publishedLastYear, Description = "Certificates Published Last Year" };

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
                            new { Name = "Expired", Count = stats.GetValueOrDefault("Expired", 0) }
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
            // Extract certificates list from data object
            var certificates = (List<Certificate>)data.Certificates;
            var description = (string)data.Description;

            var csv = new StringBuilder();
            csv.AppendLine($"Report: {description}");
            csv.AppendLine($"Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
            csv.AppendLine($"Total Certificates: {certificates.Count}");
            csv.AppendLine();
            csv.AppendLine("Certificate Number,Status,Company,Product,Certification Type,Effective Date,Expiration Date,Location,Country");

            foreach (var cert in certificates)
            {
                csv.AppendLine($"\"{cert.Number}\",\"{cert.Status}\",\"{cert.Company?.Name}\",\"{cert.Product?.Name}\",\"{cert.CertificationType?.Name}\",{cert.EffectiveDate:yyyy-MM-dd},{cert.ExpirationDate:yyyy-MM-dd},\"{cert.Location}\",\"{cert.Country}\"");
            }

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
            // Extract certificates list from data object
            var certificates = (List<Certificate>)data.Certificates;
            var description = (string)data.Description;

            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Certificates");

                // Title
                worksheet.Cell(1, 1).Value = description;
                worksheet.Cell(1, 1).Style.Font.Bold = true;
                worksheet.Cell(1, 1).Style.Font.FontSize = 14;
                worksheet.Range(1, 1, 1, 9).Merge();

                // Metadata
                worksheet.Cell(2, 1).Value = "Generated:";
                worksheet.Cell(2, 2).Value = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss") + " UTC";
                worksheet.Cell(3, 1).Value = "Total Certificates:";
                worksheet.Cell(3, 2).Value = certificates.Count;

                // Headers
                int headerRow = 5;
                worksheet.Cell(headerRow, 1).Value = "Certificate Number";
                worksheet.Cell(headerRow, 2).Value = "Status";
                worksheet.Cell(headerRow, 3).Value = "Company";
                worksheet.Cell(headerRow, 4).Value = "Product";
                worksheet.Cell(headerRow, 5).Value = "Certification Type";
                worksheet.Cell(headerRow, 6).Value = "Effective Date";
                worksheet.Cell(headerRow, 7).Value = "Expiration Date";
                worksheet.Cell(headerRow, 8).Value = "Location";
                worksheet.Cell(headerRow, 9).Value = "Country";

                // Style headers
                var headerRange = worksheet.Range(headerRow, 1, headerRow, 9);
                headerRange.Style.Font.Bold = true;
                headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#00AEEF");
                headerRange.Style.Font.FontColor = XLColor.White;
                headerRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                headerRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

                // Data
                int row = headerRow + 1;
                foreach (var cert in certificates)
                {
                    worksheet.Cell(row, 1).Value = cert.Number;
                    worksheet.Cell(row, 2).Value = cert.Status;
                    worksheet.Cell(row, 3).Value = cert.Company?.Name ?? "";
                    worksheet.Cell(row, 4).Value = cert.Product?.Name ?? "";
                    worksheet.Cell(row, 5).Value = cert.CertificationType?.Name ?? "";
                    worksheet.Cell(row, 6).Value = cert.EffectiveDate;
                    worksheet.Cell(row, 6).Style.DateFormat.Format = "yyyy-MM-dd";
                    worksheet.Cell(row, 7).Value = cert.ExpirationDate;
                    worksheet.Cell(row, 7).Style.DateFormat.Format = "yyyy-MM-dd";
                    worksheet.Cell(row, 8).Value = cert.Location ?? "";
                    worksheet.Cell(row, 9).Value = cert.Country ?? "";
                    row++;
                }

                // Add borders to data
                if (certificates.Any())
                {
                    var dataRange = worksheet.Range(headerRow, 1, row - 1, 9);
                    dataRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                    dataRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
                }

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

        // SECURITY: Test endpoints removed in production. Use proper authentication for all endpoints.
        // If test endpoints are needed for development, they should be in a separate TestController
        // with environment-specific registration (only in Development mode).

        [HttpPost]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> SeedDatabase()
        {
            try
            {
                // SECURITY: Prevent database seeding in production
                if (_environment.IsProduction())
                {
                    _logger.LogWarning("Database seeding attempted in production environment - DENIED");
                    return StatusCode(403, new {
                        Error = "Database seeding is not allowed in production environment",
                        Message = "Use SQL migration scripts for production data management"
                    });
                }

                // SECURITY: Log seeding attempt
                _logger.LogWarning("Database seeding initiated in {Environment} environment by {User}",
                    _environment.EnvironmentName,
                    User.Identity?.Name ?? "Unknown");

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
                var statuses = new[] { "Client Approved - Paid", "Client Approved - Paid", "Client Approved - Paid", "Client Approved - Paid", "Republished", "Draft", "Client Approved - Not Paid", "Expired", "Hold" };
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

        private string GenerateCertificateReportCsv(string reportTitle, List<Certificate> certificates)
        {
            var csv = new StringBuilder();
            csv.AppendLine($"Report: {reportTitle}");
            csv.AppendLine($"Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
            csv.AppendLine($"Total Certificates: {certificates.Count}");
            csv.AppendLine();
            csv.AppendLine("Certificate Number,Status,Company,Product,Certification Type,Effective Date,Expiration Date,Location,Country");

            foreach (var cert in certificates)
            {
                csv.AppendLine($"\"{cert.Number}\",\"{cert.Status}\",\"{cert.Company?.Name}\",\"{cert.Product?.Name}\",\"{cert.CertificationType?.Name}\",{cert.EffectiveDate:yyyy-MM-dd},{cert.ExpirationDate:yyyy-MM-dd},\"{cert.Location}\",\"{cert.Country}\"");
            }

            return csv.ToString();
        }

        private byte[] GenerateCertificateReportExcel(string reportTitle, List<Certificate> certificates)
        {
            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Certificates");

                // Title
                worksheet.Cell(1, 1).Value = reportTitle;
                worksheet.Cell(1, 1).Style.Font.Bold = true;
                worksheet.Cell(1, 1).Style.Font.FontSize = 14;
                worksheet.Range(1, 1, 1, 9).Merge();

                // Metadata
                worksheet.Cell(2, 1).Value = "Generated:";
                worksheet.Cell(2, 2).Value = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss") + " UTC";
                worksheet.Cell(3, 1).Value = "Total Certificates:";
                worksheet.Cell(3, 2).Value = certificates.Count;

                // Headers
                int headerRow = 5;
                worksheet.Cell(headerRow, 1).Value = "Certificate Number";
                worksheet.Cell(headerRow, 2).Value = "Status";
                worksheet.Cell(headerRow, 3).Value = "Company";
                worksheet.Cell(headerRow, 4).Value = "Product";
                worksheet.Cell(headerRow, 5).Value = "Certification Type";
                worksheet.Cell(headerRow, 6).Value = "Effective Date";
                worksheet.Cell(headerRow, 7).Value = "Expiration Date";
                worksheet.Cell(headerRow, 8).Value = "Location";
                worksheet.Cell(headerRow, 9).Value = "Country";

                // Style headers
                var headerRange = worksheet.Range(headerRow, 1, headerRow, 9);
                headerRange.Style.Font.Bold = true;
                headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#00AEEF");
                headerRange.Style.Font.FontColor = XLColor.White;
                headerRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                headerRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

                // Data
                int row = headerRow + 1;
                foreach (var cert in certificates)
                {
                    worksheet.Cell(row, 1).Value = cert.Number;
                    worksheet.Cell(row, 2).Value = cert.Status;
                    worksheet.Cell(row, 3).Value = cert.Company?.Name ?? "";
                    worksheet.Cell(row, 4).Value = cert.Product?.Name ?? "";
                    worksheet.Cell(row, 5).Value = cert.CertificationType?.Name ?? "";
                    worksheet.Cell(row, 6).Value = cert.EffectiveDate;
                    worksheet.Cell(row, 6).Style.DateFormat.Format = "yyyy-MM-dd";
                    worksheet.Cell(row, 7).Value = cert.ExpirationDate;
                    worksheet.Cell(row, 7).Style.DateFormat.Format = "yyyy-MM-dd";
                    worksheet.Cell(row, 8).Value = cert.Location ?? "";
                    worksheet.Cell(row, 9).Value = cert.Country ?? "";
                    row++;
                }

                // Add borders to data
                if (certificates.Any())
                {
                    var dataRange = worksheet.Range(headerRow, 1, row - 1, 9);
                    dataRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                    dataRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
                }

                // Auto-fit columns
                worksheet.Columns().AdjustToContents();

                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    return stream.ToArray();
                }
            }
        }
    }
}