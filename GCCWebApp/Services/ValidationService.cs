using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using GCCWebApp.Models;
using GCCWebApp.Data;

namespace GCCWebApp.Services
{
    public interface IValidationService
    {
        Task<ValidationResult> ValidateCertificateAsync(Certificate certificate);
        Task<ValidationResult> ValidateCompanyAsync(Company company);
        Task<ValidationResult> ValidateProductAsync(Product product);
        ValidationResult ValidateModel<T>(T model) where T : class;
    }

    public class ValidationService : IValidationService
    {
        private readonly ILogger<ValidationService> _logger;
        private readonly ApplicationDbContext _context;

        public ValidationService(ILogger<ValidationService> logger, ApplicationDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        public async Task<ValidationResult> ValidateCertificateAsync(Certificate certificate)
        {
            var result = new ValidationResult();

            // Basic model validation
            var modelValidation = ValidateModel(certificate);
            if (!modelValidation.IsValid)
            {
                result.AddErrors(modelValidation.Errors);
                return result;
            }

            try
            {
                // Business rule validations
                await ValidateCertificateNumberUniquenessAsync(certificate, result);
                await ValidateDateRangeAsync(certificate, result);
                await ValidateRelatedEntitiesExistAsync(certificate, result);
                ValidateCertificateBusinessRules(certificate, result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during certificate validation");
                result.AddError("An error occurred during validation");
            }

            return result;
        }

        public async Task<ValidationResult> ValidateCompanyAsync(Company company)
        {
            var result = new ValidationResult();

            // Basic model validation
            var modelValidation = ValidateModel(company);
            if (!modelValidation.IsValid)
            {
                result.AddErrors(modelValidation.Errors);
                return result;
            }

            try
            {
                // Check for duplicate company names
                if (await _context.Companies.AnyAsync(c => c.Name == company.Name && c.Id != company.Id))
                {
                    result.AddError($"A company with the name '{company.Name}' already exists.");
                }

                // Validate email format if provided
                if (!string.IsNullOrEmpty(company.ContactInfo) && company.ContactInfo.Contains("@"))
                {
                    if (!IsValidEmail(company.ContactInfo))
                    {
                        result.AddError("Contact information must be a valid email address.");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during company validation");
                result.AddError("An error occurred during validation");
            }

            return result;
        }

        public async Task<ValidationResult> ValidateProductAsync(Product product)
        {
            var result = new ValidationResult();

            // Basic model validation
            var modelValidation = ValidateModel(product);
            if (!modelValidation.IsValid)
            {
                result.AddErrors(modelValidation.Errors);
                return result;
            }

            try
            {
                // Check if category exists
                if (product.CategoryId > 0)
                {
                    if (!await _context.ProductCategories.AnyAsync(pc => pc.Id == product.CategoryId))
                    {
                        result.AddError("Selected product category does not exist.");
                    }
                }

                // Validate business rules for products
                ValidateProductBusinessRules(product, result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during product validation");
                result.AddError("An error occurred during validation");
            }

            return result;
        }

        public ValidationResult ValidateModel<T>(T model) where T : class
        {
            var result = new ValidationResult();
            var validationContext = new ValidationContext(model);
            var validationResults = new List<System.ComponentModel.DataAnnotations.ValidationResult>();

            bool isValid = Validator.TryValidateObject(model, validationContext, validationResults, true);

            if (!isValid)
            {
                foreach (var validationResult in validationResults)
                {
                    result.AddError(validationResult.ErrorMessage ?? "Validation error");
                }
            }

            return result;
        }

        #region Private Helper Methods

        private async Task ValidateCertificateNumberUniquenessAsync(Certificate certificate, ValidationResult result)
        {
            if (string.IsNullOrEmpty(certificate.Number)) return;

            // CR-038: 137 issued certificate numbers are shared by more than one migrated
            // certificate, and those numbers cannot be changed. Such rows are flagged
            // AllowsDuplicateNumber and exempted from the (now filtered) unique index.
            //
            // Without the carve-out below this check would make the migration self-defeating: every
            // one of those certificates would fail validation the first time an analyst opened and
            // saved it, purely because a sibling exists.
            //
            // So enforce only when the number is being INTRODUCED — a new certificate, or an
            // existing one whose number is actually changing. Saving a grandfathered record
            // unchanged is allowed; moving any certificate onto an in-use number is not.
            if (certificate.Id != 0)
            {
                var storedNumber = await _context.Certificates
                    .Where(c => c.Id == certificate.Id)
                    .Select(c => c.Number)
                    .FirstOrDefaultAsync();

                if (string.Equals(storedNumber, certificate.Number, StringComparison.Ordinal))
                    return; // number unchanged — nothing to police
            }

            var taken = await _context.Certificates
                .AnyAsync(c => c.Number == certificate.Number && c.Id != certificate.Id);

            if (taken)
            {
                result.AddError($"Certificate number '{certificate.Number}' is already in use.");
            }
        }

        private Task ValidateDateRangeAsync(Certificate certificate, ValidationResult result)
        {
            // Effective date cannot be in the future by more than 30 days
            if (certificate.EffectiveDate > DateTime.Today.AddDays(30))
            {
                result.AddError("Effective date cannot be more than 30 days in the future.");
            }

            // Expiration date must be after effective date
            if (certificate.ExpirationDate <= certificate.EffectiveDate)
            {
                result.AddError("Expiration date must be after the effective date.");
            }

            // Certificate cannot be valid for more than 5 years
            if (certificate.ExpirationDate > certificate.EffectiveDate.AddYears(5))
            {
                result.AddError("Certificate validity period cannot exceed 5 years.");
            }

            return Task.CompletedTask;
        }

        private async Task ValidateRelatedEntitiesExistAsync(Certificate certificate, ValidationResult result)
        {
            // Validate company exists
            if (!await _context.Companies.AnyAsync(c => c.Id == certificate.CompanyId))
            {
                result.AddError("Selected company does not exist.");
            }

            // Validate product exists
            if (!await _context.Products.AnyAsync(p => p.Id == certificate.ProductId))
            {
                result.AddError("Selected product does not exist.");
            }

            // Validate certification type exists
            if (!await _context.CertificationTypes.AnyAsync(ct => ct.Id == certificate.CertificationTypeId))
            {
                result.AddError("Selected certification type does not exist.");
            }
        }

        private void ValidateCertificateBusinessRules(Certificate certificate, ValidationResult result)
        {
            // Status validation
            var validStatuses = CertificateStatusHelper.GetAllStatuses().Keys;
            if (!validStatuses.Contains(certificate.Status))
            {
                result.AddError($"Status must be one of: {string.Join(", ", validStatuses)}");
            }

            // Validate product codes if provided
            ValidateProductCodes(certificate, result);
        }

        private void ValidateProductBusinessRules(Product product, ValidationResult result)
        {
            // Product name should not contain special characters that could cause issues
            if (!string.IsNullOrEmpty(product.Name) && ContainsInvalidCharacters(product.Name))
            {
                result.AddError("Product name contains invalid characters.");
            }
        }

        private void ValidateProductCodes(Certificate certificate, ValidationResult result)
        {
            // ASIN validation (Amazon Standard Identification Number)
            if (!string.IsNullOrEmpty(certificate.Asin) && !IsValidAsin(certificate.Asin))
            {
                result.AddError("ASIN format is invalid. Must be 10 alphanumeric characters.");
            }

            // UPC validation (Universal Product Code)
            if (!string.IsNullOrEmpty(certificate.Upc) && !IsValidUpc(certificate.Upc))
            {
                result.AddError("UPC format is invalid. Must be 12 digits.");
            }

            // GTIN validation (Global Trade Item Number)
            if (!string.IsNullOrEmpty(certificate.Gtin) && !IsValidGtin(certificate.Gtin))
            {
                result.AddError("GTIN format is invalid. Must be 8, 12, 13, or 14 digits.");
            }

            // EAN validation (European Article Number)
            if (!string.IsNullOrEmpty(certificate.Ean) && !IsValidEan(certificate.Ean))
            {
                result.AddError("EAN format is invalid. Must be 8 or 13 digits.");
            }
        }

        private bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        private bool ContainsInvalidCharacters(string input)
        {
            char[] invalidChars = { '<', '>', '"', '&', '\'' };
            return input.Any(c => invalidChars.Contains(c));
        }

        private bool IsValidAsin(string asin)
        {
            return !string.IsNullOrEmpty(asin) && 
                   asin.Length == 10 && 
                   asin.All(c => char.IsLetterOrDigit(c));
        }

        private bool IsValidUpc(string upc)
        {
            return !string.IsNullOrEmpty(upc) && 
                   upc.Length == 12 && 
                   upc.All(char.IsDigit);
        }

        private bool IsValidGtin(string gtin)
        {
            if (string.IsNullOrEmpty(gtin))
                return false;

            int[] validLengths = { 8, 12, 13, 14 };
            return validLengths.Contains(gtin.Length) && gtin.All(char.IsDigit);
        }

        private bool IsValidEan(string ean)
        {
            if (string.IsNullOrEmpty(ean))
                return false;

            return (ean.Length == 8 || ean.Length == 13) && ean.All(char.IsDigit);
        }

        #endregion
    }

    /// <summary>
    /// Represents validation result with errors
    /// </summary>
    public class ValidationResult
    {
        public bool IsValid => !Errors.Any();
        public List<string> Errors { get; } = new List<string>();

        public void AddError(string error)
        {
            if (!string.IsNullOrWhiteSpace(error))
            {
                Errors.Add(error);
            }
        }

        public void AddErrors(IEnumerable<string> errors)
        {
            foreach (var error in errors)
            {
                AddError(error);
            }
        }

        public string GetErrorsAsString()
        {
            return string.Join("; ", Errors);
        }
    }
}