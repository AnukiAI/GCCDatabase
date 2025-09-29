using GCCWebApp.Models;
using GCCWebApp.Data;
using Microsoft.EntityFrameworkCore;
using System.Collections.Concurrent;

namespace GCCWebApp.Services.ETL
{
    public class ETLService : IETLService
    {
        private readonly IDataExtractor _dataExtractor;
        private readonly IDataTransformer _dataTransformer;
        private readonly IDataLoader _dataLoader;
        private readonly IETLMonitor _monitor;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ETLService> _logger;
        private readonly ConcurrentDictionary<Guid, ETLJobStatus> _activeJobs;

        public ETLService(
            IDataExtractor dataExtractor,
            IDataTransformer dataTransformer,
            IDataLoader dataLoader,
            IETLMonitor monitor,
            ApplicationDbContext context,
            ILogger<ETLService> logger)
        {
            _dataExtractor = dataExtractor;
            _dataTransformer = dataTransformer;
            _dataLoader = dataLoader;
            _monitor = monitor;
            _context = context;
            _logger = logger;
            _activeJobs = new ConcurrentDictionary<Guid, ETLJobStatus>();
        }

        public async Task<ETLJobResult> RunFullETLPipelineAsync(ETLConfiguration config)
        {
            var jobId = Guid.NewGuid();
            var jobResult = new ETLJobResult
            {
                JobId = jobId,
                JobName = config.Name,
                Status = ETLStatus.Running,
                StartTime = DateTime.UtcNow
            };

            var jobStatus = new ETLJobStatus
            {
                JobId = jobId,
                JobName = config.Name,
                Status = ETLStatus.Running,
                StartTime = DateTime.UtcNow,
                ProgressPercentage = 0
            };

            _activeJobs.TryAdd(jobId, jobStatus);

            try
            {
                _logger.LogInformation("Starting full ETL pipeline: {JobName} ({JobId})", config.Name, jobId);
                await _monitor.LogETLEventAsync(new ETLLogEntry
                {
                    JobId = jobId,
                    LogLevel = "Information",
                    Message = $"ETL job started: {config.Name}",
                    Category = "JobStart"
                });

                // Step 1: Extract Data
                jobStatus.CurrentStep = "Extracting Data";
                jobStatus.ProgressPercentage = 10;
                _logger.LogInformation("ETL Job {JobId}: Starting data extraction", jobId);

                var extractResult = await ExtractDataAsync(config);
                if (!extractResult.Status.HasFlag(ETLStatus.Completed))
                {
                    jobResult.Status = ETLStatus.Failed;
                    jobResult.Message = "Data extraction failed";
                    jobResult.Errors.AddRange(extractResult.Errors);
                    return jobResult;
                }

                jobStatus.ProgressPercentage = 40;
                jobResult.RecordsProcessed = extractResult.RecordsProcessed;

                // Step 2: Transform Data
                jobStatus.CurrentStep = "Transforming Data";
                jobStatus.ProgressPercentage = 50;
                _logger.LogInformation("ETL Job {JobId}: Starting data transformation", jobId);

                var extractedData = (ETLExtractedData)extractResult.Metrics["ExtractedData"];
                var transformResult = await TransformDataAsync(extractedData, config);
                if (!transformResult.Status.HasFlag(ETLStatus.Completed))
                {
                    jobResult.Status = ETLStatus.Failed;
                    jobResult.Message = "Data transformation failed";
                    jobResult.Errors.AddRange(transformResult.Errors);
                    return jobResult;
                }

                jobStatus.ProgressPercentage = 70;
                jobResult.RecordsSucceeded = transformResult.RecordsSucceeded;
                jobResult.RecordsFailed = transformResult.RecordsFailed;

                // Step 3: Load Data
                jobStatus.CurrentStep = "Loading Data";
                jobStatus.ProgressPercentage = 80;
                _logger.LogInformation("ETL Job {JobId}: Starting data loading", jobId);

                var transformedData = (ETLTransformedData)transformResult.Metrics["TransformedData"];
                var loadResult = await LoadDataAsync(transformedData, config);
                if (!loadResult.Status.HasFlag(ETLStatus.Completed))
                {
                    jobResult.Status = ETLStatus.Failed;
                    jobResult.Message = "Data loading failed";
                    jobResult.Errors.AddRange(loadResult.Errors);
                    return jobResult;
                }

                // Finalize job
                jobStatus.CurrentStep = "Completed";
                jobStatus.ProgressPercentage = 100;
                jobStatus.Status = ETLStatus.Completed;

                jobResult.Status = ETLStatus.Completed;
                jobResult.EndTime = DateTime.UtcNow;
                jobResult.Message = $"ETL pipeline completed successfully. Processed {jobResult.RecordsProcessed} records.";

                // Update configuration last run time
                config.LastRunTime = DateTime.UtcNow;

                _logger.LogInformation("ETL Job {JobId} completed successfully in {Duration}", 
                    jobId, jobResult.Duration);

                await _monitor.LogETLEventAsync(new ETLLogEntry
                {
                    JobId = jobId,
                    LogLevel = "Information",
                    Message = $"ETL job completed successfully: {config.Name}",
                    Category = "JobComplete",
                    Properties = new Dictionary<string, object>
                    {
                        ["RecordsProcessed"] = jobResult.RecordsProcessed,
                        ["Duration"] = jobResult.Duration?.TotalMinutes ?? 0
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ETL Job {JobId} failed with exception", jobId);
                
                jobStatus.Status = ETLStatus.Failed;
                jobStatus.Message = ex.Message;

                jobResult.Status = ETLStatus.Failed;
                jobResult.EndTime = DateTime.UtcNow;
                jobResult.Message = $"ETL pipeline failed: {ex.Message}";
                jobResult.Errors.Add(new ETLError
                {
                    ErrorType = "PipelineError",
                    Message = ex.Message,
                    StackTrace = ex.StackTrace,
                    Severity = ETLSeverity.Error
                });

                await _monitor.LogETLEventAsync(new ETLLogEntry
                {
                    JobId = jobId,
                    LogLevel = "Error",
                    Message = $"ETL job failed: {ex.Message}",
                    Category = "JobError"
                });

                await _monitor.CreateAlertAsync(new ETLAlert
                {
                    AlertType = "JobFailed",
                    Message = $"ETL job {config.Name} failed: {ex.Message}",
                    Severity = ETLSeverity.Error,
                    Context = new Dictionary<string, object>
                    {
                        ["JobId"] = jobId,
                        ["JobName"] = config.Name,
                        ["Error"] = ex.Message
                    }
                });
            }
            finally
            {
                jobStatus.EndTime = DateTime.UtcNow;
                _activeJobs.TryUpdate(jobId, jobStatus, jobStatus);
            }

            return jobResult;
        }

        public async Task<ETLJobResult> RunIncrementalETLAsync(ETLConfiguration config)
        {
            _logger.LogInformation("Starting incremental ETL for configuration: {ConfigName}", config.Name);

            // Set incremental flag
            config.IsIncremental = true;

            return await RunFullETLPipelineAsync(config);
        }

        public Task<List<ETLJobStatus>> GetJobStatusesAsync()
        {
            return Task.FromResult(_activeJobs.Values.ToList());
        }

        public async Task<ETLJobResult> ExtractDataAsync(ETLConfiguration config)
        {
            var jobResult = new ETLJobResult
            {
                JobId = Guid.NewGuid(),
                JobName = $"{config.Name} - Extract",
                Status = ETLStatus.Running,
                StartTime = DateTime.UtcNow
            };

            try
            {
                var allExtractedData = new ETLExtractedData
                {
                    DataSource = "Combined",
                    ExtractionTime = DateTime.UtcNow
                };

                // Extract certificates if specified
                if (config.DataSources.Contains("Certificates"))
                {
                    var certificateData = await _dataExtractor.ExtractCertificatesAsync(
                        config.IsIncremental ? config.LastRunTime : null);
                    
                    foreach (var data in certificateData.Data)
                    {
                        allExtractedData.Data.Add($"Certificates_{data.Key}", data.Value);
                    }
                    
                    jobResult.RecordsProcessed += certificateData.TotalRecords;
                }

                // Extract companies if specified
                if (config.DataSources.Contains("Companies"))
                {
                    var companyData = await _dataExtractor.ExtractCompaniesAsync(
                        config.IsIncremental ? config.LastRunTime : null);
                    
                    foreach (var data in companyData.Data)
                    {
                        allExtractedData.Data.Add($"Companies_{data.Key}", data.Value);
                    }
                    
                    jobResult.RecordsProcessed += companyData.TotalRecords;
                }

                // Extract products if specified
                if (config.DataSources.Contains("Products"))
                {
                    var productData = await _dataExtractor.ExtractProductsAsync(
                        config.IsIncremental ? config.LastRunTime : null);
                    
                    foreach (var data in productData.Data)
                    {
                        allExtractedData.Data.Add($"Products_{data.Key}", data.Value);
                    }
                    
                    jobResult.RecordsProcessed += productData.TotalRecords;
                }

                // Extract from external sources if specified
                foreach (var sourceName in config.DataSources.Where(s => !new[] { "Certificates", "Companies", "Products" }.Contains(s)))
                {
                    // This would require external source configuration
                    _logger.LogWarning("External data source {SourceName} extraction not implemented", sourceName);
                }

                allExtractedData.TotalRecords = jobResult.RecordsProcessed;
                
                jobResult.Status = ETLStatus.Completed;
                jobResult.EndTime = DateTime.UtcNow;
                jobResult.RecordsSucceeded = jobResult.RecordsProcessed;
                jobResult.Message = $"Data extraction completed successfully. Extracted {jobResult.RecordsProcessed} records.";
                jobResult.Metrics.Add("ExtractedData", allExtractedData);

                _logger.LogInformation("Data extraction completed. Total records: {RecordCount}", jobResult.RecordsProcessed);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Data extraction failed");
                jobResult.Status = ETLStatus.Failed;
                jobResult.EndTime = DateTime.UtcNow;
                jobResult.Message = $"Data extraction failed: {ex.Message}";
                jobResult.Errors.Add(new ETLError
                {
                    ErrorType = "ExtractionError",
                    Message = ex.Message,
                    StackTrace = ex.StackTrace,
                    Severity = ETLSeverity.Error
                });
            }

            return jobResult;
        }

        public async Task<ETLJobResult> TransformDataAsync(ETLExtractedData extractedData, ETLConfiguration config)
        {
            var jobResult = new ETLJobResult
            {
                JobId = Guid.NewGuid(),
                JobName = $"{config.Name} - Transform",
                Status = ETLStatus.Running,
                StartTime = DateTime.UtcNow,
                RecordsProcessed = extractedData.TotalRecords
            };

            try
            {
                ETLTransformedData? transformedData = null;

                // Apply transformations based on configuration
                foreach (var transformation in config.Transformations)
                {
                    switch (transformation.ToLower())
                    {
                        case "validate":
                            transformedData = await _dataTransformer.ValidateAndCleanDataAsync(extractedData);
                            break;
                        case "enrich":
                            transformedData = await _dataTransformer.EnrichDataAsync(extractedData);
                            break;
                        case "aggregate":
                            transformedData = await _dataTransformer.AggregateDataAsync(extractedData);
                            break;
                        case "certificates":
                            transformedData = await _dataTransformer.TransformCertificateDataAsync(extractedData);
                            break;
                        default:
                            _logger.LogWarning("Unknown transformation type: {TransformationType}", transformation);
                            break;
                    }

                    if (transformedData != null)
                    {
                        // Use transformed data as input for next transformation
                        extractedData = new ETLExtractedData
                        {
                            Data = transformedData.TransformedData.ToDictionary(
                                kvp => kvp.Key, 
                                kvp => kvp.Value),
                            TotalRecords = transformedData.TotalRecords
                        };
                    }
                }

                if (transformedData == null)
                {
                    // If no specific transformations, apply default validation
                    transformedData = await _dataTransformer.ValidateAndCleanDataAsync(extractedData);
                }

                jobResult.Status = ETLStatus.Completed;
                jobResult.EndTime = DateTime.UtcNow;
                jobResult.RecordsSucceeded = transformedData.ValidRecords;
                jobResult.RecordsFailed = transformedData.InvalidRecords;
                jobResult.Message = $"Data transformation completed. Valid: {transformedData.ValidRecords}, Invalid: {transformedData.InvalidRecords}";
                jobResult.Metrics.Add("TransformedData", transformedData);
                jobResult.Metrics.Add("DataQualityIssues", transformedData.DataQualityIssues.Count);

                _logger.LogInformation("Data transformation completed. Valid records: {ValidRecords}, Invalid records: {InvalidRecords}", 
                    transformedData.ValidRecords, transformedData.InvalidRecords);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Data transformation failed");
                jobResult.Status = ETLStatus.Failed;
                jobResult.EndTime = DateTime.UtcNow;
                jobResult.Message = $"Data transformation failed: {ex.Message}";
                jobResult.Errors.Add(new ETLError
                {
                    ErrorType = "TransformationError",
                    Message = ex.Message,
                    StackTrace = ex.StackTrace,
                    Severity = ETLSeverity.Error
                });
            }

            return jobResult;
        }

        public async Task<ETLJobResult> LoadDataAsync(ETLTransformedData transformedData, ETLConfiguration config)
        {
            var jobResult = new ETLJobResult
            {
                JobId = Guid.NewGuid(),
                JobName = $"{config.Name} - Load",
                Status = ETLStatus.Running,
                StartTime = DateTime.UtcNow,
                RecordsProcessed = transformedData.TotalRecords
            };

            try
            {
                var totalRecordsLoaded = 0;
                var allLoadErrors = new List<ETLError>();

                // Load to destinations based on configuration
                foreach (var destination in config.Destinations)
                {
                    ETLLoadResult loadResult = null;

                    switch (destination.ToLower())
                    {
                        case "datawarehouse":
                            loadResult = await _dataLoader.LoadToDataWarehouseAsync(transformedData);
                            break;
                        case "reporting":
                            loadResult = await _dataLoader.LoadToReportingTablesAsync(transformedData);
                            break;
                        case var dest when dest.StartsWith("file:"):
                            var parts = dest.Split(':');
                            if (parts.Length >= 3)
                            {
                                var filePath = parts[1];
                                var format = parts[2];
                                loadResult = await _dataLoader.LoadToFileAsync(transformedData, filePath, format);
                            }
                            break;
                        default:
                            _logger.LogWarning("Unknown destination type: {DestinationType}", destination);
                            break;
                    }

                    if (loadResult != null)
                    {
                        totalRecordsLoaded += loadResult.RecordsLoaded;
                        allLoadErrors.AddRange(loadResult.LoadErrors);
                        
                        if (!loadResult.Success)
                        {
                            _logger.LogError("Load to destination {Destination} failed: {Message}", 
                                destination, loadResult.Message);
                        }
                    }
                }

                jobResult.Status = ETLStatus.Completed;
                jobResult.EndTime = DateTime.UtcNow;
                jobResult.RecordsSucceeded = totalRecordsLoaded;
                jobResult.RecordsFailed = transformedData.TotalRecords - totalRecordsLoaded;
                jobResult.Errors.AddRange(allLoadErrors);
                jobResult.Message = $"Data loading completed. Loaded {totalRecordsLoaded} records to {config.Destinations.Count} destinations.";

                _logger.LogInformation("Data loading completed. Records loaded: {RecordsLoaded}", totalRecordsLoaded);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Data loading failed");
                jobResult.Status = ETLStatus.Failed;
                jobResult.EndTime = DateTime.UtcNow;
                jobResult.Message = $"Data loading failed: {ex.Message}";
                jobResult.Errors.Add(new ETLError
                {
                    ErrorType = "LoadingError",
                    Message = ex.Message,
                    StackTrace = ex.StackTrace,
                    Severity = ETLSeverity.Error
                });
            }

            return jobResult;
        }

        public async Task<bool> CancelJobAsync(Guid jobId)
        {
            if (_activeJobs.TryGetValue(jobId, out var jobStatus))
            {
                jobStatus.Status = ETLStatus.Cancelled;
                jobStatus.EndTime = DateTime.UtcNow;
                jobStatus.Message = "Job cancelled by user";
                
                _logger.LogInformation("ETL Job {JobId} cancelled", jobId);
                
                await _monitor.LogETLEventAsync(new ETLLogEntry
                {
                    JobId = jobId,
                    LogLevel = "Warning",
                    Message = "ETL job cancelled",
                    Category = "JobCancelled"
                });

                return true;
            }

            return false;
        }
    }
}