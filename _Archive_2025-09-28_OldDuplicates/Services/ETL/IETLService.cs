using GCCWebApp.Models;

namespace GCCWebApp.Services.ETL
{
    public interface IETLService
    {
        Task<ETLJobResult> RunFullETLPipelineAsync(ETLConfiguration config);
        Task<ETLJobResult> RunIncrementalETLAsync(ETLConfiguration config);
        Task<List<ETLJobStatus>> GetJobStatusesAsync();
        Task<ETLJobResult> ExtractDataAsync(ETLConfiguration config);
        Task<ETLJobResult> TransformDataAsync(ETLExtractedData extractedData, ETLConfiguration config);
        Task<ETLJobResult> LoadDataAsync(ETLTransformedData transformedData, ETLConfiguration config);
        Task<bool> CancelJobAsync(Guid jobId);
    }

    public interface IDataExtractor
    {
        Task<ETLExtractedData> ExtractCertificatesAsync(DateTime? lastExtractTime = null);
        Task<ETLExtractedData> ExtractCompaniesAsync(DateTime? lastExtractTime = null);
        Task<ETLExtractedData> ExtractProductsAsync(DateTime? lastExtractTime = null);
        Task<ETLExtractedData> ExtractFromExternalSourceAsync(ExternalDataSource source);
    }

    public interface IDataTransformer
    {
        Task<ETLTransformedData> TransformCertificateDataAsync(ETLExtractedData extractedData);
        Task<ETLTransformedData> ValidateAndCleanDataAsync(ETLExtractedData extractedData);
        Task<ETLTransformedData> EnrichDataAsync(ETLExtractedData extractedData);
        Task<ETLTransformedData> AggregateDataAsync(ETLExtractedData extractedData);
    }

    public interface IDataLoader
    {
        Task<ETLLoadResult> LoadToDataWarehouseAsync(ETLTransformedData transformedData);
        Task<ETLLoadResult> LoadToReportingTablesAsync(ETLTransformedData transformedData);
        Task<ETLLoadResult> LoadToFileAsync(ETLTransformedData transformedData, string filePath, string format);
        Task<ETLLoadResult> LoadToExternalSystemAsync(ETLTransformedData transformedData, ExternalDataDestination destination);
    }

    public interface IETLMonitor
    {
        Task<ETLMetrics> GetETLMetricsAsync(DateTime fromDate, DateTime toDate);
        Task LogETLEventAsync(ETLLogEntry logEntry);
        Task<List<ETLAlert>> GetActiveAlertsAsync();
        Task CreateAlertAsync(ETLAlert alert);
    }
}