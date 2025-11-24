using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using GCCWebApp.Data;
using GCCWebApp.Models;
using System.Text.Json;

namespace GCCWebApp.Scripts
{
    /// <summary>
    /// Database seeding utility for populating the GCC database with realistic mock data
    /// Provides methods for clearing, seeding, and validating database content
    /// </summary>
    public class DatabaseSeeder
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<DatabaseSeeder> _logger;

        public DatabaseSeeder(ApplicationDbContext context, ILogger<DatabaseSeeder> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Main seeding orchestrator - manages the complete database population process
        /// </summary>
        public async Task<SeedResult> SeedDatabaseAsync(SeedConfiguration config)
        {
            var result = new SeedResult { StartTime = DateTime.UtcNow };

            try
            {
                _logger.LogInformation("Starting database seeding process...");
                _logger.LogInformation($"Configuration: {JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true })}");

                // Phase 1: Database preparation
                if (config.ResetDatabase)
                {
                    await ClearDatabaseAsync();
                }

                await EnsureDatabaseCreatedAsync();

                // Phase 2: Generate mock data
                var mockGenerator = new MockDataGenerator(_context, _logger);
                var success = await mockGenerator.GenerateAllMockDataAsync(
                    config.CompanyCount,
                    config.ProductCount,
                    config.CertificateCount,
                    config.UserCount
                );

                if (!success)
                {
                    throw new Exception("Mock data generation failed");
                }

                // Phase 3: Validation and reporting
                result.Statistics = await GenerateStatisticsAsync();
                result.ValidationResult = await ValidateDataIntegrityAsync();
                result.Success = true;
                result.EndTime = DateTime.UtcNow;
                result.Duration = result.EndTime - result.StartTime;

                _logger.LogInformation($"Database seeding completed successfully in {result.Duration.TotalMinutes:F2} minutes");
                _logger.LogInformation($"Statistics: {JsonSerializer.Serialize(result.Statistics, new JsonSerializerOptions { WriteIndented = true })}");

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Database seeding failed");
                result.Success = false;
                result.ErrorMessage = ex.Message;
                result.EndTime = DateTime.UtcNow;
                result.Duration = result.EndTime - result.StartTime;
                return result;
            }
        }

        /// <summary>
        /// Clears all existing data from the database
        /// </summary>
        public async Task ClearDatabaseAsync()
        {
            _logger.LogInformation("Clearing existing database data...");

            try
            {
                // Disable foreign key constraints for SQLite
                if (_context.Database.IsSqlite())
                {
                    await _context.Database.ExecuteSqlRawAsync("PRAGMA foreign_keys = OFF");
                }

                // Clear tables in dependency order (child tables first)
                await _context.Database.ExecuteSqlRawAsync("DELETE FROM AuditLogs");
                await _context.Database.ExecuteSqlRawAsync("DELETE FROM ApiKeys");
                await _context.Database.ExecuteSqlRawAsync("DELETE FROM Certificates");
                await _context.Database.ExecuteSqlRawAsync("DELETE FROM Products");
                await _context.Database.ExecuteSqlRawAsync("DELETE FROM AspNetUsers WHERE Email NOT LIKE '%admin%'");
                await _context.Database.ExecuteSqlRawAsync("DELETE FROM Companies");

                // Reset auto-increment counters for SQLite
                if (_context.Database.IsSqlite())
                {
                    await _context.Database.ExecuteSqlRawAsync("DELETE FROM sqlite_sequence WHERE name IN ('Companies', 'Products', 'Certificates')");
                    await _context.Database.ExecuteSqlRawAsync("PRAGMA foreign_keys = ON");
                }

                await _context.SaveChangesAsync();
                _logger.LogInformation("Database cleared successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing database");
                throw;
            }
        }

        /// <summary>
        /// Ensures the database and all required tables exist
        /// </summary>
        public async Task EnsureDatabaseCreatedAsync()
        {
            _logger.LogInformation("Ensuring database is created and up-to-date...");

            try
            {
                // Create database if it doesn't exist
                await _context.Database.EnsureCreatedAsync();

                // Apply any pending migrations
                var pendingMigrations = await _context.Database.GetPendingMigrationsAsync();
                if (pendingMigrations.Any())
                {
                    _logger.LogInformation($"Applying {pendingMigrations.Count()} pending migrations...");
                    await _context.Database.MigrateAsync();
                }

                _logger.LogInformation("Database is ready");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error ensuring database creation");
                throw;
            }
        }

        /// <summary>
        /// Generates comprehensive statistics about the seeded data
        /// </summary>
        public async Task<DatabaseStatistics> GenerateStatisticsAsync()
        {
            _logger.LogInformation("Generating database statistics...");

            var statistics = new DatabaseStatistics
            {
                GeneratedAt = DateTime.UtcNow,
                
                // Entity counts
                CompanyCount = await _context.Companies.CountAsync(),
                ProductCount = await _context.Products.CountAsync(),
                CertificateCount = await _context.Certificates.CountAsync(),
                UserCount = await _context.Users.CountAsync(),
                ApiKeyCount = await _context.ApiKeys.CountAsync(),
                
                // Company statistics
                CompaniesByIndustry = await _context.Companies
                    .GroupBy(c => c.Industry)
                    .Select(g => new { Industry = g.Key, Count = g.Count() })
                    .ToDictionaryAsync(x => x.Industry, x => x.Count),

                CompaniesByCountry = await _context.Companies
                    .GroupBy(c => c.Country)
                    .Select(g => new { Country = g.Key, Count = g.Count() })
                    .ToDictionaryAsync(x => x.Country, x => x.Count),

                // Product statistics
                ProductsByCategory = await _context.Products
                    .GroupBy(p => p.Category)
                    .Select(g => new { Category = g.Key, Count = g.Count() })
                    .ToDictionaryAsync(x => x.Category, x => x.Count),

                ActiveProductsCount = await _context.Products.CountAsync(p => p.IsActive),

                // Certificate statistics
                CertificatesByStatus = await _context.Certificates
                    .GroupBy(c => c.Status)
                    .Select(g => new { Status = g.Key, Count = g.Count() })
                    .ToDictionaryAsync(x => x.Status, x => x.Count),

                CertificatesByType = await _context.Certificates
                    .GroupBy(c => c.Type)
                    .Select(g => new { Type = g.Key, Count = g.Count() })
                    .ToDictionaryAsync(x => x.Type, x => x.Count),

                PublicCertificatesCount = await _context.Certificates.CountAsync(c => c.IsPublic),
                ExpiredCertificatesCount = await _context.Certificates.CountAsync(c => c.ExpirationDate < DateTime.UtcNow),
                ExpiringWithin30Days = await _context.Certificates.CountAsync(c => 
                    c.ExpirationDate > DateTime.UtcNow && c.ExpirationDate < DateTime.UtcNow.AddDays(30)),

                // User statistics
                UsersByRole = await _context.Users
                    .GroupBy(u => u.Role)
                    .Select(g => new { Role = g.Key, Count = g.Count() })
                    .ToDictionaryAsync(x => x.Role, x => x.Count),

                ActiveUsersCount = await _context.Users.CountAsync(u => u.IsActive),
                TwoFactorEnabledCount = await _context.Users.CountAsync(u => u.TwoFactorEnabled),
                EmailConfirmedCount = await _context.Users.CountAsync(u => u.EmailConfirmed),

                // API Key statistics
                ActiveApiKeysCount = await _context.ApiKeys.CountAsync(k => k.IsActive)
            };

            return statistics;
        }

        /// <summary>
        /// Validates data integrity and relationships
        /// </summary>
        public async Task<ValidationResult> ValidateDataIntegrityAsync()
        {
            _logger.LogInformation("Validating data integrity...");

            var result = new ValidationResult { IsValid = true, Issues = new List<string>() };

            try
            {
                // Validate foreign key relationships
                var orphanedCertificates = await _context.Certificates
                    .Where(c => !_context.Companies.Any(co => co.Id == c.CompanyId) || 
                               !_context.Products.Any(p => p.Id == c.ProductId))
                    .CountAsync();

                if (orphanedCertificates > 0)
                {
                    result.Issues.Add($"Found {orphanedCertificates} certificates with invalid company or product references");
                    result.IsValid = false;
                }

                // Validate user-company relationships
                var orphanedUsers = await _context.Users
                    .Where(u => u.CompanyId.HasValue && !_context.Companies.Any(c => c.Id == u.CompanyId))
                    .CountAsync();

                if (orphanedUsers > 0)
                {
                    result.Issues.Add($"Found {orphanedUsers} users with invalid company references");
                    result.IsValid = false;
                }

                // Validate API key relationships
                var orphanedApiKeys = await _context.ApiKeys
                    .Where(k => !_context.Users.Any(u => u.Id == k.UserId))
                    .CountAsync();

                if (orphanedApiKeys > 0)
                {
                    result.Issues.Add($"Found {orphanedApiKeys} API keys with invalid user references");
                    result.IsValid = false;
                }

                // Validate business rules
                var futureCertificates = await _context.Certificates
                    .Where(c => c.IssuedDate > DateTime.UtcNow)
                    .CountAsync();

                if (futureCertificates > 0)
                {
                    result.Issues.Add($"Found {futureCertificates} certificates with future issue dates");
                    result.IsValid = false;
                }

                var invalidExpirationDates = await _context.Certificates
                    .Where(c => c.ExpirationDate <= c.IssuedDate)
                    .CountAsync();

                if (invalidExpirationDates > 0)
                {
                    result.Issues.Add($"Found {invalidExpirationDates} certificates with expiration dates before issue dates");
                    result.IsValid = false;
                }

                // Validate required fields
                var companiesWithoutNames = await _context.Companies
                    .Where(c => string.IsNullOrEmpty(c.Name))
                    .CountAsync();

                if (companiesWithoutNames > 0)
                {
                    result.Issues.Add($"Found {companiesWithoutNames} companies without names");
                    result.IsValid = false;
                }

                var certificatesWithoutNumbers = await _context.Certificates
                    .Where(c => string.IsNullOrEmpty(c.Number))
                    .CountAsync();

                if (certificatesWithoutNumbers > 0)
                {
                    result.Issues.Add($"Found {certificatesWithoutNumbers} certificates without numbers");
                    result.IsValid = false;
                }

                if (result.IsValid)
                {
                    _logger.LogInformation("Data integrity validation passed");
                }
                else
                {
                    _logger.LogWarning($"Data integrity validation failed with {result.Issues.Count} issues");
                    foreach (var issue in result.Issues)
                    {
                        _logger.LogWarning($"  - {issue}");
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during data validation");
                result.IsValid = false;
                result.Issues.Add($"Validation error: {ex.Message}");
                return result;
            }
        }

        /// <summary>
        /// Exports database statistics to a JSON file
        /// </summary>
        public async Task ExportStatisticsAsync(DatabaseStatistics statistics, string filePath)
        {
            _logger.LogInformation($"Exporting statistics to {filePath}...");

            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            var json = JsonSerializer.Serialize(statistics, options);
            await File.WriteAllTextAsync(filePath, json);

            _logger.LogInformation("Statistics exported successfully");
        }

        /// <summary>
        /// Gets the current database file size
        /// </summary>
        public async Task<long> GetDatabaseSizeAsync()
        {
            try
            {
                if (_context.Database.IsSqlite())
                {
                    var connectionString = _context.Database.GetConnectionString();
                    var dataSource = connectionString?.Split(';')
                        .FirstOrDefault(x => x.StartsWith("Data Source="))?
                        .Replace("Data Source=", "");

                    if (!string.IsNullOrEmpty(dataSource) && File.Exists(dataSource))
                    {
                        return new FileInfo(dataSource).Length;
                    }
                }

                // For other database types, we'd need different approaches
                return 0;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not determine database size");
                return 0;
            }
        }
    }

    #region Data Models

    public class SeedConfiguration
    {
        public bool ResetDatabase { get; set; } = false;
        public int CompanyCount { get; set; } = 150;
        public int ProductCount { get; set; } = 250;
        public int CertificateCount { get; set; } = 1000;
        public int UserCount { get; set; } = 75;
        public bool GenerateDocuments { get; set; } = true;
        public bool GenerateAuditLogs { get; set; } = true;
        public int RandomSeed { get; set; } = 42;
    }

    public class SeedResult
    {
        public bool Success { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public TimeSpan Duration { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public DatabaseStatistics Statistics { get; set; } = new();
        public ValidationResult ValidationResult { get; set; } = new();
    }

    public class DatabaseStatistics
    {
        public DateTime GeneratedAt { get; set; }
        
        // Entity counts
        public int CompanyCount { get; set; }
        public int ProductCount { get; set; }
        public int CertificateCount { get; set; }
        public int UserCount { get; set; }
        public int ApiKeyCount { get; set; }
        
        // Company statistics
        public Dictionary<string, int> CompaniesByIndustry { get; set; } = new();
        public Dictionary<string, int> CompaniesByCountry { get; set; } = new();
        
        // Product statistics
        public Dictionary<string, int> ProductsByCategory { get; set; } = new();
        public int ActiveProductsCount { get; set; }
        
        // Certificate statistics
        public Dictionary<string, int> CertificatesByStatus { get; set; } = new();
        public Dictionary<string, int> CertificatesByType { get; set; } = new();
        public int PublicCertificatesCount { get; set; }
        public int ExpiredCertificatesCount { get; set; }
        public int ExpiringWithin30Days { get; set; }
        
        // User statistics
        public Dictionary<string, int> UsersByRole { get; set; } = new();
        public int ActiveUsersCount { get; set; }
        public int TwoFactorEnabledCount { get; set; }
        public int EmailConfirmedCount { get; set; }
        
        // API Key statistics
        public int ActiveApiKeysCount { get; set; }
    }

    public class ValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> Issues { get; set; } = new();
    }

    #endregion
}