// Authentication helper functions
document.addEventListener('DOMContentLoaded', function() {
    // Handle logout form submission
    const logoutForm = document.getElementById('logoutForm');
    if (logoutForm) {
        logoutForm.addEventListener('submit', function(e) {
            // Optionally show a loading state
            const submitButton = this.querySelector('button[type="submit"]');
            if (submitButton) {
                submitButton.innerHTML = '<i class="fas fa-spinner fa-spin"></i> Signing out...';
                submitButton.disabled = true;
            }
        });
    }

    // Handle login form submission
    const loginForm = document.querySelector('form[action*="Login"]');
    if (loginForm) {
        loginForm.addEventListener('submit', function(e) {
            const submitButton = this.querySelector('button[type="submit"]');
            if (submitButton) {
                const originalText = submitButton.innerHTML;
                submitButton.innerHTML = '<i class="fas fa-spinner fa-spin"></i> Signing in...';
                submitButton.disabled = true;
                
                // Re-enable after a timeout in case of error
                setTimeout(() => {
                    submitButton.innerHTML = originalText;
                    submitButton.disabled = false;
                }, 10000);
            }
        });
    }

    // Auto-fill test credentials in development
    if (window.location.hostname === 'localhost') {
        const emailInput = document.getElementById('Email');
        const passwordInput = document.getElementById('Password');
        const testCredentials = document.querySelector('.test-credentials');
        
        if (testCredentials && emailInput && passwordInput) {
            testCredentials.addEventListener('click', function(e) {
                if (e.target.tagName === 'LI' || e.target.tagName === 'STRONG') {
                    const li = e.target.closest('li');
                    if (li) {
                        const text = li.textContent;
                        const match = text.match(/(\S+@\S+\.\S+)\s*\/\s*(\S+)/);
                        if (match) {
                            emailInput.value = match[1];
                            passwordInput.value = match[2];
                            emailInput.focus();
                        }
                    }
                }
            });
            
            // Add click hint
            const items = testCredentials.querySelectorAll('li');
            items.forEach(item => {
                item.style.cursor = 'pointer';
                item.title = 'Click to auto-fill';
            });
        }
    }

    // Handle session timeout
    let sessionTimeout;
    let warningTimeout;
    const SESSION_TIMEOUT = 30 * 60 * 1000; // 30 minutes
    const WARNING_TIME = 5 * 60 * 1000; // 5 minutes before timeout
    let warningShown = false;

    function resetSessionTimeout() {
        clearTimeout(sessionTimeout);
        clearTimeout(warningTimeout);
        warningShown = false;

        // Show warning 5 minutes before timeout
        warningTimeout = setTimeout(() => {
            if (document.querySelector('#logoutForm') && !warningShown) {
                warningShown = true;
                AdminCore.showNotification(
                    'Your session will expire in 5 minutes due to inactivity. Click anywhere to stay logged in.',
                    'warning',
                    { duration: 10000 }
                );
            }
        }, SESSION_TIMEOUT - WARNING_TIME);

        // Logout at timeout
        sessionTimeout = setTimeout(() => {
            if (document.querySelector('#logoutForm')) {
                // Save any unsaved form data before logout
                if (typeof UnsavedChanges !== 'undefined' && UnsavedChanges.initialized && UnsavedChanges.hasChanges) {
                    UnsavedChanges.saveToLocalStorage();
                    AdminCore.showNotification('Your session has expired. Form data has been saved and can be restored when you log back in.', 'warning', { duration: 0 });
                } else {
                    AdminCore.showNotification('Your session has expired. You will be logged out.', 'warning', { duration: 0 });
                }
                setTimeout(() => {
                    document.getElementById('logoutForm').submit();
                }, 3000); // Give user time to see the message
            }
        }, SESSION_TIMEOUT);
    }

    // Reset timeout on user activity
    if (document.querySelector('#logoutForm')) {
        ['click', 'mousemove', 'keypress', 'scroll', 'touchstart'].forEach(event => {
            document.addEventListener(event, resetSessionTimeout, { passive: true });
        });
        resetSessionTimeout();
    }

    // Handle authentication errors (only if server didn't already render one)
    const urlParams = new URLSearchParams(window.location.search);
    const error = urlParams.get('error');
    if (error && !document.querySelector('.alert-error')) {
        const errorMessages = {
            'authentication_failed': 'Authentication failed. Please check your credentials.',
            'access_denied': 'Access denied. Please contact your administrator.',
            'session_expired': 'Your session has expired. Please sign in again.',
            'invalid_token': 'Invalid authentication token. Please try again.'
        };

        const message = errorMessages[error] || 'An authentication error occurred.';
        const alertDiv = document.createElement('div');
        alertDiv.className = 'alert alert-error';
        alertDiv.innerHTML = `<i class="fas fa-exclamation-circle"></i> ${message}`;

        const container = document.querySelector('.login-form-section') || document.querySelector('main');
        if (container) {
            container.insertBefore(alertDiv, container.firstChild);
        }
    }

    // QC #71: Global fetch interceptor — catch 401 responses from AJAX calls when session expires
    // and redirect to login instead of silently failing or showing broken UI
    const originalFetch = window.fetch;
    window.fetch = async function(...args) {
        const response = await originalFetch.apply(this, args);
        if (response.status === 401 && document.querySelector('#logoutForm')) {
            // Session has expired server-side — redirect to login
            if (typeof UnsavedChanges !== 'undefined' && UnsavedChanges.initialized && UnsavedChanges.hasChanges) {
                UnsavedChanges.saveToLocalStorage();
            }
            window.location.href = '/Account/Login?error=session_expired';
            // Return the response so in-flight code doesn't throw
            return response;
        }
        return response;
    };

    // Smooth redirect after login
    const returnUrl = urlParams.get('returnUrl');
    if (returnUrl && window.location.pathname.includes('/Account/Login')) {
        const returnInfo = document.createElement('div');
        returnInfo.className = 'return-info';
        returnInfo.style.cssText = 'background: #f0f9ff; border: 1px solid #bae6fd; padding: 10px; border-radius: 5px; margin-bottom: 15px; font-size: 14px; color: #0369a1;';
        returnInfo.innerHTML = `<i class="fas fa-info-circle"></i> You will be redirected to your requested page after signing in.`;
        
        const loginHeader = document.querySelector('.login-header');
        if (loginHeader && loginHeader.nextSibling) {
            loginHeader.parentNode.insertBefore(returnInfo, loginHeader.nextSibling);
        }
    }
});