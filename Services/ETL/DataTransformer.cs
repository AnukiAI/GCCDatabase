using GCCWebApp.Models;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Globalization;

namespace GCCWebApp.Services.ETL
{
    public class DataTransformer : IDataTransformer
    {
        private readonly ILogger<DataTransformer> _logger;

        public DataTransformer(ILogger<DataTransformer> logger)
        {
            _logger = logger;
        }

        public async Task<ETLTransformedData> TransformCertificateDataAsync(ETLExtractedData extractedData)
        {
            _logger.LogInformation("Starting certificate data transformation for {RecordCount} records", 
                extractedData.TotalRecords);

            var transformedData = new ETLTransformedData
            {
                TransformationTime = DateTime.UtcNow
            };

            try
            {
                if (!extractedData.Data.ContainsKey("Certificates"))
                {
                    _logger.LogWarning("No certificate data found in extracted data");
                    return transformedData;
                }

                var certificates = extractedData.Data["Certificates"];
                var transformedCertificates = new List<object>();

                foreach (var cert in certificates)
                {
                    var certificateData = JsonSerializer.Deserialize<Dictionary<string, object>>(
                        JsonSerializer.Serialize(cert));

                    if (certificateData != null)
                    {
                        var transformedCert = await TransformSingleCertificateAsync(certificateData, transformedData);
                        if (transformedCert != null)
                        {
                            transformedCertificates.Add(transformedCert);
                            transformedData.ValidRecords++;
                        }
                        else
                        {
                            transformedData.InvalidRecords++;
                        }
                    }
                }

                transformedData.TransformedData.Add("Certificates", transformedCertificates);
                transformedData.TotalRecords = transformedCertificates.Count;

                _logger.LogInformation("Certificate transformation completed. Valid: {Valid}, Invalid: {Invalid}", 
                    transformedData.ValidRecords, transformedData.InvalidRecords);

                return transformedData;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during certificate data transformation");
                throw;
            }
        }

        public async Task<ETLTransformedData> ValidateAndCleanDataAsync(ETLExtractedData extractedData)
        {
            _logger.LogInformation("Starting data validation and cleaning");

            var transformedData = new ETLTransformedData
            {
                TransformationTime = DateTime.UtcNow
            };

            try
            {
                foreach (var dataSet in extractedData.Data)
                {
                    var cleanedData = new List<object>();
                    
                    foreach (var record in dataSet.Value)
                    {
                        var recordDict = JsonSerializer.Deserialize<Dictionary<string, object>>(
                            JsonSerializer.Serialize(record));

                        if (recordDict != null)
                        {
                            var cleanedRecord = CleanDataRecord(recordDict, dataSet.Key, transformedData);
                            if (cleanedRecord != null)
                            {
                                cleanedData.Add(cleanedRecord);
                                transformedData.ValidRecords++;
                            }
                            else
                            {
                                transformedData.InvalidRecords++;
                            }
                        }
                    }

                    transformedData.TransformedData.Add(dataSet.Key, cleanedData);
                }

                transformedData.TotalRecords = transformedData.ValidRecords + transformedData.InvalidRecords;

                _logger.LogInformation("Data validation completed. Valid: {Valid}, Invalid: {Invalid}, Issues: {Issues}", 
                    transformedData.ValidRecords, transformedData.InvalidRecords, 
                    transformedData.DataQualityIssues.Count);

                return transformedData;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during data validation and cleaning");
                throw;
            }
        }

        public async Task<ETLTransformedData> EnrichDataAsync(ETLExtractedData extractedData)
        {
            _logger.LogInformation("Starting data enrichment");

            var transformedData = new ETLTransformedData
            {
                TransformationTime = DateTime.UtcNow
            };

            try
            {
                // Enrich certificate data with calculated fields and lookups
                if (extractedData.Data.ContainsKey("Certificates"))
                {
                    var certificates = extractedData.Data["Certificates"];
                    var enrichedCertificates = new List<object>();

                    foreach (var cert in certificates)
                    {
                        var certDict = JsonSerializer.Deserialize<Dictionary<string, object>>(
                            JsonSerializer.Serialize(cert));

                        if (certDict != null)
                        {
                            var enrichedCert = await EnrichCertificateData(certDict);
                            enrichedCertificates.Add(enrichedCert);
                        }
                    }

                    transformedData.TransformedData.Add("Certificates", enrichedCertificates);
                }

                // Copy other data sets as-is for now
                foreach (var dataSet in extractedData.Data)
                {
                    if (!transformedData.TransformedData.ContainsKey(dataSet.Key))
                    {
                        transformedData.TransformedData.Add(dataSet.Key, dataSet.Value);
                    }
                }

                transformedData.TotalRecords = transformedData.TransformedData.Values.Sum(v => v.Count);
                transformedData.ValidRecords = transformedData.TotalRecords;

                _logger.LogInformation("Data enrichment completed for {Count} records", transformedData.TotalRecords);

                return transformedData;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during data enrichment");
                throw;
            }
        }

        public async Task<ETLTransformedData> AggregateDataAsync(ETLExtractedData extractedData)
        {
            _logger.LogInformation("Starting data aggregation");

            var transformedData = new ETLTransformedData
            {
                TransformationTime = DateTime.UtcNow
            };

            try
            {
                // Create daily certificate aggregates
                if (extractedData.Data.ContainsKey("Certificates"))
                {
                    var certificates = extractedData.Data["Certificates"];
                    var dailyAggregates = await CreateDailyAggregatesAsync(certificates);
                    var weeklyAggregates = await CreateWeeklyAggregatesAsync(certificates);
                    var monthlyAggregates = await CreateMonthlyAggregatesAsync(certificates);

                    transformedData.TransformedData.Add("DailyCertificateAggregates", dailyAggregates);
                    transformedData.TransformedData.Add("WeeklyCertificateAggregates", weeklyAggregates);
                    transformedData.TransformedData.Add("MonthlyCertificateAggregates", monthlyAggregates);
                }

                // Create company aggregates
                if (extractedData.Data.ContainsKey("Companies"))
                {
                    var companies = extractedData.Data["Companies"];
                    var companyAggregates = await CreateCompanyAggregatesAsync(companies);
                    transformedData.TransformedData.Add("CompanyAggregates", companyAggregates);
                }

                transformedData.TotalRecords = transformedData.TransformedData.Values.Sum(v => v.Count);
                transformedData.ValidRecords = transformedData.TotalRecords;

                _logger.LogInformation("Data aggregation completed. Created {Count} aggregate records", 
                    transformedData.TotalRecords);

                return transformedData;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during data aggregation");
                throw;
            }
        }

        private async Task<object?> TransformSingleCertificateAsync(Dictionary<string, object> certificate, 
            ETLTransformedData transformedData)
        {
            try
            {
                // Validate required fields
                if (!ValidateRequiredFields(certificate, new[] { "Id", "Number", "CompanyId", "ProductId" }))
                {
                    AddDataQualityIssue(transformedData, "MissingRequiredFields", 
                        "Certificate missing required fields", certificate.GetValueOrDefault("Id")?.ToString());
                    return null;
                }

                // Clean and standardize certificate number
                if (certificate.ContainsKey("Number"))
                {
                    var cleanNumber = CleanCertificateNumber(certificate["Number"]?.ToString());
                    if (string.IsNullOrEmpty(cleanNumber))
                    {
                        AddDataQualityIssue(transformedData, "InvalidCertificateNumber", 
                            "Invalid certificate number format", certificate["Id"]?.ToString());
                        return null;
                    }
                    certificate["Number"] = cleanNumber;
                }

                // Validate and format dates
                if (certificate.ContainsKey("EffectiveDate"))
                {
                    var effectiveDate = ParseDate(certificate["EffectiveDate"]?.ToString());
                    if (effectiveDate.HasValue)
                        certificate["EffectiveDate"] = effectiveDate.Value;
                }

                if (certificate.ContainsKey("ExpirationDate"))
                {
                    var expirationDate = ParseDate(certificate["ExpirationDate"]?.ToString());
                    if (expirationDate.HasValue)
                        certificate["ExpirationDate"] = expirationDate.Value;
                }

                // Add calculated fields
                certificate["ProcessedAt"] = DateTime.UtcNow;
                certificate["DataQuality"] = "Good";

                return certificate;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error transforming certificate with ID: {Id}", 
                    certificate.GetValueOrDefault("Id"));
                return null;
            }
        }

        private Dictionary<string, object>? CleanDataRecord(Dictionary<string, object> record, string dataType, 
            ETLTransformedData transformedData)
        {
            try
            {
                var cleanedRecord = new Dictionary<string, object>();

                foreach (var kvp in record)
                {
                    var cleanedValue = CleanValue(kvp.Value, kvp.Key, dataType, transformedData);
                    if (cleanedValue != null)
                    {
                        cleanedRecord[kvp.Key] = cleanedValue;
                    }
                }

                return cleanedRecord.Count > 0 ? cleanedRecord : null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cleaning data record");
                return null;
            }
        }

        private object? CleanValue(object? value, string fieldName, string dataType, ETLTransformedData transformedData)
        {
            if (value == null) return null;

            var stringValue = value.ToString()?.Trim();
            if (string.IsNullOrEmpty(stringValue)) return null;

            // Clean based on field name patterns
            switch (fieldName.ToLower())
            {
                case "email":
                    return CleanEmail(stringValue, fieldName, transformedData);
                case "phone":
                    return CleanPhoneNumber(stringValue);
                case "country":
                    return CleanCountryName(stringValue);
                case "number" when dataType == "Certificates":
                    return CleanCertificateNumber(stringValue);
                default:
                    return stringValue;
            }
        }

        private async Task<object> EnrichCertificateData(Dictionary<string, object> certificate)
        {
            var enrichedCert = new Dictionary<string, object>(certificate);

            // Add risk assessment
            enrichedCert["RiskLevel"] = CalculateRiskLevel(certificate);

            // Add business insights
            enrichedCert["BusinessInsights"] = GenerateBusinessInsights(certificate);

            // Add geographical information
            if (certificate.ContainsKey("Country"))
            {
                enrichedCert["GeographicalRegion"] = GetGeographicalRegion(certificate["Country"]?.ToString());
            }

            // Add expiration alerts
            if (certificate.ContainsKey("ExpirationDate"))
            {
                var expirationDate = ParseDate(certificate["ExpirationDate"]?.ToString());
                if (expirationDate.HasValue)
                {
                    var daysUntilExpiration = (expirationDate.Value - DateTime.UtcNow).Days;
                    enrichedCert["ExpirationAlert"] = GetExpirationAlert(daysUntilExpiration);
                }
            }

            return enrichedCert;
        }

        private async Task<List<object>> CreateDailyAggregatesAsync(List<object> certificates)
        {
            var aggregates = new List<object>();

            var groupedByDate = certificates
                .Select(c => JsonSerializer.Deserialize<Dictionary<string, object>>(JsonSerializer.Serialize(c)))
                .Where(c => c != null)
                .GroupBy(c => ParseDate(c!["CreatedAt"]?.ToString())?.Date ?? DateTime.Today);

            foreach (var group in groupedByDate)
            {
                var aggregate = new CertificateAggregateData
                {
                    Period = "Daily",
                    PeriodDate = group.Key,
                    TotalCertificates = group.Count(),
                    ActiveCertificates = group.Count(c => c["Status"]?.ToString() == "active"),
                    ExpiredCertificates = group.Count(c => 
                        ParseDate(c["ExpirationDate"]?.ToString()) < DateTime.UtcNow),
                    CreatedAt = DateTime.UtcNow
                };

                aggregates.Add(aggregate);
            }

            return aggregates;
        }

        private async Task<List<object>> CreateWeeklyAggregatesAsync(List<object> certificates)
        {
            // Similar to daily but group by week
            return await CreateDailyAggregatesAsync(certificates); // Simplified for demo
        }

        private async Task<List<object>> CreateMonthlyAggregatesAsync(List<object> certificates)
        {
            // Similar to daily but group by month
            return await CreateDailyAggregatesAsync(certificates); // Simplified for demo
        }

        private async Task<List<object>> CreateCompanyAggregatesAsync(List<object> companies)
        {
            return companies.Select(c =>
            {
                var companyDict = JsonSerializer.Deserialize<Dictionary<string, object>>(JsonSerializer.Serialize(c));
                return new
                {
                    CompanyId = companyDict?["Id"],
                    CompanyName = companyDict?["Name"],
                    AggregatedAt = DateTime.UtcNow,
                    TotalCertificates = companyDict?["TotalCertificates"] ?? 0,
                    ActiveCertificates = companyDict?["ActiveCertificates"] ?? 0
                };
            }).Cast<object>().ToList();
        }

        // Helper methods
        private bool ValidateRequiredFields(Dictionary<string, object> record, string[] requiredFields)
        {
            return requiredFields.All(field => 
                record.ContainsKey(field) && record[field] != null && 
                !string.IsNullOrWhiteSpace(record[field].ToString()));
        }

        private string? CleanCertificateNumber(string? certificateNumber)
        {
            if (string.IsNullOrWhiteSpace(certificateNumber)) return null;
            
            // Remove special characters and normalize
            var cleaned = Regex.Replace(certificateNumber, @"[^\w]", "").ToUpper();
            return cleaned.Length >= 3 && cleaned.Length <= 10 ? cleaned : null;
        }

        private string? CleanEmail(string email, string fieldName, ETLTransformedData transformedData)
        {
            var emailRegex = new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$");
            if (emailRegex.IsMatch(email))
                return email.ToLower();

            AddDataQualityIssue(transformedData, "InvalidEmail", "Invalid email format", null, fieldName, email, null);
            return null;
        }

        private string? CleanPhoneNumber(string phone)
        {
            return Regex.Replace(phone, @"[^\d]", "");
        }

        private string CleanCountryName(string country)
        {
            return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(country.ToLower());
        }

        private DateTime? ParseDate(string? dateString)
        {
            if (string.IsNullOrWhiteSpace(dateString)) return null;
            return DateTime.TryParse(dateString, out var date) ? date : null;
        }

        private string CalculateRiskLevel(Dictionary<string, object> certificate)
        {
            var expirationDate = ParseDate(certificate.GetValueOrDefault("ExpirationDate")?.ToString());
            if (!expirationDate.HasValue) return "Unknown";

            var daysUntilExpiration = (expirationDate.Value - DateTime.UtcNow).Days;
            
            return daysUntilExpiration switch
            {
                < 0 => "Critical", // Expired
                < 30 => "High",    // Expires within 30 days
                < 90 => "Medium",  // Expires within 90 days
                _ => "Low"
            };
        }

        private Dictionary<string, object> GenerateBusinessInsights(Dictionary<string, object> certificate)
        {
            return new Dictionary<string, object>
            {
                ["ComplianceStatus"] = "Active",
                ["RenewalRequired"] = ParseDate(certificate.GetValueOrDefault("ExpirationDate")?.ToString()) <= DateTime.UtcNow.AddDays(60),
                ["MarketRelevance"] = "High"
            };
        }

        private string GetGeographicalRegion(string? country)
        {
            if (string.IsNullOrEmpty(country)) return "Unknown";

            // Simplified region mapping
            var regions = new Dictionary<string, string>
            {
                ["USA"] = "North America",
                ["Canada"] = "North America",
                ["Mexico"] = "North America",
                ["Germany"] = "Europe",
                ["France"] = "Europe",
                ["China"] = "Asia Pacific",
                ["Japan"] = "Asia Pacific"
            };

            return regions.GetValueOrDefault(country, "Other");
        }

        private string GetExpirationAlert(int daysUntilExpiration)
        {
            return daysUntilExpiration switch
            {
                < 0 => "EXPIRED",
                < 7 => "CRITICAL - Expires this week",
                < 30 => "WARNING - Expires this month",
                < 60 => "NOTICE - Expires in 2 months",
                _ => "OK"
            };
        }

        private void AddDataQualityIssue(ETLTransformedData transformedData, string issueType, string description, 
            string? recordId = null, string? fieldName = null, object? actualValue = null, object? expectedValue = null)
        {
            transformedData.DataQualityIssues.Add(new ETLDataQualityIssue
            {
                IssueType = issueType,
                Description = description,
                RecordId = recordId,
                FieldName = fieldName ?? "",
                ActualValue = actualValue,
                ExpectedValue = expectedValue,
                Severity = ETLSeverity.Warning
            });
        }
    }
}