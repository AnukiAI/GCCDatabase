# ETL Process Usage Guide

## Overview

The GCC Web Application now includes a comprehensive ETL (Extract, Transform, Load) system designed to handle certificate data processing, aggregation, and reporting. This system provides automated data pipeline capabilities with monitoring, scheduling, and quality control features.

## Architecture

### Core Components

1. **DataExtractor** - Extracts data from certificates, companies, products, and external sources
2. **DataTransformer** - Validates, cleans, enriches, and aggregates data
3. **DataLoader** - Loads processed data to data warehouses, files, and external systems
4. **ETLService** - Orchestrates the complete ETL pipeline
5. **ETLMonitor** - Provides monitoring, metrics, and alerting capabilities
6. **ETLSchedulerService** - Background service for scheduling and health monitoring

## API Endpoints

### ETL Management

- `POST /api/etl/run-full` - Run a complete ETL pipeline
- `POST /api/etl/run-incremental` - Run incremental data processing
- `GET /api/etl/jobs/status` - Get status of all ETL jobs
- `POST /api/etl/jobs/{jobId}/cancel` - Cancel a running ETL job
- `POST /api/etl/extract` - Extract data only
- `POST /api/etl/export` - Export data to file

### Monitoring & Reporting

- `GET /api/etl/metrics` - Get ETL performance metrics
- `GET /api/etl/alerts` - Get active alerts
- `POST /api/etl/alerts` - Create custom alerts
- `GET /api/etl/dashboard` - Get dashboard summary data
- `POST /api/etl/validate-config` - Validate ETL configuration

## Usage Examples

### 1. Basic Certificate Data Export

```json
POST /api/etl/export
{
  "dataSources": ["Certificates"],
  "filePath": "/exports/certificates_export.json",
  "format": "json"
}
```

### 2. Full ETL Pipeline with Aggregation

```json
POST /api/etl/run-full
{
  "name": "Daily Certificate Processing",
  "jobType": "Scheduled",
  "isIncremental": false,
  "dataSources": ["Certificates", "Companies", "Products"],
  "transformations": ["validate", "enrich", "aggregate"],
  "destinations": ["datawarehouse", "reporting"],
  "settings": {
    "batchSize": 1000,
    "enableDataQuality": true
  },
  "isActive": true,
  "cronSchedule": "0 2 * * *"
}
```

### 3. Incremental Data Processing

```json
POST /api/etl/run-incremental
{
  "name": "Hourly Updates",
  "jobType": "Incremental",
  "dataSources": ["Certificates"],
  "transformations": ["validate", "enrich"],
  "destinations": ["datawarehouse"],
  "settings": {
    "lookbackHours": 1
  }
}
```

### 4. Export to Multiple Formats

```json
POST /api/etl/run-full
{
  "name": "Multi-Format Export",
  "dataSources": ["Certificates", "Companies"],
  "transformations": ["validate", "enrich"],
  "destinations": [
    "file:/exports/data.json:json",
    "file:/exports/data.csv:csv",
    "file:/exports/data.xml:xml"
  ]
}
```

## Configuration Options

### Data Sources
- `Certificates` - Extract certificate data with relationships
- `Companies` - Extract company data with aggregates
- `Products` - Extract product data with certifications
- `ExternalAPI` - Extract from external APIs (requires configuration)
- `CSV` - Extract from CSV files
- `Database` - Extract from external databases

### Transformations
- `validate` - Data validation and cleaning
- `enrich` - Add calculated fields and business insights
- `aggregate` - Create daily/weekly/monthly aggregates
- `certificates` - Certificate-specific transformations

### Destinations
- `datawarehouse` - Load to internal data warehouse tables
- `reporting` - Update reporting views and summary tables
- `file:{path}:{format}` - Export to file (json, csv, xml)
- `api:{url}` - Send to external API
- `database:{connection}` - Load to external database

## Monitoring and Alerting

### Dashboard Metrics
- Total jobs run (last 7 days)
- Success/failure rates
- Records processed
- Average job duration
- Active jobs count
- Data quality scores
- System health status

### Alert Types
- `JobFailed` - ETL job execution failure
- `LongRunningJob` - Job running longer than threshold
- `HighFailureRate` - Multiple job failures
- `DataQualityIssue` - Data quality problems detected
- `SystemHealth` - System performance issues

### Example Metrics Request

```http
GET /api/etl/metrics?fromDate=2024-01-01&toDate=2024-01-31
```

Response:
```json
{
  "fromDate": "2024-01-01",
  "toDate": "2024-01-31",
  "totalJobsRun": 45,
  "successfulJobs": 42,
  "failedJobs": 3,
  "totalRecordsProcessed": 125000,
  "averageJobDuration": "00:05:30",
  "dataQualityMetrics": {
    "overallQualityScore": 98.5,
    "missingRequiredFields": 15,
    "invalidEmails": 3
  },
  "performanceMetrics": [
    {
      "metricName": "RecordsPerMinute",
      "value": 2500.0,
      "unit": "records/min"
    }
  ]
}
```

## Scheduling

### Cron Schedule Examples
- `0 2 * * *` - Daily at 2:00 AM
- `0 */6 * * *` - Every 6 hours
- `0 0 * * 0` - Weekly on Sunday at midnight
- `0 0 1 * *` - Monthly on the 1st at midnight

### Background Processing

The ETL system includes a background service that:
- Monitors scheduled jobs and executes them automatically
- Performs health checks every 5 minutes
- Collects performance metrics every 15 minutes
- Cleans up old logs and resolved alerts
- Detects long-running or stuck jobs

## Data Quality Features

### Validation Rules
- Required field validation
- Data type validation
- Format validation (emails, phone numbers)
- Business rule validation
- Range and constraint checking

### Data Cleaning
- Duplicate removal
- Null value handling
- Format standardization
- Data type conversion
- Invalid record filtering

### Quality Metrics
- Record completion rates
- Data accuracy scores
- Validation error counts
- Field-level quality assessment

## Security and Permissions

### API Security
- All ETL endpoints require Admin or SuperAdmin roles
- JWT token authentication
- Request logging and audit trails

### Data Protection
- Sensitive data handling
- Encryption in transit
- Secure file storage
- Access control logging

## Troubleshooting

### Common Issues

1. **Job Stuck in Running State**
   - Check for long-running queries
   - Review memory usage
   - Cancel and restart if necessary

2. **Data Quality Issues**
   - Review transformation logs
   - Check source data format
   - Update validation rules

3. **Export Failures**
   - Verify file path permissions
   - Check disk space
   - Validate export format

4. **Performance Issues**
   - Monitor batch sizes
   - Check database indexes
   - Review transformation complexity

### Logging

All ETL operations are logged with the following levels:
- **Information** - Normal processing events
- **Warning** - Data quality issues, performance concerns
- **Error** - Job failures, system errors
- **Critical** - System-wide failures

### Support

For ETL-related issues:
1. Check the `/api/etl/dashboard` for system status
2. Review `/api/etl/alerts` for active issues
3. Examine `/api/etl/metrics` for performance trends
4. Contact system administrator for assistance

## Best Practices

1. **Start Small** - Begin with simple configurations
2. **Test Thoroughly** - Validate configurations before production
3. **Monitor Actively** - Set up appropriate alerts
4. **Schedule Wisely** - Avoid peak business hours
5. **Document Changes** - Keep configuration history
6. **Regular Maintenance** - Clean up old data and logs
7. **Performance Tuning** - Monitor and optimize regularly

## Example Scenarios

### Scenario 1: Daily Certificate Report
```json
{
  "name": "Daily Certificate Report",
  "dataSources": ["Certificates"],
  "transformations": ["validate", "enrich", "aggregate"],
  "destinations": ["file:/reports/daily_certificates.csv:csv"],
  "cronSchedule": "0 6 * * *"
}
```

### Scenario 2: Data Warehouse Refresh
```json
{
  "name": "Weekly Data Warehouse Refresh",
  "dataSources": ["Certificates", "Companies", "Products"],
  "transformations": ["validate", "enrich", "aggregate"],
  "destinations": ["datawarehouse", "reporting"],
  "cronSchedule": "0 0 * * 0"
}
```

### Scenario 3: Real-time Integration
```json
{
  "name": "External System Sync",
  "dataSources": ["Certificates"],
  "transformations": ["validate", "enrich"],
  "destinations": ["api:https://external-system.com/api/certificates"],
  "cronSchedule": "0 */4 * * *"
}
```

This ETL system provides a robust foundation for data processing, reporting, and integration needs in the GCC Web Application.