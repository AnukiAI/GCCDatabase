using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace GCCWebApp.Validation;

/// <summary>
/// Validates input against common injection attack patterns
/// </summary>
public class InputSanitizationAttribute : ValidationAttribute
{
    private readonly bool _allowHtml;
    private readonly bool _allowSqlKeywords;
    private readonly bool _allowScriptTags;

    public InputSanitizationAttribute(bool allowHtml = false, bool allowSqlKeywords = false, bool allowScriptTags = false)
    {
        _allowHtml = allowHtml;
        _allowSqlKeywords = allowSqlKeywords;
        _allowScriptTags = allowScriptTags;
        ErrorMessage = "Input contains potentially unsafe characters or patterns.";
    }

    protected override ValidationResult IsValid(object? value, ValidationContext validationContext)
    {
        if (value == null || string.IsNullOrWhiteSpace(value.ToString()))
            return ValidationResult.Success!;

        string input = value.ToString()!;
        
        // Check for SQL injection patterns
        if (!_allowSqlKeywords && ContainsSqlInjectionPatterns(input))
        {
            return new ValidationResult("Input contains potentially unsafe SQL keywords.");
        }

        // Check for script injection patterns
        if (!_allowScriptTags && ContainsScriptInjectionPatterns(input))
        {
            return new ValidationResult("Input contains potentially unsafe script content.");
        }

        // Check for HTML injection if not allowed
        if (!_allowHtml && ContainsHtmlPatterns(input))
        {
            return new ValidationResult("Input contains potentially unsafe HTML content.");
        }

        // Check for path traversal attempts
        if (ContainsPathTraversalPatterns(input))
        {
            return new ValidationResult("Input contains potentially unsafe path traversal patterns.");
        }

        return ValidationResult.Success!;
    }

    private bool ContainsSqlInjectionPatterns(string input)
    {
        string[] sqlKeywords = {
            "SELECT", "INSERT", "UPDATE", "DELETE", "DROP", "CREATE", "ALTER", 
            "EXEC", "EXECUTE", "UNION", "SCRIPT", "DECLARE", "CURSOR", "FETCH"
        };

        string upperInput = input.ToUpperInvariant();
        
        // Check for SQL keywords
        foreach (string keyword in sqlKeywords)
        {
            if (upperInput.Contains(keyword))
                return true;
        }

        // Check for common SQL injection patterns
        string[] injectionPatterns = { 
            "--", "/*", "*/", ";", "'", "\"", "xp_", "sp_", 
            "1=1", "OR 1=1", "AND 1=1", "' OR '1'='1", "\" OR \"1\"=\"1"
        };
        
        return injectionPatterns.Any(pattern => upperInput.Contains(pattern.ToUpperInvariant()));
    }

    private bool ContainsScriptInjectionPatterns(string input)
    {
        // Regex patterns for script injection
        string[] patterns = {
            @"<script[^>]*>.*?</script>",
            @"javascript:",
            @"vbscript:",
            @"on\w+\s*=",
            @"eval\s*\(",
            @"expression\s*\(",
            @"alert\s*\(",
            @"confirm\s*\(",
            @"prompt\s*\("
        };

        return patterns.Any(pattern => 
            Regex.IsMatch(input, pattern, RegexOptions.IgnoreCase | RegexOptions.Singleline));
    }

    private bool ContainsHtmlPatterns(string input)
    {
        // Check for HTML/XML tags
        return Regex.IsMatch(input, @"<[^>]+>", RegexOptions.IgnoreCase);
    }

    private bool ContainsPathTraversalPatterns(string input)
    {
        string[] traversalPatterns = { "../", "..\\", "%2e%2e%2f", "%2e%2e%5c", "..%2f", "..%5c" };
        return traversalPatterns.Any(pattern => 
            input.IndexOf(pattern, StringComparison.OrdinalIgnoreCase) >= 0);
    }
}