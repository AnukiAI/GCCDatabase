using GCCWebApp.Models;

namespace GCCWebApp.Services
{
    public interface ICertificateFieldService
    {
        /// <summary>
        /// Gets all visible custom fields (global - applies to all certificate types)
        /// certificateTypeId parameter kept for backward compatibility but ignored
        /// </summary>
        Task<List<CustomField>> GetFieldsForCertificateTypeAsync(int certificateTypeId);

        /// <summary>
        /// Gets all visible custom fields with role-based filtering
        /// </summary>
        /// <param name="includeInternalFields">If true, includes fields where IsPublic=false. If false, only returns public fields.</param>
        /// <returns>List of custom fields filtered by visibility and public/internal status</returns>
        Task<List<CustomField>> GetFieldsAsync(bool includeInternalFields = true);

        /// <summary>
        /// Gets all field values for a specific certificate
        /// </summary>
        Task<Dictionary<int, string>> GetFieldValuesForCertificateAsync(int certificateId);

        /// <summary>
        /// Saves or updates field values for a certificate
        /// </summary>
        Task<bool> SaveFieldValuesAsync(int certificateId, Dictionary<int, string> fieldValues);

        /// <summary>
        /// Validates field values against field definitions
        /// </summary>
        Task<(bool isValid, List<string> errors)> ValidateFieldValuesAsync(Dictionary<int, string> fieldValues, List<CustomField> fields);

        /// <summary>
        /// Gets formatted display values for certificate details view
        /// </summary>
        Task<List<CustomFieldValueDisplay>> GetFormattedFieldValuesAsync(int certificateId);
    }

    public class CustomFieldValueDisplay
    {
        public CustomField Field { get; set; } = null!;
        public string? Value { get; set; }

        public string DisplayValue
        {
            get
            {
                if (string.IsNullOrWhiteSpace(Value))
                    return "-";

                switch (Field.FieldType.ToLower())
                {
                    case "date":
                        if (DateTime.TryParse(Value, out var date))
                            return date.ToString("MMMM dd, yyyy");
                        break;
                    case "boolean":
                    case "checkbox":
                        return Value.ToLower() == "true" ? "Yes" : "No";
                    case "number":
                        if (decimal.TryParse(Value, out var number))
                            return number.ToString("N2");
                        break;
                }

                return Value;
            }
        }
    }
}
