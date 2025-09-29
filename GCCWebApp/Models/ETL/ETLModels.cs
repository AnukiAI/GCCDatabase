using System.ComponentModel.DataAnnotations;

namespace GCCWebApp.Models
{
    public class ETLConfiguration
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = string.Empty;
        public ETLJobType JobType { get; set; }
        public bool IsIncremental { get; set; }
        public DateTime? LastRunTime { get; set; }
        public List<string> DataSources { get; set; } = new();
        public List<string> Transformations { get; set; } = new();
        public List<string> Destinations { get; set; } = new();
        public Dictionary<string, object> Settings { get; set; } = new();
        public bool IsActive { get; set; } = true;
        public string? CronSchedule { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }

    public class ETLJobResult
    {
        public Guid JobId { get; set; }
        public string JobName { get; set; } = string.Empty;
        public ETLStatus Status { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public TimeSpan? Duration => EndTime?.Subtract(StartTime);
        public int RecordsProcessed { get; set; }
        public int RecordsSucceeded { get; set; }
        public int RecordsFailed { get; set; }
        public List<ETLError> Errors { get; set; } = new();
        public Dictionary<string, object> Metrics { get; set; } = new();
        public string? Message { get; set; }
    }

    public class ETLJobStatus
    {
        public Guid JobId { get; set; }
        public string JobName { get; set; } = string.Empty;
        public ETLStatus Status { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public int ProgressPercentage { get; set; }
        public string? CurrentStep { get; set; }
        public string? Message { get; set; }
    }

    public class ETLExtractedData
    {
        public Guid ExtractionId { get; set; } = Guid.NewGuid();
        public DateTime ExtractionTime { get; set; } = DateTime.UtcNow;
        public string DataSource { get; set; } = string.Empty;
        public Dictionary<string, List<object>> Data { get; set; } = new();
        public Dictionary<string, object> Metadata { get; set; } = new();
        public int TotalRecords { get; set; }
    }

    public class ETLTransformedData
    {
        public Guid TransformationId { get; set; } = Guid.NewGuid();
        public DateTime TransformationTime { get; set; } = DateTime.UtcNow;
        public Dictionary<string, List<object>> TransformedData { get; set; } = new();
        public Dictionary<string, object> TransformationMetadata { get; set; } = new();
        public List<ETLDataQualityIssue> DataQualityIssues { get; set; } = new();
        public int TotalRecords { get; set; }
        public int ValidRecords { get; set; }
        public int InvalidRecords { get; set; }
    }

    public class ETLLoadResult
    {
        public Guid LoadId { get; set; } = Guid.NewGuid();
        public DateTime LoadTime { get; set; } = DateTime.UtcNow;
        public string Destination { get; set; } = string.Empty;
        public int RecordsLoaded { get; set; }
        public int RecordsFailed { get; set; }
        public List<ETLError> LoadErrors { get; set; } = new();
        public bool Success { get; set; }
        public string? Message { get; set; }
    }

    public class ETLError
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string ErrorType { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string? StackTrace { get; set; }
        public Dictionary<string, object> Context { get; set; } = new();
        public string? RecordId { get; set; }
        public ETLSeverity Severity { get; set; }
    }

    public class ETLDataQualityIssue
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string IssueType { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string FieldName { get; set; } = string.Empty;
        public string? RecordId { get; set; }
        public object? ExpectedValue { get; set; }
        public object? ActualValue { get; set; }
        public ETLSeverity Severity { get; set; }
        public DateTime DetectedAt { get; set; } = DateTime.UtcNow;
    }

    public class ETLMetrics
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public int TotalJobsRun { get; set; }
        public int SuccessfulJobs { get; set; }
        public int FailedJobs { get; set; }
        public long TotalRecordsProcessed { get; set; }
        public TimeSpan AverageJobDuration { get; set; }
        public Dictionary<string, int> JobTypeMetrics { get; set; } = new();
        public Dictionary<string, double> DataQualityMetrics { get; set; } = new();
        public List<ETLPerformanceMetric> PerformanceMetrics { get; set; } = new();
    }

    public class ETLPerformanceMetric
    {
        public string MetricName { get; set; } = string.Empty;
        public double Value { get; set; }
        public string Unit { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    public class ETLAlert
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string AlertType { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public ETLSeverity Severity { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool IsResolved { get; set; }
        public DateTime? ResolvedAt { get; set; }
        public Dictionary<string, object> Context { get; set; } = new();
    }

    public class ETLLogEntry
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid JobId { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string LogLevel { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string? Category { get; set; }
        public Dictionary<string, object> Properties { get; set; } = new();
    }

    public class ExternalDataSource
    {
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty; // API, Database, File, etc.
        public string ConnectionString { get; set; } = string.Empty;
        public Dictionary<string, string> Configuration { get; set; } = new();
        public Dictionary<string, string> Authentication { get; set; } = new();
    }

    public class ExternalDataDestination
    {
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty; // Database, File, API, etc.
        public string ConnectionString { get; set; } = string.Empty;
        public Dictionary<string, string> Configuration { get; set; } = new();
    }

    public class CertificateAggregateData
    {
        public string Period { get; set; } = string.Empty; // Daily, Weekly, Monthly
        public DateTime PeriodDate { get; set; }
        public int TotalCertificates { get; set; }
        public int ActiveCertificates { get; set; }
        public int ExpiredCertificates { get; set; }
        public int ExpiringThisMonth { get; set; }
        public Dictionary<string, int> CertificatesByType { get; set; } = new();
        public Dictionary<string, int> CertificatesByCountry { get; set; } = new();
        public Dictionary<string, int> CertificatesByCompany { get; set; } = new();
        public double AverageValidityPeriod { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public enum ETLJobType
    {
        Full,
        Incremental,
        RealTime,
        OnDemand,
        Scheduled
    }

    [Flags]
    public enum ETLStatus
    {
        Pending = 1,
        Running = 2,
        Completed = 4,
        Failed = 8,
        Cancelled = 16,
        Paused = 32
    }

    public enum ETLSeverity
    {
        Info,
        Warning,
        Error,
        Critical
    }
}