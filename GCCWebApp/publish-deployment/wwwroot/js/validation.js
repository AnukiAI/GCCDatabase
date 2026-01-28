// Global validation utilities for admin forms

// Validation patterns
const ValidationPatterns = {
    email: /^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$/,
    phone: /^[\+]?[1-9][\d]{0,15}$/,
    strongPassword: /^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{8,}$/,
    alphanumeric: /^[a-zA-Z0-9\s]*$/,
    noSpecialChars: /^[a-zA-Z0-9\s\-\.]*$/
};

// Field length limits
const FieldLimits = {
    name: { min: 2, max: 50 },
    email: { min: 5, max: 100 },
    company: { min: 2, max: 100 },
    description: { min: 0, max: 500 },
    attributeName: { min: 3, max: 100 },
    tagName: { min: 2, max: 50 },
    fieldName: { min: 3, max: 100 }
};

// Validation functions
const Validator = {
    // Validate required fields
    required: function(value, fieldName) {
        if (!value || value.trim() === '') {
            return { valid: false, message: `${fieldName} is required` };
        }
        return { valid: true };
    },

    // Validate email format
    email: function(value) {
        if (!value) return { valid: true }; // Skip if empty (handle required separately)
        
        if (!ValidationPatterns.email.test(value)) {
            return { valid: false, message: 'Please enter a valid email address' };
        }
        return { valid: true };
    },

    // Validate field length
    length: function(value, fieldName, limits) {
        if (!value) return { valid: true }; // Skip if empty
        
        const limit = limits || FieldLimits[fieldName] || { min: 0, max: 255 };
        
        if (value.length < limit.min) {
            return { valid: false, message: `${fieldName} must be at least ${limit.min} characters` };
        }
        
        if (value.length > limit.max) {
            return { valid: false, message: `${fieldName} cannot exceed ${limit.max} characters` };
        }
        
        return { valid: true };
    },

    // Validate unique values (requires array of existing values)
    unique: function(value, fieldName, existingValues) {
        if (!value) return { valid: true };
        
        const exists = existingValues.some(existing => 
            existing.toLowerCase() === value.toLowerCase()
        );
        
        if (exists) {
            return { valid: false, message: `${fieldName} already exists` };
        }
        
        return { valid: true };
    },

    // Validate alphanumeric with spaces
    alphanumeric: function(value, fieldName) {
        if (!value) return { valid: true };
        
        if (!ValidationPatterns.noSpecialChars.test(value)) {
            return { valid: false, message: `${fieldName} can only contain letters, numbers, spaces, hyphens, and periods` };
        }
        
        return { valid: true };
    },

    // Strong password validation
    strongPassword: function(value) {
        if (!value) return { valid: true };
        
        if (!ValidationPatterns.strongPassword.test(value)) {
            return { 
                valid: false, 
                message: 'Password must be at least 8 characters and include uppercase, lowercase, number, and special character' 
            };
        }
        
        return { valid: true };
    }
};

// Form validation helper
function validateForm(formData, validationRules) {
    const errors = [];
    
    for (const field in validationRules) {
        const value = formData[field];
        const rules = validationRules[field];
        
        // Apply each validation rule
        for (const rule of rules) {
            let result;
            
            if (typeof rule === 'string') {
                // Simple validation type
                result = Validator[rule](value, field);
            } else if (typeof rule === 'object') {
                // Complex validation with parameters
                result = Validator[rule.type](value, field, rule.params);
            }
            
            if (result && !result.valid) {
                errors.push({
                    field: field,
                    message: result.message
                });
                break; // Stop at first error for this field
            }
        }
    }
    
    return {
        valid: errors.length === 0,
        errors: errors
    };
}

// Show validation errors in UI
function showValidationErrors(errors, formSelector = null) {
    // Clear existing errors
    document.querySelectorAll('.validation-error').forEach(error => error.remove());
    
    errors.forEach(error => {
        const fieldElement = document.getElementById(error.field) || 
                           document.querySelector(`[name="${error.field}"]`);
        
        if (fieldElement) {
            // Add error class to field
            fieldElement.classList.add('validation-error-field');
            
            // Create error message element
            const errorElement = document.createElement('div');
            errorElement.className = 'validation-error';
            errorElement.textContent = error.message;
            errorElement.style.color = '#dc3545';
            errorElement.style.fontSize = '16px';
            errorElement.style.marginTop = '5px';
            
            // Insert error message after the field
            fieldElement.parentNode.insertBefore(errorElement, fieldElement.nextSibling);
        }
    });
}

// Clear validation errors
function clearValidationErrors() {
    document.querySelectorAll('.validation-error').forEach(error => error.remove());
    document.querySelectorAll('.validation-error-field').forEach(field => {
        field.classList.remove('validation-error-field');
    });
}

// Add CSS for validation errors
if (!document.querySelector('#validation-styles')) {
    const style = document.createElement('style');
    style.id = 'validation-styles';
    style.textContent = `
        .validation-error-field {
            border-color: #dc3545 !important;
            box-shadow: 0 0 0 0.2rem rgba(220, 53, 69, 0.25) !important;
        }
        
        .validation-error {
            color: #dc3545;
            font-size: 16px;
            margin-top: 5px;
            display: block;
        }
    `;
    document.head.appendChild(style);
}