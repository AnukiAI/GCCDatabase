using Microsoft.EntityFrameworkCore;
using GCCWebApp.Data;
using GCCWebApp.Models;
using System.Text.Json;

namespace GCCWebApp.Services
{
    public class CertificateFieldService : ICertificateFieldService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<CertificateFieldService> _logger;

        public CertificateFieldService(ApplicationDbContext context, ILogger<CertificateFieldService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<List<CustomField>> GetFieldsForCertificateTypeAsync(int certificateTypeId)
        {
            try
            {
                return await _context.CustomFields
                    .Where(cf => cf.CertificateTypeId == certificateTypeId && cf.IsVisible)
                    .OrderBy(cf => cf.SortOrder)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting fields for certificate type {CertificateTypeId}", certificateTypeId);
                return new List<CustomField>();
            }
        }

        public async Task<Dictionary<int, string>> GetFieldValuesForCertificateAsync(int certificateId)
        {
            try
            {
                var values = await _context.CustomFieldValues
                    .Where(cfv => cfv.CertificateId == certificateId)
                    .ToDictionaryAsync(cfv => cfv.FieldId, cfv => cfv.TextValue ?? string.Empty);

                return values;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting field values for certificate {CertificateId}", certificateId);
                return new Dictionary<int, string>();
            }
        }

        public async Task<bool> SaveFieldValuesAsync(int certificateId, Dictionary<int, string> fieldValues)
        {
            try
            {
                // Get existing values
                var existingValues = await _context.CustomFieldValues
                    .Where(cfv => cfv.CertificateId == certificateId)
                    .ToListAsync();

                foreach (var fieldValue in fieldValues)
                {
                    var fieldId = fieldValue.Key;
                    var value = fieldValue.Value;

                    var existing = existingValues.FirstOrDefault(ev => ev.FieldId == fieldId);

                    if (existing != null)
                    {
                        // Update existing value
                        if (string.IsNullOrWhiteSpace(value))
                        {
                            // Remove if empty
                            _context.CustomFieldValues.Remove(existing);
                        }
                        else
                        {
                            existing.TextValue = value;
                        }
                    }
                    else if (!string.IsNullOrWhiteSpace(value))
                    {
                        // Create new value
                        _context.CustomFieldValues.Add(new CustomFieldValue
                        {
                            CertificateId = certificateId,
                            FieldId = fieldId,
                            TextValue = value
                        });
                    }
                }

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving field values for certificate {CertificateId}", certificateId);
                return false;
            }
        }

        public async Task<(bool isValid, List<string> errors)> ValidateFieldValuesAsync(Dictionary<int, string> fieldValues, List<CustomField> fields)
        {
            var errors = new List<string>();

            foreach (var field in fields.Where(f => f.IsRequired))
            {
                if (!fieldValues.ContainsKey(field.Id) || string.IsNullOrWhiteSpace(fieldValues[field.Id]))
                {
                    errors.Add($"{field.Name} is required");
                }
            }

            // Validate field types
            foreach (var fieldValue in fieldValues)
            {
                var field = fields.FirstOrDefault(f => f.Id == fieldValue.Key);
                if (field == null) continue;

                var value = fieldValue.Value;
                if (string.IsNullOrWhiteSpace(value)) continue;

                switch (field.FieldType.ToLower())
                {
                    case "number":
                        if (!decimal.TryParse(value, out _))
                        {
                            errors.Add($"{field.Name} must be a valid number");
                        }
                        break;

                    case "date":
                        if (!DateTime.TryParse(value, out _))
                        {
                            errors.Add($"{field.Name} must be a valid date");
                        }
                        break;

                    case "boolean":
                    case "checkbox":
                        if (!bool.TryParse(value, out _))
                        {
                            errors.Add($"{field.Name} must be true or false");
                        }
                        break;

                    case "select":
                    case "dropdown":
                        if (!string.IsNullOrEmpty(field.Options))
                        {
                            try
                            {
                                var options = JsonSerializer.Deserialize<List<string>>(field.Options);
                                if (options != null && !options.Contains(value))
                                {
                                    errors.Add($"{field.Name} must be one of the available options");
                                }
                            }
                            catch
                            {
                                // Invalid options format - skip validation
                            }
                        }
                        break;
                }

                // Custom validation rules
                if (!string.IsNullOrEmpty(field.ValidationRules))
                {
                    try
                    {
                        var rules = JsonSerializer.Deserialize<Dictionary<string, object>>(field.ValidationRules);
                        if (rules != null)
                        {
                            // Add custom validation logic here as needed
                            // Example: min/max length, regex patterns, etc.
                        }
                    }
                    catch
                    {
                        // Invalid validation rules format - skip
                    }
                }
            }

            return (errors.Count == 0, errors);
        }

        public async Task<List<CustomFieldValueDisplay>> GetFormattedFieldValuesAsync(int certificateId)
        {
            try
            {
                var result = new List<CustomFieldValueDisplay>();

                var certificate = await _context.Certificates
                    .Include(c => c.CertificationType)
                    .FirstOrDefaultAsync(c => c.Id == certificateId);

                if (certificate == null)
                    return result;

                var fields = await GetFieldsForCertificateTypeAsync(certificate.CertificationTypeId);
                var values = await GetFieldValuesForCertificateAsync(certificateId);

                foreach (var field in fields)
                {
                    values.TryGetValue(field.Id, out var value);
                    result.Add(new CustomFieldValueDisplay
                    {
                        Field = field,
                        Value = value
                    });
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting formatted field values for certificate {CertificateId}", certificateId);
                return new List<CustomFieldValueDisplay>();
            }
        }
    }
}
