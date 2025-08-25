using Microsoft.EntityFrameworkCore;
using GCCWebApp.Data;
using GCCWebApp.Models;

namespace GCCWebApp.Services.Performance;

/// <summary>
/// Service for database optimization including index management and query optimization
/// </summary>
public interface IDatabaseOptimizationService
{
    Task OptimizeIndexesAsync();
    Task AnalyzeQueryPerformanceAsync();
    Task<DatabasePerformanceReport> GeneratePerformanceReportAsync();
    Task CleanupOldDataAsync();
    Task UpdateStatisticsAsync();
}

public class DatabaseOptimizationService : IDatabaseOptimizationService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<DatabaseOptimizationService> _logger;

    public DatabaseOptimizationService(ApplicationDbContext context, ILogger<DatabaseOptimizationService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task OptimizeIndexesAsync()
    {
        try
        {
            _logger.LogInformation("Starting database index optimization");

            // Create indexes for common query patterns
            var indexCommands = new[]
            {
                // Certificates table indexes
                "CREATE INDEX IF NOT EXISTS IX_Certificates_Number ON Certificates (Number)",
                "CREATE INDEX IF NOT EXISTS IX_Certificates_CompanyName ON Certificates (CompanyName)",
                "CREATE INDEX IF NOT EXISTS IX_Certificates_ProductName ON Certificates (ProductName)",
                "CREATE INDEX IF NOT EXISTS IX_Certificates_ExpirationDate ON Certificates (ExpirationDate)",
                "CREATE INDEX IF NOT EXISTS IX_Certificates_Status ON Certificates (Status)",
                "CREATE INDEX IF NOT EXISTS IX_Certificates_CreatedDate ON Certificates (CreatedDate)",
                "CREATE INDEX IF NOT EXISTS IX_Certificates_CompanyName_ProductName ON Certificates (CompanyName, ProductName)",
                "CREATE INDEX IF NOT EXISTS IX_Certificates_Status_ExpirationDate ON Certificates (Status, ExpirationDate)",

                // Companies table indexes
                "CREATE INDEX IF NOT EXISTS IX_Companies_Name ON Companies (Name)",
                "CREATE INDEX IF NOT EXISTS IX_Companies_Country ON Companies (Country)",
                "CREATE INDEX IF NOT EXISTS IX_Companies_Website ON Companies (Website)",
                "CREATE INDEX IF NOT EXISTS IX_Companies_CreatedDate ON Companies (CreatedDate)",

                // Products table indexes
                "CREATE INDEX IF NOT EXISTS IX_Products_Name ON Products (Name)",
                "CREATE INDEX IF NOT EXISTS IX_Products_Category ON Products (Category)",
                "CREATE INDEX IF NOT EXISTS IX_Products_CompanyName ON Products (CompanyName)",
                "CREATE INDEX IF NOT EXISTS IX_Products_Category_CompanyName ON Products (Category, CompanyName)",

                // Partners table indexes
                "CREATE INDEX IF NOT EXISTS IX_Partners_Name ON Partners (Name)",
                "CREATE INDEX IF NOT EXISTS IX_Partners_Country ON Partners (Country)",
                "CREATE INDEX IF NOT EXISTS IX_Partners_Type ON Partners (Type)",
                "CREATE INDEX IF NOT EXISTS IX_Partners_IsActive ON Partners (IsActive)",

                // Users table indexes
                "CREATE INDEX IF NOT EXISTS IX_Users_Email ON Users (Email)",
                "CREATE INDEX IF NOT EXISTS IX_Users_Role ON Users (Role)",
                "CREATE INDEX IF NOT EXISTS IX_Users_IsActive ON Users (IsActive)",
                "CREATE INDEX IF NOT EXISTS IX_Users_CreatedDate ON Users (CreatedDate)",

                // ApiKeys table indexes
                "CREATE INDEX IF NOT EXISTS IX_ApiKeys_KeyHash ON ApiKeys (KeyHash)",
                "CREATE INDEX IF NOT EXISTS IX_ApiKeys_UserId ON ApiKeys (UserId)",
                "CREATE INDEX IF NOT EXISTS IX_ApiKeys_IsActive ON ApiKeys (IsActive)",
                "CREATE INDEX IF NOT EXISTS IX_ApiKeys_ExpiresAt ON ApiKeys (ExpiresAt)",

                // Composite indexes for complex queries
                "CREATE INDEX IF NOT EXISTS IX_Certificates_Search ON Certificates (CompanyName, ProductName, Status, ExpirationDate)",
                "CREATE INDEX IF NOT EXISTS IX_Companies_Active ON Companies (Name, Website, Country) WHERE IsActive = 1",
                "CREATE INDEX IF NOT EXISTS IX_Products_Active ON Products (Name, Category, CompanyName) WHERE IsActive = 1"
            };

            foreach (var command in indexCommands)
            {
                try
                {
                    await _context.Database.ExecuteSqlRawAsync(command);
                    _logger.LogDebug("Executed index command: {Command}", command);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to execute index command: {Command}", command);
                }
            }

            _logger.LogInformation("Database index optimization completed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during database index optimization");
            throw;
        }
    }

    public async Task AnalyzeQueryPerformanceAsync()
    {
        try
        {
            _logger.LogInformation("Starting query performance analysis");

            // Enable query plan collection if supported
            if (_context.Database.IsSqlServer())
            {
                await _context.Database.ExecuteSqlRawAsync("SET STATISTICS IO ON");
                await _context.Database.ExecuteSqlRawAsync("SET STATISTICS TIME ON");
            }

            // Test common query patterns and log performance
            var performanceTests = new Dictionary<string, Func<Task<long>>>
            {
                ["Certificate Search by Company"] = async () =>
                {
                    var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                    var result = await _context.Certificates
                        .Include(c => c.Company)
                        .Where(c => c.Company.Name.Contains("Test"))
                        .CountAsync();
                    stopwatch.Stop();
                    return stopwatch.ElapsedMilliseconds;
                },

                ["Certificate Search by Product"] = async () =>
                {
                    var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                    var result = await _context.Certificates
                        .Include(c => c.Product)
                        .Where(c => c.Product.Name.Contains("Test"))
                        .CountAsync();
                    stopwatch.Stop();
                    return stopwatch.ElapsedMilliseconds;
                },

                ["Active Certificates"] = async () =>
                {
                    var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                    var result = await _context.Certificates
                        .Where(c => c.Status == "Active" && c.ExpirationDate > DateTime.UtcNow)
                        .CountAsync();
                    stopwatch.Stop();
                    return stopwatch.ElapsedMilliseconds;
                },

                ["Company Lookup"] = async () =>
                {
                    var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                    var result = await _context.Companies
                        .Where(c => c.Name.StartsWith("A"))
                        .CountAsync();
                    stopwatch.Stop();
                    return stopwatch.ElapsedMilliseconds;
                },

                ["Product Categories"] = async () =>
                {
                    var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                    var result = await _context.Products
                        .GroupBy(p => p.Category)
                        .CountAsync();
                    stopwatch.Stop();
                    return stopwatch.ElapsedMilliseconds;
                }
            };

            foreach (var test in performanceTests)
            {
                try
                {
                    var duration = await test.Value();
                    _logger.LogInformation("Query performance test '{TestName}': {Duration}ms", test.Key, duration);
                    
                    if (duration > 1000) // Log slow queries as warnings
                    {
                        _logger.LogWarning("Slow query detected: '{TestName}' took {Duration}ms", test.Key, duration);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in query performance test: {TestName}", test.Key);
                }
            }

            _logger.LogInformation("Query performance analysis completed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during query performance analysis");
            throw;
        }
    }

    public async Task<DatabasePerformanceReport> GeneratePerformanceReportAsync()
    {
        try
        {
            _logger.LogInformation("Generating database performance report");

            var report = new DatabasePerformanceReport
            {
                GeneratedAt = DateTime.UtcNow,
                DatabaseProvider = _context.Database.ProviderName ?? "Unknown"
            };

            // Table statistics
            report.TableStatistics = new List<TableStatistic>
            {
                new() { TableName = "Certificates", RecordCount = await _context.Certificates.CountAsync() },
                new() { TableName = "Companies", RecordCount = await _context.Companies.CountAsync() },
                new() { TableName = "Products", RecordCount = await _context.Products.CountAsync() },
                // new() { TableName = "Partners", RecordCount = await _context.Partners.CountAsync() }, // Partners table may not exist
                new() { TableName = "Users", RecordCount = await _context.Users.CountAsync() },
                new() { TableName = "ApiKeys", RecordCount = await _context.ApiKeys.CountAsync() }
            };

            // Index information (SQLite specific)
            if (_context.Database.IsSqlite())
            {
                var indexQuery = @"
                    SELECT name as IndexName, tbl_name as TableName 
                    FROM sqlite_master 
                    WHERE type = 'index' AND name NOT LIKE 'sqlite_%'
                    ORDER BY tbl_name, name";

                var indexes = await _context.Database
                    .SqlQueryRaw<IndexInfo>(indexQuery)
                    .ToListAsync();

                report.IndexInformation = indexes;
            }

            // Database size estimation
            if (_context.Database.IsSqlite())
            {
                var sizeQuery = "SELECT page_count * page_size as size FROM pragma_page_count(), pragma_page_size()";
                try
                {
                    var sizeResult = await _context.Database.SqlQueryRaw<long>(sizeQuery).FirstOrDefaultAsync();
                    report.DatabaseSizeBytes = sizeResult;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Could not determine database size");
                }
            }

            // Performance recommendations
            report.Recommendations = GenerateRecommendations(report);

            _logger.LogInformation("Database performance report generated successfully");
            return report;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating database performance report");
            throw;
        }
    }

    public async Task CleanupOldDataAsync()
    {
        try
        {
            _logger.LogInformation("Starting old data cleanup");

            var cutoffDate = DateTime.UtcNow.AddMonths(-12);

            // Archive or delete old certificates that have been expired for over a year
            var oldCertificates = await _context.Certificates
                .Where(c => c.ExpirationDate < cutoffDate && c.Status == "Expired")
                .CountAsync();

            if (oldCertificates > 0)
            {
                _logger.LogInformation("Found {Count} old expired certificates for potential cleanup", oldCertificates);
                // In a production environment, you might archive these instead of deleting
                // For now, we'll just log the count
            }

            // Clean up old API keys
            var expiredApiKeys = await _context.ApiKeys
                .Where(k => k.ExpiresAt < DateTime.UtcNow.AddDays(-30))
                .ToListAsync();

            if (expiredApiKeys.Any())
            {
                _context.ApiKeys.RemoveRange(expiredApiKeys);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Cleaned up {Count} expired API keys", expiredApiKeys.Count);
            }

            _logger.LogInformation("Old data cleanup completed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during old data cleanup");
            throw;
        }
    }

    public async Task UpdateStatisticsAsync()
    {
        try
        {
            _logger.LogInformation("Updating database statistics");

            if (_context.Database.IsSqlServer())
            {
                // Update statistics for SQL Server
                var tables = new[] { "Certificates", "Companies", "Products", "Partners", "Users", "ApiKeys" };
                
                foreach (var table in tables)
                {
                    try
                    {
                        await _context.Database.ExecuteSqlRawAsync($"UPDATE STATISTICS {table}");
                        _logger.LogDebug("Updated statistics for table: {Table}", table);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to update statistics for table: {Table}", table);
                    }
                }
            }
            else if (_context.Database.IsSqlite())
            {
                // SQLite automatic vacuum and analyze
                await _context.Database.ExecuteSqlRawAsync("PRAGMA optimize");
                await _context.Database.ExecuteSqlRawAsync("ANALYZE");
                _logger.LogDebug("Executed SQLite ANALYZE and optimize");
            }

            _logger.LogInformation("Database statistics update completed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating database statistics");
            throw;
        }
    }

    private List<string> GenerateRecommendations(DatabasePerformanceReport report)
    {
        var recommendations = new List<string>();

        // Check table sizes
        var largestTable = report.TableStatistics.OrderByDescending(t => t.RecordCount).FirstOrDefault();
        if (largestTable?.RecordCount > 100000)
        {
            recommendations.Add($"Consider partitioning the {largestTable.TableName} table as it contains {largestTable.RecordCount:N0} records");
        }

        // Check database size
        if (report.DatabaseSizeBytes > 1024 * 1024 * 1024) // 1GB
        {
            recommendations.Add("Database size exceeds 1GB - consider archiving old data or implementing data compression");
        }

        // Check for missing indexes (basic heuristic)
        var certificateCount = report.TableStatistics.FirstOrDefault(t => t.TableName == "Certificates")?.RecordCount ?? 0;
        if (certificateCount > 10000)
        {
            recommendations.Add("Large certificate table detected - ensure proper indexing on search columns");
        }

        // General recommendations
        recommendations.Add("Regularly update database statistics for optimal query performance");
        recommendations.Add("Monitor slow query log and optimize frequently used queries");
        recommendations.Add("Consider implementing read replicas for heavy reporting workloads");

        return recommendations;
    }
}

public class DatabasePerformanceReport
{
    public DateTime GeneratedAt { get; set; }
    public string DatabaseProvider { get; set; } = string.Empty;
    public long DatabaseSizeBytes { get; set; }
    public List<TableStatistic> TableStatistics { get; set; } = new();
    public List<IndexInfo> IndexInformation { get; set; } = new();
    public List<string> Recommendations { get; set; } = new();
}

public class TableStatistic
{
    public string TableName { get; set; } = string.Empty;
    public int RecordCount { get; set; }
}

public class IndexInfo
{
    public string IndexName { get; set; } = string.Empty;
    public string TableName { get; set; } = string.Empty;
}