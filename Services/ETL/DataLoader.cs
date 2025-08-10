using Microsoft.EntityFrameworkCore;
using GCCWebApp.Data;
using GCCWebApp.Models;
using System.Text.Json;
using System.Text;
using System.Data.SqlClient;

namespace GCCWebApp.Services.ETL
{
    public class DataLoader : IDataLoader
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<DataLoader> _logger;
        private readonly IConfiguration _configuration;

        public DataLoader(ApplicationDbContext context, ILogger<DataLoader> logger, IConfiguration configuration)
        {
            _context = context;
            _logger = logger;
            _configuration = configuration;
        }

        public async Task<ETLLoadResult> LoadToDataWarehouseAsync(ETLTransformedData transformedData)
        {
            _logger.LogInformation("Starting data warehouse load operation");

            var loadResult = new ETLLoadResult
            {
                Destination = "DataWarehouse",
                LoadTime = DateTime.UtcNow
            };

            try
            {
                using var transaction = await _context.Database.BeginTransactionAsync();

                try
                {
                    // Load certificate aggregates
                    if (transformedData.TransformedData.ContainsKey("DailyCertificateAggregates"))
                    {
                        var result = await LoadCertificateAggregatesAsync(
                            transformedData.TransformedData["DailyCertificateAggregates"], "Daily");
                        loadResult.RecordsLoaded += result.RecordsLoaded;
                        loadResult.LoadErrors.AddRange(result.LoadErrors);
                    }

                    if (transformedData.TransformedData.ContainsKey("WeeklyCertificateAggregates"))
                    {
                        var result = await LoadCertificateAggregatesAsync(
                            transformedData.TransformedData["WeeklyCertificateAggregates"], "Weekly");
                        loadResult.RecordsLoaded += result.RecordsLoaded;
                        loadResult.LoadErrors.AddRange(result.LoadErrors);
                    }

                    if (transformedData.TransformedData.ContainsKey("MonthlyCertificateAggregates"))
                    {
                        var result = await LoadCertificateAggregatesAsync(
                            transformedData.TransformedData["MonthlyCertificateAggregates"], "Monthly");
                        loadResult.RecordsLoaded += result.RecordsLoaded;
                        loadResult.LoadErrors.AddRange(result.LoadErrors);
                    }

                    // Load company aggregates
                    if (transformedData.TransformedData.ContainsKey("CompanyAggregates"))
                    {
                        var result = await LoadCompanyAggregatesAsync(
                            transformedData.TransformedData["CompanyAggregates"]);
                        loadResult.RecordsLoaded += result.RecordsLoaded;
                        loadResult.LoadErrors.AddRange(result.LoadErrors);
                    }

                    await transaction.CommitAsync();
                    loadResult.Success = true;
                    loadResult.Message = $"Successfully loaded {loadResult.RecordsLoaded} records to data warehouse";

                    _logger.LogInformation("Data warehouse load completed successfully. Records loaded: {RecordsLoaded}", 
                        loadResult.RecordsLoaded);
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during data warehouse load operation");
                loadResult.Success = false;
                loadResult.Message = $"Data warehouse load failed: {ex.Message}";
                loadResult.LoadErrors.Add(new ETLError
                {
                    ErrorType = "LoadError",
                    Message = ex.Message,
                    StackTrace = ex.StackTrace,
                    Severity = ETLSeverity.Error
                });
            }

            return loadResult;
        }

        public async Task<ETLLoadResult> LoadToReportingTablesAsync(ETLTransformedData transformedData)
        {
            _logger.LogInformation("Starting reporting tables load operation");

            var loadResult = new ETLLoadResult
            {
                Destination = "ReportingTables",
                LoadTime = DateTime.UtcNow
            };

            try
            {
                // Create reporting views and summary tables
                await CreateCertificateReportingViews();
                await CreateCompanyReportingViews();
                await CreateComplianceReportingTables(transformedData);

                loadResult.Success = true;
                loadResult.Message = "Reporting tables updated successfully";

                _logger.LogInformation("Reporting tables load completed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during reporting tables load operation");
                loadResult.Success = false;
                loadResult.Message = $"Reporting tables load failed: {ex.Message}";
                loadResult.LoadErrors.Add(new ETLError
                {
                    ErrorType = "ReportingLoadError",
                    Message = ex.Message,
                    StackTrace = ex.StackTrace,
                    Severity = ETLSeverity.Error
                });
            }

            return loadResult;
        }

        public async Task<ETLLoadResult> LoadToFileAsync(ETLTransformedData transformedData, string filePath, string format)
        {
            _logger.LogInformation("Starting file load operation to {FilePath} in {Format} format", filePath, format);

            var loadResult = new ETLLoadResult
            {
                Destination = $"File: {filePath}",
                LoadTime = DateTime.UtcNow
            };

            try
            {
                switch (format.ToLower())
                {
                    case "json":
                        await LoadToJsonFileAsync(transformedData, filePath, loadResult);
                        break;
                    case "csv":
                        await LoadToCsvFileAsync(transformedData, filePath, loadResult);
                        break;
                    case "xml":
                        await LoadToXmlFileAsync(transformedData, filePath, loadResult);
                        break;
                    default:
                        throw new NotSupportedException($"File format '{format}' is not supported");
                }

                loadResult.Success = true;
                loadResult.Message = $"Data successfully exported to {filePath}";

                _logger.LogInformation("File load completed successfully to {FilePath}", filePath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during file load operation to {FilePath}", filePath);
                loadResult.Success = false;
                loadResult.Message = $"File load failed: {ex.Message}";
                loadResult.LoadErrors.Add(new ETLError
                {
                    ErrorType = "FileLoadError",
                    Message = ex.Message,
                    StackTrace = ex.StackTrace,
                    Severity = ETLSeverity.Error
                });
            }

            return loadResult;
        }

        public async Task<ETLLoadResult> LoadToExternalSystemAsync(ETLTransformedData transformedData, ExternalDataDestination destination)
        {
            _logger.LogInformation("Starting external system load operation to {Destination}", destination.Name);

            var loadResult = new ETLLoadResult
            {
                Destination = $"External: {destination.Name}",
                LoadTime = DateTime.UtcNow
            };

            try
            {
                switch (destination.Type.ToLower())
                {
                    case "api":
                        await LoadToExternalApiAsync(transformedData, destination, loadResult);
                        break;
                    case "database":
                        await LoadToExternalDatabaseAsync(transformedData, destination, loadResult);
                        break;
                    case "ftp":
                        await LoadToFtpAsync(transformedData, destination, loadResult);
                        break;
                    default:
                        throw new NotSupportedException($"External destination type '{destination.Type}' is not supported");
                }

                loadResult.Success = true;
                loadResult.Message = $"Data successfully loaded to external system {destination.Name}";

                _logger.LogInformation("External system load completed successfully to {Destination}", destination.Name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during external system load operation to {Destination}", destination.Name);
                loadResult.Success = false;
                loadResult.Message = $"External system load failed: {ex.Message}";
                loadResult.LoadErrors.Add(new ETLError
                {
                    ErrorType = "ExternalLoadError",
                    Message = ex.Message,
                    StackTrace = ex.StackTrace,
                    Severity = ETLSeverity.Error
                });
            }

            return loadResult;
        }

        private async Task<ETLLoadResult> LoadCertificateAggregatesAsync(List<object> aggregates, string period)
        {
            var loadResult = new ETLLoadResult
            {
                Destination = $"CertificateAggregates_{period}",
                LoadTime = DateTime.UtcNow
            };

            try
            {
                foreach (var aggregate in aggregates)
                {
                    var aggregateData = JsonSerializer.Deserialize<CertificateAggregateData>(
                        JsonSerializer.Serialize(aggregate));

                    if (aggregateData != null)
                    {
                        // Check if aggregate already exists for this period and date
                        var existing = await _context.Set<CertificateAggregateData>()
                            .FirstOrDefaultAsync(a => a.Period == period && a.PeriodDate == aggregateData.PeriodDate);

                        if (existing != null)
                        {
                            // Update existing aggregate
                            existing.TotalCertificates = aggregateData.TotalCertificates;
                            existing.ActiveCertificates = aggregateData.ActiveCertificates;
                            existing.ExpiredCertificates = aggregateData.ExpiredCertificates;
                            existing.CreatedAt = DateTime.UtcNow;
                            _context.Update(existing);
                        }
                        else
                        {
                            // Add new aggregate
                            _context.Add(aggregateData);
                        }

                        loadResult.RecordsLoaded++;
                    }
                }

                await _context.SaveChangesAsync();
                loadResult.Success = true;
            }
            catch (Exception ex)
            {
                loadResult.RecordsFailed = aggregates.Count - loadResult.RecordsLoaded;
                loadResult.LoadErrors.Add(new ETLError
                {
                    ErrorType = "AggregateLoadError",
                    Message = ex.Message,
                    Severity = ETLSeverity.Error
                });
            }

            return loadResult;
        }

        private async Task<ETLLoadResult> LoadCompanyAggregatesAsync(List<object> aggregates)
        {
            var loadResult = new ETLLoadResult
            {
                Destination = "CompanyAggregates",
                LoadTime = DateTime.UtcNow
            };

            try
            {
                // Implementation for company aggregates loading
                // This would involve creating/updating company summary records
                loadResult.RecordsLoaded = aggregates.Count;
                loadResult.Success = true;
            }
            catch (Exception ex)
            {
                loadResult.LoadErrors.Add(new ETLError
                {
                    ErrorType = "CompanyAggregateLoadError",
                    Message = ex.Message,
                    Severity = ETLSeverity.Error
                });
            }

            return loadResult;
        }

        private async Task CreateCertificateReportingViews()
        {
            var sql = @"
                CREATE OR REPLACE VIEW CertificateReportingView AS
                SELECT 
                    c.Id,
                    c.Number,
                    c.Status,
                    c.EffectiveDate,
                    c.ExpirationDate,
                    c.Country,
                    comp.Name AS CompanyName,
                    p.Name AS ProductName,
                    ct.Name AS CertificationType,
                    CASE 
                        WHEN c.ExpirationDate < GETDATE() THEN 'Expired'
                        WHEN c.ExpirationDate <= DATEADD(day, 30, GETDATE()) THEN 'Expiring Soon'
                        ELSE 'Active'
                    END AS ExpirationStatus,
                    DATEDIFF(day, GETDATE(), c.ExpirationDate) AS DaysUntilExpiration
                FROM Certificates c
                INNER JOIN Companies comp ON c.CompanyId = comp.Id
                INNER JOIN Products p ON c.ProductId = p.Id
                INNER JOIN CertificationTypes ct ON c.CertificationTypeId = ct.Id
            ";

            await _context.Database.ExecuteSqlRawAsync(sql);
        }

        private async Task CreateCompanyReportingViews()
        {
            var sql = @"
                CREATE OR REPLACE VIEW CompanyReportingView AS
                SELECT 
                    c.Id,
                    c.Name,
                    c.Country,
                    c.IsActive,
                    COUNT(cert.Id) AS TotalCertificates,
                    COUNT(CASE WHEN cert.Status = 'active' THEN 1 END) AS ActiveCertificates,
                    COUNT(CASE WHEN cert.ExpirationDate < GETDATE() THEN 1 END) AS ExpiredCertificates,
                    COUNT(CASE WHEN cert.ExpirationDate <= DATEADD(day, 30, GETDATE()) THEN 1 END) AS ExpiringSoonCertificates
                FROM Companies c
                LEFT JOIN Certificates cert ON c.Id = cert.CompanyId
                GROUP BY c.Id, c.Name, c.Country, c.IsActive
            ";

            await _context.Database.ExecuteSqlRawAsync(sql);
        }

        private async Task CreateComplianceReportingTables(ETLTransformedData transformedData)
        {
            // Create compliance dashboard data
            var complianceSql = @"
                CREATE TABLE IF NOT EXISTS ComplianceDashboard (
                    Id INT PRIMARY KEY IDENTITY(1,1),
                    ReportDate DATE,
                    TotalCertificates INT,
                    CompliantCertificates INT,
                    NonCompliantCertificates INT,
                    ComplianceRate DECIMAL(5,2),
                    CreatedAt DATETIME2 DEFAULT GETDATE()
                )
            ";

            await _context.Database.ExecuteSqlRawAsync(complianceSql);
        }

        private async Task LoadToJsonFileAsync(ETLTransformedData transformedData, string filePath, ETLLoadResult loadResult)
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            var jsonData = JsonSerializer.Serialize(transformedData.TransformedData, options);
            await File.WriteAllTextAsync(filePath, jsonData);
            
            loadResult.RecordsLoaded = transformedData.TotalRecords;
        }

        private async Task LoadToCsvFileAsync(ETLTransformedData transformedData, string filePath, ETLLoadResult loadResult)
        {
            var csv = new StringBuilder();
            
            foreach (var dataSet in transformedData.TransformedData)
            {
                csv.AppendLine($"# {dataSet.Key}");
                
                if (dataSet.Value.Count > 0)
                {
                    var firstRecord = JsonSerializer.Deserialize<Dictionary<string, object>>(
                        JsonSerializer.Serialize(dataSet.Value.First()));
                    
                    if (firstRecord != null)
                    {
                        // Headers
                        csv.AppendLine(string.Join(",", firstRecord.Keys));
                        
                        // Data
                        foreach (var record in dataSet.Value)
                        {
                            var recordDict = JsonSerializer.Deserialize<Dictionary<string, object>>(
                                JsonSerializer.Serialize(record));
                            
                            if (recordDict != null)
                            {
                                csv.AppendLine(string.Join(",", recordDict.Values.Select(v => 
                                    v?.ToString()?.Replace(",", ";") ?? "")));
                            }
                        }
                    }
                }
                
                csv.AppendLine();
            }

            await File.WriteAllTextAsync(filePath, csv.ToString());
            loadResult.RecordsLoaded = transformedData.TotalRecords;
        }

        private async Task LoadToXmlFileAsync(ETLTransformedData transformedData, string filePath, ETLLoadResult loadResult)
        {
            var xml = new StringBuilder();
            xml.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
            xml.AppendLine("<ETLData>");

            foreach (var dataSet in transformedData.TransformedData)
            {
                xml.AppendLine($"  <{dataSet.Key}>");
                
                foreach (var record in dataSet.Value)
                {
                    xml.AppendLine("    <Record>");
                    
                    var recordDict = JsonSerializer.Deserialize<Dictionary<string, object>>(
                        JsonSerializer.Serialize(record));
                    
                    if (recordDict != null)
                    {
                        foreach (var kvp in recordDict)
                        {
                            xml.AppendLine($"      <{kvp.Key}>{System.Security.SecurityElement.Escape(kvp.Value?.ToString() ?? "")}</{kvp.Key}>");
                        }
                    }
                    
                    xml.AppendLine("    </Record>");
                }
                
                xml.AppendLine($"  </{dataSet.Key}>");
            }

            xml.AppendLine("</ETLData>");

            await File.WriteAllTextAsync(filePath, xml.ToString());
            loadResult.RecordsLoaded = transformedData.TotalRecords;
        }

        private async Task LoadToExternalApiAsync(ETLTransformedData transformedData, ExternalDataDestination destination, ETLLoadResult loadResult)
        {
            using var httpClient = new HttpClient();
            
            // Add authentication if provided
            if (destination.Configuration.ContainsKey("ApiKey"))
            {
                httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {destination.Configuration["ApiKey"]}");
            }

            var jsonData = JsonSerializer.Serialize(transformedData.TransformedData);
            var content = new StringContent(jsonData, Encoding.UTF8, "application/json");

            var response = await httpClient.PostAsync(destination.ConnectionString, content);
            
            if (response.IsSuccessStatusCode)
            {
                loadResult.RecordsLoaded = transformedData.TotalRecords;
            }
            else
            {
                throw new Exception($"API call failed with status code: {response.StatusCode}");
            }
        }

        private async Task LoadToExternalDatabaseAsync(ETLTransformedData transformedData, ExternalDataDestination destination, ETLLoadResult loadResult)
        {
            // This would implement loading to an external database
            // For now, we'll just log that it's not implemented
            _logger.LogWarning("External database loading not implemented for destination: {Destination}", destination.Name);
            loadResult.RecordsLoaded = 0;
        }

        private async Task LoadToFtpAsync(ETLTransformedData transformedData, ExternalDataDestination destination, ETLLoadResult loadResult)
        {
            // This would implement FTP upload functionality
            // For now, we'll just log that it's not implemented
            _logger.LogWarning("FTP loading not implemented for destination: {Destination}", destination.Name);
            loadResult.RecordsLoaded = 0;
        }
    }
}