// Two-Factor Preferences JavaScript
// Handles all client-side functionality for the Two-Factor Preferences page

(function() {
    'use strict';

    // Initialize when DOM is ready
    document.addEventListener('DOMContentLoaded', function() {
        initializeAlertDismissal();
        initializeRadioEnhancements();
        initializeFormValidation();
        initializeDisableAuthenticatorButton();
    });

    // Alert dismissal functionality
    function initializeAlertDismissal() {
        const closeButtons = document.querySelectorAll('[data-bs-dismiss="alert"]');
        closeButtons.forEach(function(button) {
            button.addEventListener('click', function() {
                const alert = this.closest('.alert');
                if (alert) {
                    alert.style.transition = 'opacity 0.3s ease';
                    alert.style.opacity = '0';
                    setTimeout(function() {
                        alert.remove();
                    }, 300);
                }
            });
        });
    }

    // Radio button enhancement for accessibility
    function initializeRadioEnhancements() {
        const radioOptions = document.querySelectorAll('.radio-option');
        radioOptions.forEach(function(option) {
            // Click handler
            option.addEventListener('click', function() {
                const radio = option.querySelector('input[type="radio"]');
                if (radio && !radio.checked) {
                    radio.checked = true;
                    // Trigger change event for form validation
                    radio.dispatchEvent(new Event('change', { bubbles: true }));
                }
            });
            
            // Keyboard support
            option.addEventListener('keydown', function(e) {
                if (e.key === 'Enter' || e.key === ' ') {
                    e.preventDefault();
                    const radio = option.querySelector('input[type="radio"]');
                    if (radio && !radio.checked) {
                        radio.checked = true;
                        radio.dispatchEvent(new Event('change', { bubbles: true }));
                    }
                }
            });
            
            // Make focusable for keyboard navigation
            option.setAttribute('tabindex', '0');
            option.setAttribute('role', 'radio');
            
            // Set initial aria-checked state
            const radio = option.querySelector('input[type="radio"]');
            if (radio) {
                option.setAttribute('aria-checked', radio.checked ? 'true' : 'false');
                
                // Update aria-checked when radio changes
                radio.addEventListener('change', function() {
                    // Update all radio options in the same group
                    const groupName = radio.name;
                    const allRadios = document.querySelectorAll(`input[type="radio"][name="${groupName}"]`);
                    allRadios.forEach(function(r) {
                        const parentOption = r.closest('.radio-option');
                        if (parentOption) {
                            parentOption.setAttribute('aria-checked', r.checked ? 'true' : 'false');
                        }
                    });
                });
            }
        });
    }

    // Form validation
    function initializeFormValidation() {
        const forms = document.querySelectorAll('form');
        forms.forEach(function(form) {
            form.addEventListener('submit', function(e) {
                // Add any custom validation here if needed
                const phoneInput = form.querySelector('input[type="tel"]');
                if (phoneInput && phoneInput.value) {
                    // Basic phone number validation
                    const phonePattern = /^[\d\s\(\)\-\+]+$/;
                    if (!phonePattern.test(phoneInput.value)) {
                        e.preventDefault();
                        alert('Please enter a valid phone number');
                        phoneInput.focus();
                        return false;
                    }
                }
            });
        });
    }

    // Handle disable authenticator button
    function initializeDisableAuthenticatorButton() {
        const disableButton = document.querySelector('[data-disable-authenticator]');
        if (disableButton) {
            disableButton.addEventListener('click', function(e) {
                const confirmed = confirm('Are you sure you want to disable Microsoft Authenticator? This will make your account less secure.');
                if (!confirmed) {
                    e.preventDefault();
                    return false;
                }
            });
        }
    }

    // Public API (if needed by other scripts)
    window.TwoFactorPreferences = {
        initialize: function() {
            // Re-initialize if needed
            initializeAlertDismissal();
            initializeRadioEnhancements();
            initializeFormValidation();
            initializeDisableAuthenticatorButton();
        }
    };
})();