using System.ComponentModel.DataAnnotations;
using GCCWebApp.Models.Api;

namespace GCCWebApp.Extensions
{
    public static class ValidationExtensions
    {
        public static List<ApiError> ValidateModel<T>(this T model) where T : class
        {
            var errors = new List<ApiError>();
            var validationContext = new ValidationContext(model);
            var validationResults = new List<ValidationResult>();

            bool isValid = Validator.TryValidateObject(model, validationContext, validationResults, true);

            if (!isValid)
            {
                foreach (var validationResult in validationResults)
                {
                    var field = validationResult.MemberNames.FirstOrDefault();
                    errors.Add(new ApiError(
                        ApiErrorCodes.ValidationError,
                        validationResult.ErrorMessage ?? "Validation error",
                        field
                    ));
                }
            }

            return errors;
        }

        public static bool IsValidModel<T>(this T model, out List<ApiError> errors) where T : class
        {
            errors = model.ValidateModel();
            return !errors.Any();
        }

        public static List<ApiError> ValidateBusinessRules<T>(this T model) where T : class
        {
            var errors = new List<ApiError>();

            // Add custom business rule validations here
            // This is a placeholder for business-specific validation logic
            switch (model)
            {
                case Models.Certificate certificate:
                    errors.AddRange(ValidateCertificateBusinessRules(certificate));
                    break;
                case Models.Product product:
                    errors.AddRange(ValidateProductBusinessRules(product));
                    break;
            }

            return errors;
        }

        private static List<ApiError> ValidateCertificateBusinessRules(Models.Certificate certificate)
        {
            var errors = new List<ApiError>();

            // Effective date cannot be in the future beyond 1 year
            if (certificate.EffectiveDate > DateTime.UtcNow.AddYears(1))
            {
                errors.Add(new ApiError(
                    ApiErrorCodes.BusinessRuleViolation,
                    "Effective date cannot be more than 1 year in the future",
                    nameof(certificate.EffectiveDate).ToLowerInvariant()
                ));
            }

            // Expiration date must be after effective date
            if (certificate.ExpirationDate != default && 
                certificate.ExpirationDate <= certificate.EffectiveDate)
            {
                errors.Add(new ApiError(
                    ApiErrorCodes.BusinessRuleViolation,
                    "Expiration date must be after effective date",
                    nameof(certificate.ExpirationDate).ToLowerInvariant()
                ));
            }

            // Certificate number format validation
            if (!string.IsNullOrEmpty(certificate.Number) && 
                certificate.Number.Length < 3)
            {
                errors.Add(new ApiError(
                    ApiErrorCodes.BusinessRuleViolation,
                    "Certificate number must be at least 3 characters long",
                    nameof(certificate.Number).ToLowerInvariant()
                ));
            }

            return errors;
        }

        private static List<ApiError> ValidateProductBusinessRules(Models.Product product)
        {
            var errors = new List<ApiError>();

            // Product name cannot contain special characters that might cause issues
            if (!string.IsNullOrEmpty(product.Name) && 
                product.Name.Any(c => char.IsControl(c)))
            {
                errors.Add(new ApiError(
                    ApiErrorCodes.BusinessRuleViolation,
                    "Product name cannot contain control characters",
                    nameof(product.Name).ToLowerInvariant()
                ));
            }

            // SKU format validation (if provided)
            if (!string.IsNullOrEmpty(product.Sku) && 
                (product.Sku.Length < 3 || product.Sku.Length > 50))
            {
                errors.Add(new ApiError(
                    ApiErrorCodes.BusinessRuleViolation,
                    "SKU must be between 3 and 50 characters",
                    nameof(product.Sku).ToLowerInvariant()
                ));
            }

            return errors;
        }

        public static List<ApiError> CombineValidationErrors<T>(this T model) where T : class
        {
            var allErrors = new List<ApiError>();
            
            // Add model validation errors
            allErrors.AddRange(model.ValidateModel());
            
            // Add business rule validation errors
            allErrors.AddRange(model.ValidateBusinessRules());

            return allErrors;
        }
    }
}